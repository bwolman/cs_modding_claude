using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class GroundWaterPollutionSystem : GameSystemBase
{
	[BurstCompile]
	private struct PolluteGroundWaterJob : IJob
	{
		public NativeArray<GroundWater> m_GroundWaterMap;

		[ReadOnly]
		public NativeArray<GroundPollution> m_PollutionMap;

		public void Execute()
		{
			for (int i = 0; i < m_GroundWaterMap.Length; i++)
			{
				GroundWater value = m_GroundWaterMap[i];
				GroundPollution pollution = GroundPollutionSystem.GetPollution(GroundWaterSystem.GetCellCenter(i), m_PollutionMap);
				if (pollution.m_Pollution > 0)
				{
					value.m_Polluted = (short)math.min(value.m_Amount, value.m_Polluted + pollution.m_Pollution / 200);
					m_GroundWaterMap[i] = value;
				}
			}
		}
	}

	private GroundWaterSystem m_GroundWaterSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		PolluteGroundWaterJob jobData = new PolluteGroundWaterJob
		{
			m_GroundWaterMap = m_GroundWaterSystem.GetMap(readOnly: false, out dependencies),
			m_PollutionMap = m_GroundPollutionSystem.GetMap(readOnly: true, out dependencies2)
		};
		base.Dependency = jobData.Schedule(JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
		m_GroundWaterSystem.AddWriter(base.Dependency);
		m_GroundPollutionSystem.AddReader(base.Dependency);
	}

	[Preserve]
	public GroundWaterPollutionSystem()
	{
	}
}
