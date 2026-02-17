using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TouristSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpawnTouristHouseholdJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_PrefabEntities;

		[ReadOnly]
		public NativeList<ArchetypeData> m_Archetypes;

		[ReadOnly]
		public NativeList<HouseholdData> m_HouseholdPrefabs;

		[ReadOnly]
		public NativeList<Entity> m_OutsideConnectionEntities;

		[ReadOnly]
		public ComponentLookup<Tourism> m_Tourisms;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public AttractivenessParameterData m_AttractivenessParameter;

		[ReadOnly]
		public DemandParameterData m_DemandParameterData;

		[ReadOnly]
		public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_StatisticsLookup;

		[ReadOnly]
		public BufferLookup<CityStatistic> m_CityStatistics;

		public RandomSeed m_RandomSeed;

		public Entity m_City;

		public uint m_Frame;

		public EntityCommandBuffer m_CommandBuffer;

		public ClimateSystem.WeatherClassification m_WeatherClassification;

		public float m_Temperature;

		public float m_Precipitation;

		public bool m_IsRaining;

		public bool m_IsSnowing;

		public void Execute()
		{
			if (!m_Tourisms.HasComponent(m_City))
			{
				return;
			}
			Random random = m_RandomSeed.GetRandom((int)m_Frame);
			int attractiveness = m_Tourisms[m_City].m_Attractiveness;
			int statisticValue = CityStatisticsSystem.GetStatisticValue(m_StatisticsLookup, m_CityStatistics, StatisticType.TouristCount);
			float touristProbability = TourismSystem.GetTouristProbability(m_AttractivenessParameter, attractiveness, statisticValue, m_WeatherClassification, m_Temperature, m_Precipitation, m_IsRaining, m_IsSnowing);
			if (random.NextFloat() < touristProbability)
			{
				int index = random.NextInt(m_HouseholdPrefabs.Length);
				Entity prefab = m_PrefabEntities[index];
				ArchetypeData archetypeData = m_Archetypes[index];
				Entity e = m_CommandBuffer.CreateEntity(archetypeData.m_Archetype);
				PrefabRef component = new PrefabRef
				{
					m_Prefab = prefab
				};
				m_CommandBuffer.SetComponent(e, component);
				Household component2 = new Household
				{
					m_Flags = HouseholdFlags.Tourist
				};
				m_CommandBuffer.SetComponent(e, component2);
				m_CommandBuffer.AddComponent(e, new TouristHousehold
				{
					m_Hotel = Entity.Null,
					m_LeavingTime = 0u
				});
				if (m_OutsideConnectionEntities.Length > 0 && BuildingUtils.GetRandomOutsideConnectionByParameters(ref m_OutsideConnectionEntities, ref m_OutsideConnectionDatas, ref m_PrefabRefs, random, m_DemandParameterData.m_TouristOCSpawnParameters, out var result))
				{
					CurrentBuilding component3 = new CurrentBuilding
					{
						m_CurrentBuilding = result
					};
					m_CommandBuffer.AddComponent(e, component3);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Tourism> __Game_City_Tourism_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(isReadOnly: true);
		}
	}

	private EntityQuery m_HouseholdPrefabQuery;

	private EntityQuery m_OutsideConnectionQuery;

	private EntityQuery m_AttractivenessParameterQuery;

	private EntityQuery m_DemandParameterQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private ClimateSystem m_ClimateSystem;

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

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
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_HouseholdPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ArchetypeData>(), ComponentType.ReadOnly<HouseholdData>());
		m_OutsideConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Game.Objects.ElectricityOutsideConnection>(), ComponentType.Exclude<Game.Objects.WaterPipeOutsideConnection>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		m_AttractivenessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
		RequireForUpdate(m_HouseholdPrefabQuery);
		RequireForUpdate(m_OutsideConnectionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		JobHandle outJobHandle2;
		JobHandle outJobHandle3;
		JobHandle outJobHandle4;
		SpawnTouristHouseholdJob jobData = new SpawnTouristHouseholdJob
		{
			m_PrefabEntities = m_HouseholdPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_Archetypes = m_HouseholdPrefabQuery.ToComponentDataListAsync<ArchetypeData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
			m_HouseholdPrefabs = m_HouseholdPrefabQuery.ToComponentDataListAsync<HouseholdData>(base.World.UpdateAllocator.ToAllocator, out outJobHandle3),
			m_OutsideConnectionEntities = m_OutsideConnectionQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle4),
			m_Tourisms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Tourism_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttractivenessParameter = m_AttractivenessParameterQuery.GetSingleton<AttractivenessParameterData>(),
			m_DemandParameterData = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
			m_WeatherClassification = m_ClimateSystem.classification,
			m_Temperature = m_ClimateSystem.temperature,
			m_Precipitation = m_ClimateSystem.precipitation,
			m_IsRaining = m_ClimateSystem.isRaining,
			m_IsSnowing = m_ClimateSystem.isSnowing,
			m_StatisticsLookup = m_CityStatisticsSystem.GetLookup(),
			m_CityStatistics = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_Frame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(outJobHandle, outJobHandle2, JobHandle.CombineDependencies(outJobHandle3, base.Dependency, outJobHandle4)));
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
	public TouristSpawnSystem()
	{
	}
}
