using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Pathfind;
using Game.Prefabs;
using Game.Triggers;
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
public class FindSchoolSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindSchoolJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<SchoolSeeker> m_SchoolSeekerType;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<Household> m_Households;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<SchoolSeeker> nativeArray3 = chunk.GetNativeArray(ref m_SchoolSeekerType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity owner = nativeArray2[i].m_Owner;
				if (!m_Citizens.HasComponent(owner))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(Deleted));
					continue;
				}
				Citizen citizen = m_Citizens[owner];
				Entity household = m_HouseholdMembers[owner].m_Household;
				if (m_PropertyRenters.HasComponent(household))
				{
					Entity entity = nativeArray[i];
					Entity property = m_PropertyRenters[household].m_Property;
					int level = nativeArray3[i].m_Level;
					Entity entity2 = Entity.Null;
					if (m_CurrentDistrictData.HasComponent(property))
					{
						entity2 = m_CurrentDistrictData[property].m_District;
					}
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new PathInformation
					{
						m_State = PathFlags.Pending
					});
					Household household2 = m_Households[household];
					DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[household];
					PathfindParameters parameters = new PathfindParameters
					{
						m_MaxSpeed = 111.111115f,
						m_WalkSpeed = 1.6666667f,
						m_Weights = CitizenUtils.GetPathfindWeights(citizen, household2, dynamicBuffer.Length),
						m_Methods = (PathMethod.Pedestrian | PathMethod.PublicTransportDay),
						m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost,
						m_PathfindFlags = (PathfindFlags.Simplified | PathfindFlags.IgnorePath)
					};
					SetupQueueTarget origin = new SetupQueueTarget
					{
						m_Type = SetupTargetType.CurrentLocation,
						m_Methods = PathMethod.Pedestrian
					};
					SetupQueueTarget destination = new SetupQueueTarget
					{
						m_Type = SetupTargetType.SchoolSeekerTo,
						m_Methods = PathMethod.Pedestrian,
						m_Value = level,
						m_Entity = entity2
					};
					if (citizen.GetAge() != CitizenAge.Child)
					{
						PathUtils.UpdateOwnedVehicleMethods(household, ref m_OwnedVehicles, ref parameters, ref origin, ref destination);
					}
					SetupQueueItem value = new SetupQueueItem(entity, parameters, origin, destination);
					m_PathfindQueue.Enqueue(value);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct StartStudyingJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<SchoolSeeker> m_SchoolSeekerType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInfoType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Citizen> m_Citizens;

		public BufferLookup<Game.Buildings.Student> m_StudentBuffers;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_SchoolData;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMembers;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdDatas;

		[ReadOnly]
		public BufferLookup<Efficiency> m_Efficiencies;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_Fees;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<Resources> m_Resources;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		public BufferLookup<Employee> m_Employees;

		public NativeQueue<TriggerAction> m_TriggerBuffer;

		public uint m_SimulationFrame;

		public Entity m_City;

		public EconomyParameterData m_EconomyParameters;

		public TimeData m_TimeData;

		public EntityCommandBuffer m_CommandBuffer;

		public RandomSeed m_RandomSeed;

		public bool m_DebugFastFindSchool;

		public void Execute()
		{
			m_RandomSeed.GetRandom((int)m_SimulationFrame);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Owner> nativeArray = archetypeChunk.GetNativeArray(ref m_OwnerType);
				NativeArray<PathInformation> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PathInfoType);
				NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<SchoolSeeker> nativeArray4 = archetypeChunk.GetNativeArray(ref m_SchoolSeekerType);
				_ = m_CityModifiers[m_City];
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_State & PathFlags.Pending) != 0)
					{
						continue;
					}
					Entity e = nativeArray3[j];
					Entity owner = nativeArray[j].m_Owner;
					bool flag = false;
					if (m_Citizens.HasComponent(owner) && !m_Deleteds.HasComponent(owner))
					{
						Entity destination = nativeArray2[j].m_Destination;
						if (m_Prefabs.HasComponent(destination) && m_StudentBuffers.HasBuffer(destination))
						{
							DynamicBuffer<Game.Buildings.Student> dynamicBuffer = m_StudentBuffers[destination];
							Entity prefab = m_Prefabs[destination].m_Prefab;
							if (m_SchoolData.HasComponent(prefab))
							{
								SchoolData data = m_SchoolData[prefab];
								if (m_InstalledUpgrades.HasBuffer(destination))
								{
									UpgradeUtils.CombineStats(ref data, m_InstalledUpgrades[destination], ref m_Prefabs, ref m_SchoolData);
								}
								int studentCapacity = data.m_StudentCapacity;
								if (dynamicBuffer.Length < studentCapacity)
								{
									dynamicBuffer.Add(new Game.Buildings.Student
									{
										m_Student = owner
									});
									m_CommandBuffer.AddComponent(owner, new Game.Citizens.Student
									{
										m_School = destination,
										m_LastCommuteTime = nativeArray2[j].m_Duration,
										m_Level = (byte)nativeArray4[j].m_Level
									});
									if (m_Workers.HasComponent(owner))
									{
										Entity workplace = m_Workers[owner].m_Workplace;
										if (m_Employees.HasBuffer(workplace))
										{
											DynamicBuffer<Employee> dynamicBuffer2 = m_Employees[workplace];
											for (int k = 0; k < dynamicBuffer2.Length; k++)
											{
												if (dynamicBuffer2[k].m_Worker == owner)
												{
													dynamicBuffer2.RemoveAtSwapBack(k);
													break;
												}
											}
										}
										m_CommandBuffer.RemoveComponent<Worker>(owner);
									}
									m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenStartedSchool, Entity.Null, owner, destination));
									Citizen value = m_Citizens[owner];
									value.SetFailedEducationCount(0);
									m_Citizens[owner] = value;
									flag = true;
									m_CommandBuffer.RemoveComponent<SchoolSeekerCooldown>(owner);
								}
							}
						}
						if (!flag)
						{
							m_CommandBuffer.AddComponent(owner, new SchoolSeekerCooldown
							{
								m_SimulationFrame = m_SimulationFrame
							});
						}
					}
					m_CommandBuffer.RemoveComponent<HasSchoolSeeker>(owner);
					m_CommandBuffer.AddComponent(e, default(Deleted));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SchoolSeeker> __Game_Citizens_SchoolSeeker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

		public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RW_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		public BufferLookup<Employee> __Game_Companies_Employee_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_SchoolSeeker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SchoolSeeker>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Citizens_Citizen_RW_ComponentLookup = state.GetComponentLookup<Citizen>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
			__Game_Buildings_Student_RW_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>();
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Companies_Employee_RW_BufferLookup = state.GetBufferLookup<Employee>();
		}
	}

	public bool debugFastFindSchool;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private SimulationSystem m_SimulationSystem;

	private CitySystem m_CitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_SchoolSeekerQuery;

	private EntityQuery m_ResultsQuery;

	private TriggerSystem m_TriggerSystem;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_17488131_0;

	private EntityQuery __query_17488131_1;

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
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_SchoolSeekerQuery = GetEntityQuery(ComponentType.ReadWrite<SchoolSeeker>(), ComponentType.ReadOnly<Owner>(), ComponentType.Exclude<PathInformation>(), ComponentType.Exclude<Deleted>());
		m_ResultsQuery = GetEntityQuery(ComponentType.ReadWrite<SchoolSeeker>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PathInformation>(), ComponentType.Exclude<Deleted>());
		RequireAnyForUpdate(m_SchoolSeekerQuery, m_ResultsQuery);
		RequireForUpdate<EconomyParameterData>();
		RequireForUpdate<TimeData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_SchoolSeekerQuery.IsEmptyIgnoreFilter)
		{
			FindSchoolJob jobData = new FindSchoolJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_SchoolSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_SchoolSeeker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Households = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_SchoolSeekerQuery, base.Dependency);
			m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		}
		if (!m_ResultsQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			StartStudyingJob jobData2 = new StartStudyingJob
			{
				m_Chunks = m_ResultsQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_SchoolSeekerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_SchoolSeeker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PathInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SchoolData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StudentBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Student_RW_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_Fees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Efficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
				m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
				m_HouseholdMembers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Deleteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RW_BufferLookup, ref base.CheckedStateRef),
				m_City = m_CitySystem.City,
				m_EconomyParameters = __query_17488131_0.GetSingleton<EconomyParameterData>(),
				m_TimeData = __query_17488131_1.GetSingleton<TimeData>(),
				m_RandomSeed = RandomSeed.Next(),
				m_SimulationFrame = m_SimulationSystem.frameIndex,
				m_DebugFastFindSchool = debugFastFindSchool,
				m_TriggerBuffer = m_TriggerSystem.CreateActionBuffer(),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
			};
			base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
			m_TriggerSystem.AddActionBufferWriter(base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_17488131_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<TimeData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_17488131_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public FindSchoolSystem()
	{
	}
}
