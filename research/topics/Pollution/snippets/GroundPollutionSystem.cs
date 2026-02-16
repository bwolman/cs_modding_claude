using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GroundPollutionSystem : CellMapSystem<GroundPollution>, IJobSerializable
{
	[BurstCompile]
	private struct PollutionFadeJob : IJob
	{
		public NativeArray<GroundPollution> m_PollutionMap;

		public PollutionParameterData m_PollutionParameters;

		public RandomSeed m_Random;

		public uint m_Frame;

		public void Execute()
		{
			Unity.Mathematics.Random random = m_Random.GetRandom((int)m_Frame);
			for (int i = 0; i < m_PollutionMap.Length; i++)
			{
				GroundPollution value = m_PollutionMap[i];
				if (value.m_Pollution > 0)
				{
					value.m_Pollution = (short)math.max(0, m_PollutionMap[i].m_Pollution - MathUtils.RoundToIntRandom(ref random, (float)m_PollutionParameters.m_GroundFade / (float)kUpdatesPerDay));
				}
				m_PollutionMap[i] = value;
			}
		}
	}

	public static readonly int kTextureSize = 256;

	public static readonly int kUpdatesPerDay = 128;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_PollutionParameterGroup;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<GroundPollution>.GetCellCenter(index, kTextureSize);
	}

	public static GroundPollution GetPollution(float3 position, NativeArray<GroundPollution> pollutionMap)
	{
		GroundPollution result = default(GroundPollution);
		int2 cell = CellMapSystem<GroundPollution>.GetCell(position, CellMapSystem<GroundPollution>.kMapSize, kTextureSize);
		float2 cellCoords = CellMapSystem<GroundPollution>.GetCellCoords(position, CellMapSystem<GroundPollution>.kMapSize, kTextureSize);
		if (cell.x < 0 || cell.x >= kTextureSize || cell.y < 0 || cell.y >= kTextureSize)
		{
			return result;
		}
		GroundPollution groundPollution = pollutionMap[cell.x + kTextureSize * cell.y];
		GroundPollution groundPollution2 = ((cell.x < kTextureSize - 1) ? pollutionMap[cell.x + 1 + kTextureSize * cell.y] : default(GroundPollution));
		GroundPollution groundPollution3 = ((cell.y < kTextureSize - 1) ? pollutionMap[cell.x + kTextureSize * (cell.y + 1)] : default(GroundPollution));
		GroundPollution groundPollution4 = ((cell.x < kTextureSize - 1 && cell.y < kTextureSize - 1) ? pollutionMap[cell.x + 1 + kTextureSize * (cell.y + 1)] : default(GroundPollution));
		result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(groundPollution.m_Pollution, groundPollution2.m_Pollution, cellCoords.x - (float)cell.x), math.lerp(groundPollution3.m_Pollution, groundPollution4.m_Pollution, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
		return result;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		CreateTextures(kTextureSize);
		m_PollutionParameterGroup = GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
		RequireForUpdate(m_PollutionParameterGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PollutionFadeJob jobData = new PollutionFadeJob
		{
			m_PollutionMap = m_Map,
			m_PollutionParameters = m_PollutionParameterGroup.GetSingleton<PollutionParameterData>(),
			m_Random = RandomSeed.Next(),
			m_Frame = m_SimulationSystem.frameIndex
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies, base.Dependency));
		AddWriter(base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
	}

	[Preserve]
	public GroundPollutionSystem()
	{
	}
}
