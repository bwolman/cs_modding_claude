using System;
using System.Runtime.CompilerServices;
using Game.City;
using Game.Economy;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class XPAccumulationSystem : GameSystemBase
{
	[BurstCompile]
	private struct XPAccumulateJob : IJob
	{
		[ReadOnly]
		public XPParameterData m_XPParameters;

		[ReadOnly]
		public ComponentLookup<Population> m_CityPopulations;

		[ReadOnly]
		public BufferLookup<CityStatistic> m_CityStatistics;

		[ReadOnly]
		public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_StatsLookup;

		public ComponentLookup<XP> m_CityXPs;

		public NativeQueue<XPGain> m_XPQueue;

		[ReadOnly]
		public Entity m_City;

		public void Execute()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_014a: Unknown result type (might be due to invalid IL or missing references)
			//IL_014f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0199: Unknown result type (might be due to invalid IL or missing references)
			//IL_019e: Unknown result type (might be due to invalid IL or missing references)
			Population population = m_CityPopulations[m_City];
			if (population.m_Population >= 10)
			{
				XP xP = m_CityXPs[m_City];
				int num = math.max(0, population.m_Population - xP.m_MaximumPopulation);
				xP.m_MaximumPopulation = Math.Max(xP.m_MaximumPopulation, population.m_Population);
				int num2 = 0;
				for (int i = 0; i < 5; i++)
				{
					num2 = CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.ResidentialTaxableIncome, i);
				}
				ResourceIterator iterator = ResourceIterator.GetIterator();
				while (iterator.Next())
				{
					num2 += CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.CommercialTaxableIncome, (int)iterator.resource) + CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.IndustrialTaxableIncome, (int)iterator.resource) + CityStatisticsSystem.GetStatisticValue(m_StatsLookup, m_CityStatistics, StatisticType.OfficeTaxableIncome, (int)iterator.resource);
				}
				int num3 = num2 / 10;
				xP.m_MaximumIncome = Math.Max(xP.m_MaximumIncome, num3);
				m_CityXPs[m_City] = xP;
				m_XPQueue.Enqueue(new XPGain
				{
					amount = Mathf.FloorToInt(m_XPParameters.m_XPPerPopulation * (float)num / (float)kUpdatesPerDay),
					entity = Entity.Null,
					reason = XPReason.Population
				});
				m_XPQueue.Enqueue(new XPGain
				{
					amount = Mathf.FloorToInt(m_XPParameters.m_XPPerHappiness * (float)population.m_AverageHappiness / (float)kUpdatesPerDay),
					entity = Entity.Null,
					reason = XPReason.Happiness
				});
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		public ComponentLookup<XP> __Game_City_XP_RW_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			__Game_City_Population_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Population>(true);
			__Game_City_XP_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<XP>(false);
			__Game_City_CityStatistic_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<CityStatistic>(true);
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private XPSystem m_XPSystem;

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_XPSettingsQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_XPSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<XPSystem>();
		m_CitySystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CitySystem>();
		m_CityStatisticsSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_XPSettingsQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<XPParameterData>() });
		((ComponentSystemBase)this).RequireForUpdate(m_XPSettingsQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		JobHandle deps;
		XPAccumulateJob xPAccumulateJob = new XPAccumulateJob
		{
			m_CityPopulations = InternalCompilerInterface.GetComponentLookup<Population>(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_CityXPs = InternalCompilerInterface.GetComponentLookup<XP>(ref __TypeHandle.__Game_City_XP_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_XPParameters = ((EntityQuery)(ref m_XPSettingsQuery)).GetSingleton<XPParameterData>(),
			m_CityStatistics = InternalCompilerInterface.GetBufferLookup<CityStatistic>(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_StatsLookup = m_CityStatisticsSystem.GetLookup(),
			m_City = m_CitySystem.City,
			m_XPQueue = m_XPSystem.GetQueue(out deps)
		};
		((SystemBase)this).Dependency = IJobExtensions.Schedule<XPAccumulateJob>(xPAccumulateJob, JobHandle.CombineDependencies(((SystemBase)this).Dependency, deps));
		m_XPSystem.AddQueueWriter(((SystemBase)this).Dependency);
	}

	[MethodImpl((MethodImplOptions)256)]
	private void __AssignQueries(ref SystemState state)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		((EntityQueryBuilder)(ref val)).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		((ComponentSystemBase)this).OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public XPAccumulationSystem()
	{
	}
}
