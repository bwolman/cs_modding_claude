using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Events;
using Game.Pathfind;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TouristFindTargetSystem : GameSystemBase
{
	private struct HotelReserveAction
	{
		public Entity m_Household;

		public Entity m_Target;
	}

	[BurstCompile]
	private struct TouristFindTargetJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TouristHousehold> m_HouseholdType;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildings;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformations;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> m_LodgingProviders;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenBufs;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicleBufs;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<AddMeetingSystem.AddMeeting>.ParallelWriter m_MeetingQueue;

		public NativeQueue<HotelReserveAction>.ParallelWriter m_ReserveQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public ComponentTypeSet m_PathfindTypeSet;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (!m_PathInformations.HasComponent(entity))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, in m_PathfindTypeSet);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, entity, new PathInformation
					{
						m_State = PathFlags.Pending
					});
					PathfindParameters parameters = new PathfindParameters
					{
						m_MaxSpeed = 277.77777f,
						m_WalkSpeed = 1.6666667f,
						m_Weights = new PathfindWeights(0.1f, 0.1f, 0.1f, 0.2f),
						m_Methods = (PathMethod.PublicTransportDay | PathMethod.Taxi | PathMethod.PublicTransportNight),
						m_TaxiIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults(),
						m_PathfindFlags = (PathfindFlags.IgnoreFlow | PathfindFlags.Simplified | PathfindFlags.IgnorePath)
					};
					Entity entity2 = Entity.Null;
					for (int j = 0; j < m_HouseholdCitizenBufs[entity].Length; j++)
					{
						if (m_CurrentBuildings.HasComponent(m_HouseholdCitizenBufs[entity][j].m_Citizen))
						{
							entity2 = m_CurrentBuildings[m_HouseholdCitizenBufs[entity][j].m_Citizen].m_CurrentBuilding;
						}
					}
					SetupQueueTarget origin = new SetupQueueTarget
					{
						m_Type = SetupTargetType.CurrentLocation,
						m_Methods = PathMethod.Pedestrian,
						m_Entity = entity2
					};
					SetupQueueTarget destination = new SetupQueueTarget
					{
						m_Type = SetupTargetType.TouristFindTarget,
						m_Methods = PathMethod.Pedestrian,
						m_Entity = entity
					};
					PathUtils.UpdateOwnedVehicleMethods(entity, ref m_OwnedVehicleBufs, ref parameters, ref origin, ref destination);
					SetupQueueItem value = new SetupQueueItem(entity, parameters, origin, destination);
					m_PathfindQueue.Enqueue(value);
					continue;
				}
				PathInformation pathInformation = m_PathInformations[entity];
				if ((pathInformation.m_State & PathFlags.Pending) != 0)
				{
					continue;
				}
				Entity destination2 = pathInformation.m_Destination;
				if (destination2 != Entity.Null)
				{
					if (m_RenterBufs.HasBuffer(destination2) && m_RenterBufs[destination2].Length > 0 && m_LodgingProviders.HasComponent(m_RenterBufs[destination2][0].m_Renter))
					{
						m_ReserveQueue.Enqueue(new HotelReserveAction
						{
							m_Household = entity,
							m_Target = m_RenterBufs[pathInformation.m_Destination][0].m_Renter
						});
					}
					else
					{
						m_MeetingQueue.Enqueue(new AddMeetingSystem.AddMeeting
						{
							m_Household = entity,
							m_Type = LeisureType.Attractions
						});
					}
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Target(destination2));
				}
				else
				{
					CitizenUtils.HouseholdMoveAway(m_CommandBuffer, unfilteredChunkIndex, entity, MoveAwayReason.TouristNoTarget);
				}
				m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity);
				m_CommandBuffer.RemoveComponent<LodgingSeeker>(unfilteredChunkIndex, entity);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HotelReserveJob : IJob
	{
		public ComponentLookup<LodgingProvider> m_LodgingProviders;

		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		public BufferLookup<Renter> m_RenterBufs;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<HotelReserveAction> m_ReserveQueue;

		public void Execute()
		{
			HotelReserveAction item;
			while (m_ReserveQueue.TryDequeue(out item))
			{
				Entity entity = item.m_Target;
				Entity entity2 = item.m_Household;
				if (m_RenterBufs.HasBuffer(entity) && m_LodgingProviders.HasComponent(entity) && m_TouristHouseholds.HasComponent(entity2))
				{
					DynamicBuffer<Renter> dynamicBuffer = m_RenterBufs[entity];
					LodgingProvider value = m_LodgingProviders[entity];
					TouristHousehold value2 = m_TouristHouseholds[entity2];
					if (value.m_FreeRooms > 0)
					{
						value.m_FreeRooms--;
						m_LodgingProviders[entity] = value;
						dynamicBuffer.Add(new Renter
						{
							m_Renter = entity2
						});
						value2.m_Hotel = entity;
						m_TouristHouseholds[entity2] = value2;
						m_CommandBuffer.RemoveComponent<LodgingSeeker>(entity2);
					}
					else
					{
						m_CommandBuffer.AddComponent<LodgingSeeker>(entity2);
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentLookup;

		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RW_ComponentLookup;

		public ComponentLookup<LodgingProvider> __Game_Companies_LodgingProvider_RW_ComponentLookup;

		public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_TouristHousehold_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TouristHousehold>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Companies_LodgingProvider_RO_ComponentLookup = state.GetComponentLookup<LodgingProvider>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RW_ComponentLookup = state.GetComponentLookup<TouristHousehold>();
			__Game_Companies_LodgingProvider_RW_ComponentLookup = state.GetComponentLookup<LodgingProvider>();
			__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
		}
	}

	private EntityQuery m_SeekerQuery;

	private ComponentTypeSet m_PathfindTypes;

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private AddMeetingSystem m_AddMeetingSystem;

	private NativeQueue<HotelReserveAction> m_HotelReserveQueue;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_AddMeetingSystem = base.World.GetOrCreateSystemManaged<AddMeetingSystem>();
		m_SeekerQuery = GetEntityQuery(ComponentType.ReadWrite<TouristHousehold>(), ComponentType.ReadWrite<LodgingSeeker>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_PathfindTypes = new ComponentTypeSet(ComponentType.ReadWrite<PathInformation>());
		m_HotelReserveQueue = new NativeQueue<HotelReserveAction>(Allocator.Persistent);
		RequireForUpdate(m_SeekerQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_HotelReserveQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		TouristFindTargetJob jobData = new TouristFindTargetJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizenBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_OwnedVehicleBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathfindTypeSet = m_PathfindTypes,
			m_PathInformations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LodgingProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_MeetingQueue = m_AddMeetingSystem.GetMeetingQueue(out deps).AsParallelWriter(),
			m_ReserveQueue = m_HotelReserveQueue.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_SeekerQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
		HotelReserveJob jobData2 = new HotelReserveJob
		{
			m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LodgingProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_LodgingProvider_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferLookup, ref base.CheckedStateRef),
			m_ReserveQueue = m_HotelReserveQueue,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public TouristFindTargetSystem()
	{
	}
}
