using Colossal.Serialization.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class TrafficAmbienceSystem : CellMapSystem<TrafficAmbienceCell>, IJobSerializable
{
	[BurstCompile]
	private struct TrafficAmbienceUpdateJob : IJobParallelFor
	{
		public NativeArray<TrafficAmbienceCell> m_TrafficMap;

		public void Execute(int index)
		{
			TrafficAmbienceCell trafficAmbienceCell = m_TrafficMap[index];
			m_TrafficMap[index] = new TrafficAmbienceCell
			{
				m_Traffic = trafficAmbienceCell.m_Accumulator,
				m_Accumulator = 0f
			};
		}
	}

	public static readonly int kTextureSize = 64;

	public static readonly int kUpdatesPerDay = 1024;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<TrafficAmbienceCell>.GetCellCenter(index, kTextureSize);
	}

	public static TrafficAmbienceCell GetTrafficAmbience2(float3 position, NativeArray<TrafficAmbienceCell> trafficAmbienceMap, float maxPerCell)
	{
		TrafficAmbienceCell result = default(TrafficAmbienceCell);
		int2 cell = CellMapSystem<TrafficAmbienceCell>.GetCell(position, CellMapSystem<TrafficAmbienceCell>.kMapSize, kTextureSize);
		float num = 0f;
		float num2 = 0f;
		for (int i = cell.x - 2; i <= cell.x + 2; i++)
		{
			for (int j = cell.y - 2; j <= cell.y + 2; j++)
			{
				if (i >= 0 && i < kTextureSize && j >= 0 && j < kTextureSize)
				{
					int index = i + kTextureSize * j;
					float num3 = math.max(1f, math.distancesq(GetCellCenter(index), position));
					num += math.min(maxPerCell, trafficAmbienceMap[index].m_Traffic) / num3;
					num2 += 1f / num3;
				}
			}
		}
		result.m_Traffic = num / num2;
		return result;
	}

	public static TrafficAmbienceCell GetTrafficAmbience(float3 position, NativeArray<TrafficAmbienceCell> trafficAmbienceMap)
	{
		TrafficAmbienceCell result = default(TrafficAmbienceCell);
		int2 cell = CellMapSystem<TrafficAmbienceCell>.GetCell(position, CellMapSystem<TrafficAmbienceCell>.kMapSize, kTextureSize);
		if (cell.x < 0 || cell.x >= kTextureSize || cell.y < 0 || cell.y >= kTextureSize)
		{
			return new TrafficAmbienceCell
			{
				m_Accumulator = 0f,
				m_Traffic = 0f
			};
		}
		float2 cellCoords = CellMapSystem<TrafficAmbienceCell>.GetCellCoords(position, CellMapSystem<TrafficAmbienceCell>.kMapSize, kTextureSize);
		float traffic = trafficAmbienceMap[cell.x + kTextureSize * cell.y].m_Traffic;
		float end = ((cell.x < kTextureSize - 1) ? trafficAmbienceMap[cell.x + 1 + kTextureSize * cell.y].m_Traffic : 0f);
		float start = ((cell.y < kTextureSize - 1) ? trafficAmbienceMap[cell.x + kTextureSize * (cell.y + 1)].m_Traffic : 0f);
		float end2 = ((cell.x < kTextureSize - 1 && cell.y < kTextureSize - 1) ? trafficAmbienceMap[cell.x + 1 + kTextureSize * (cell.y + 1)].m_Traffic : 0f);
		result.m_Traffic = math.lerp(math.lerp(traffic, end, cellCoords.x - (float)cell.x), math.lerp(start, end2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
		return result;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		CreateTextures(kTextureSize);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TrafficAmbienceUpdateJob jobData = new TrafficAmbienceUpdateJob
		{
			m_TrafficMap = m_Map
		};
		base.Dependency = IJobParallelForExtensions.Schedule(jobData, kTextureSize * kTextureSize, kTextureSize, JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies, base.Dependency));
		AddWriter(base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
	}

	[Preserve]
	public TrafficAmbienceSystem()
	{
	}
}
