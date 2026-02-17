using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PropertyRenterSystem : GameSystemBase
{
	[BurstCompile]
	private struct PayRentJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoned;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_Destroyed;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

		public RandomSeed m_RandomSeed;

		public NativeQueue<ServiceFeeSystem.FeeEvent>.ParallelWriter m_FeeQueue;

		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public EntityArchetype m_RentEventArchetype;

		public bool m_ProvidedGarbageService;

		public ServiceFeeParameterData m_FeeParameters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(1 + unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			for (int i = 0; i < chunk.Count; i++)
			{
				DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (!m_SpawnableBuildingData.HasComponent(prefab))
				{
					continue;
				}
				SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingData[prefab];
				AreaType areaType = m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_AreaType;
				bool isOffice = (m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_ZoneFlags & ZoneFlags.Office) != 0;
				BuildingPropertyData buildingPropertyData = m_BuildingProperties[prefab];
				int buildingGarbageFeePerDay = m_FeeParameters.GetBuildingGarbageFeePerDay(areaType, isOffice);
				if (m_ProvidedGarbageService)
				{
					m_FeeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
					{
						m_Amount = 1f,
						m_Cost = 1f * (float)buildingGarbageFeePerDay / (float)kUpdatesPerDay,
						m_Outside = false,
						m_Resource = PlayerResource.Garbage
					});
				}
				int num = MathUtils.RoundToIntRandom(ref random, 1f * (float)buildingGarbageFeePerDay / (float)dynamicBuffer.Length) / kUpdatesPerDay;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity renter = dynamicBuffer[j].m_Renter;
					if (m_PropertyRenters.HasComponent(renter))
					{
						PropertyRenter propertyRenter = m_PropertyRenters[renter];
						int num2 = ((!m_Storages.HasComponent(renter)) ? MathUtils.RoundToIntRandom(ref random, (float)propertyRenter.m_Rent * 1f / (float)kUpdatesPerDay) : EconomyUtils.GetResources(Resource.Money, m_Resources[renter]));
						EconomyUtils.AddResources(Resource.Money, -num2, m_Resources[renter]);
						if (!m_Storages.HasComponent(renter))
						{
							EconomyUtils.AddResources(Resource.Money, -num, m_Resources[renter]);
						}
					}
				}
				bool flag = !m_Abandoned.HasComponent(nativeArray[i]) && !m_Destroyed.HasComponent(nativeArray[i]);
				bool flag2 = false;
				for (int num3 = dynamicBuffer.Length - 1; num3 >= 0; num3--)
				{
					Entity renter2 = dynamicBuffer[num3].m_Renter;
					if (!m_PropertyRenters.HasComponent(renter2))
					{
						dynamicBuffer.RemoveAt(num3);
						flag2 = true;
					}
				}
				if (dynamicBuffer.Length < buildingPropertyData.CountProperties() && !m_PropertiesOnMarket.HasComponent(nativeArray[i]) && flag && !chunk.Has<Signature>())
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(PropertyToBeOnMarket));
				}
				int num4 = buildingPropertyData.CountProperties();
				while ((dynamicBuffer.Length > 0 && !flag) || dynamicBuffer.Length > num4)
				{
					Entity renter3 = dynamicBuffer[dynamicBuffer.Length - 1].m_Renter;
					if (m_PropertyRenters.HasComponent(renter3))
					{
						m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, renter3);
					}
					dynamicBuffer.RemoveAt(dynamicBuffer.Length - 1);
					flag2 = true;
				}
				if (flag2)
				{
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_RentEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new RentersUpdated(nativeArray[i]));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct RenterMovingAwayJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity e = nativeArray[i];
				m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, e);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Renter_RW_BufferTypeHandle = state.GetBufferTypeHandle<Renter>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ServiceFeeSystem m_ServiceFeeSystem;

	private EntityQuery m_BuildingGroup;

	private EntityQuery m_GarbageFacilityGroup;

	private EntityQuery m_MovingAwayHouseholdGroup;

	private EntityArchetype m_RentEventArchetype;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_595560377_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	public static int GetUpkeep(int level, float baseUpkeep, int lotSize, AreaType areaType, ref EconomyParameterData economyParameterData, bool isStorage = false)
	{
		float num;
		switch (areaType)
		{
		case AreaType.Residential:
			return Mathf.RoundToInt(math.pow(level, economyParameterData.m_ResidentialUpkeepLevelExponent) * baseUpkeep * (float)lotSize);
		default:
			num = 1f;
			break;
		case AreaType.Industrial:
			num = economyParameterData.m_IndustrialUpkeepLevelExponent;
			break;
		case AreaType.Commercial:
			num = economyParameterData.m_CommercialUpkeepLevelExponent;
			break;
		}
		float y = num;
		return Mathf.RoundToInt(math.pow(level, y) * baseUpkeep * (float)lotSize * (isStorage ? 0.5f : 1f));
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.taxRateArrayLength && reader.context.version < Version.economyFix)
		{
			reader.Read(out Entity _);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ServiceFeeSystem = base.World.GetOrCreateSystemManaged<ServiceFeeSystem>();
		m_BuildingGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Renter>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			Any = new ComponentType[1] { ComponentType.ReadWrite<BuildingCondition>() },
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_GarbageFacilityGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_MovingAwayHouseholdGroup = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<MovingAway>(), ComponentType.ReadOnly<PropertyRenter>());
		m_RentEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<RentersUpdated>());
		RequireForUpdate(m_BuildingGroup);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		bool providedGarbageService = false;
		NativeArray<Entity> nativeArray = m_GarbageFacilityGroup.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (base.EntityManager.TryGetComponent<Building>(nativeArray[i], out var component) && !BuildingUtils.CheckOption(component, BuildingOption.Inactive))
			{
				providedGarbageService = true;
				break;
			}
		}
		nativeArray.Dispose();
		JobHandle jobHandle = JobChunkExtensions.Schedule(new RenterMovingAwayJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_MovingAwayHouseholdGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		jobHandle = JobChunkExtensions.ScheduleParallel(new PayRentJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_BuildingProperties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertiesOnMarket = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Abandoned = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Destroyed = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Storages = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RentEventArchetype = m_RentEventArchetype,
			m_RandomSeed = RandomSeed.Next(),
			m_FeeParameters = __query_595560377_0.GetSingleton<ServiceFeeParameterData>(),
			m_UpdateFrameIndex = updateFrame,
			m_ProvidedGarbageService = providedGarbageService,
			m_FeeQueue = m_ServiceFeeSystem.GetFeeQueue(out var deps).AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_BuildingGroup, JobHandle.CombineDependencies(jobHandle, deps));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_ServiceFeeSystem.AddQueueWriter(jobHandle);
		base.Dependency = jobHandle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_595560377_0 = entityQueryBuilder2.Build(ref state);
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
	public PropertyRenterSystem()
	{
	}
}
