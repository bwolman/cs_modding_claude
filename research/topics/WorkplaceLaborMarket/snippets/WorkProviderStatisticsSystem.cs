using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WorkProviderStatisticsSystem : GameSystemBase
{
	[BurstCompile]
	private struct CountSeniorWorkplacesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public ComponentTypeHandle<FreeWorkplaces> m_FreeWorkplacesType;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		public int m_SeniorEmployeeLevel;

		public NativeAccumulator<AverageFloat>.ParallelWriter m_FreeSeniorWorkplaces;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<WorkProvider> nativeArray2 = chunk.GetNativeArray(ref m_WorkProviderType);
			NativeArray<FreeWorkplaces> nativeArray3 = chunk.GetNativeArray(ref m_FreeWorkplacesType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity prefab = nativeArray[i].m_Prefab;
				WorkProvider workProvider = nativeArray2[i];
				if (m_WorkplaceDatas.TryGetComponent(prefab, out var componentData))
				{
					SpawnableBuildingData componentData2;
					int buildingLevel = ((!m_SpawnableBuildingDatas.TryGetComponent(prefab, out componentData2)) ? 1 : componentData2.m_Level);
					Workplaces workplaces = EconomyUtils.CalculateNumberOfWorkplaces(workProvider.m_MaxWorkers, componentData.m_Complexity, buildingLevel);
					FreeWorkplaces freeWorkplaces = ((nativeArray3.Length != 0) ? nativeArray3[i] : default(FreeWorkplaces));
					for (int j = m_SeniorEmployeeLevel; j < 5; j++)
					{
						m_FreeSeniorWorkplaces.Accumulate(new AverageFloat
						{
							m_Count = freeWorkplaces.GetFree(j),
							m_Total = workplaces[j]
						});
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct StatisticsJob : IJob
	{
		public NativeAccumulator<AverageFloat> m_FreeSeniorWorkplaces;

		public NativeQueue<StatisticsEvent> m_StatisticsEventQueue;

		public void Execute()
		{
			m_StatisticsEventQueue.Enqueue(new StatisticsEvent
			{
				m_Statistic = StatisticType.SeniorWorkerInDemandPercentage,
				m_Change = 100f * m_FreeSeniorWorkplaces.GetResult().average
			});
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FreeWorkplaces> __Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
			__Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FreeWorkplaces>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
		}
	}

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_WorkProviderQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1149970541_0;

	private EntityQuery __query_1149970541_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 8192;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_WorkProviderQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<WorkProvider>(),
				ComponentType.ReadOnly<Employee>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_WorkProviderQuery);
		RequireForUpdate<WorkProviderParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PoliceConfigurationData singleton = __query_1149970541_0.GetSingleton<PoliceConfigurationData>();
		if (!base.EntityManager.HasEnabledComponent<Locked>(singleton.m_PoliceServicePrefab))
		{
			NativeAccumulator<AverageFloat> freeSeniorWorkplaces = new NativeAccumulator<AverageFloat>(Allocator.TempJob);
			CountSeniorWorkplacesJob jobData = new CountSeniorWorkplacesJob
			{
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FreeWorkplacesType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SeniorEmployeeLevel = __query_1149970541_1.GetSingleton<WorkProviderParameterData>().m_SeniorEmployeeLevel,
				m_FreeSeniorWorkplaces = freeSeniorWorkplaces.AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_WorkProviderQuery, base.Dependency);
			JobHandle deps;
			StatisticsJob jobData2 = new StatisticsJob
			{
				m_FreeSeniorWorkplaces = freeSeniorWorkplaces,
				m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps)
			};
			base.Dependency = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, deps));
			m_CityStatisticsSystem.AddWriter(base.Dependency);
			freeSeniorWorkplaces.Dispose(base.Dependency);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<PoliceConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1149970541_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<WorkProviderParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1149970541_1 = entityQueryBuilder2.Build(ref state);
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
	public WorkProviderStatisticsSystem()
	{
	}
}
