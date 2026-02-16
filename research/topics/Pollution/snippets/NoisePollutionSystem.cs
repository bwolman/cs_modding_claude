using Colossal.Serialization.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class NoisePollutionSystem : CellMapSystem<NoisePollution>, IJobSerializable
{
	[BurstCompile]
	private struct NoisePollutionSwapJob : IJobParallelFor
	{
		[NativeDisableParallelForRestriction]
		public NativeArray<NoisePollution> m_PollutionMap;

		public void Execute(int index)
		{
			NoisePollution value = m_PollutionMap[index];
			int num = index % kTextureSize;
			int num2 = index / kTextureSize;
			short num3 = (short)((num > 0) ? m_PollutionMap[index - 1].m_PollutionTemp : 0);
			short num4 = (short)((num < kTextureSize - 1) ? m_PollutionMap[index + 1].m_PollutionTemp : 0);
			short num5 = (short)((num2 > 0) ? m_PollutionMap[index - kTextureSize].m_PollutionTemp : 0);
			short num6 = (short)((num2 < kTextureSize - 1) ? m_PollutionMap[index + kTextureSize].m_PollutionTemp : 0);
			short num7 = (short)((num > 0 && num2 > 0) ? m_PollutionMap[index - 1 - kTextureSize].m_PollutionTemp : 0);
			short num8 = (short)((num < kTextureSize - 1 && num2 > 0) ? m_PollutionMap[index + 1 - kTextureSize].m_PollutionTemp : 0);
			short num9 = (short)((num > 0 && num2 < kTextureSize - 1) ? m_PollutionMap[index - 1 + kTextureSize].m_PollutionTemp : 0);
			short num10 = (short)((num < kTextureSize - 1 && num2 < kTextureSize - 1) ? m_PollutionMap[index + 1 + kTextureSize].m_PollutionTemp : 0);
			value.m_Pollution = (short)(value.m_PollutionTemp / 4 + (num3 + num4 + num5 + num6) / 8 + (num7 + num8 + num9 + num10) / 16);
			m_PollutionMap[index] = value;
		}
	}

	[BurstCompile]
	private struct NoisePollutionClearJob : IJobParallelFor
	{
		public NativeArray<NoisePollution> m_PollutionMap;

		public void Execute(int index)
		{
			NoisePollution value = m_PollutionMap[index];
			value.m_PollutionTemp = 0;
			m_PollutionMap[index] = value;
		}
	}

	public static readonly int kTextureSize = 256;

	public static readonly int kUpdatesPerDay = 128;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<NoisePollution>.GetCellCenter(index, kTextureSize);
	}

	public static NoisePollution GetPollution(float3 position, NativeArray<NoisePollution> pollutionMap)
	{
		NoisePollution result = default(NoisePollution);
		float num = (float)CellMapSystem<NoisePollution>.kMapSize / (float)kTextureSize;
		int2 cell = CellMapSystem<NoisePollution>.GetCell(position - new float3(num / 2f, 0f, num / 2f), CellMapSystem<NoisePollution>.kMapSize, kTextureSize);
		float2 @float = CellMapSystem<NoisePollution>.GetCellCoords(position, CellMapSystem<NoisePollution>.kMapSize, kTextureSize) - new float2(0.5f, 0.5f);
		cell = math.clamp(cell, 0, kTextureSize - 2);
		short pollution = pollutionMap[cell.x + kTextureSize * cell.y].m_Pollution;
		short pollution2 = pollutionMap[cell.x + 1 + kTextureSize * cell.y].m_Pollution;
		short pollution3 = pollutionMap[cell.x + kTextureSize * (cell.y + 1)].m_Pollution;
		short pollution4 = pollutionMap[cell.x + 1 + kTextureSize * (cell.y + 1)].m_Pollution;
		result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
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
		JobHandle dependencies;
		NoisePollutionSwapJob jobData = new NoisePollutionSwapJob
		{
			m_PollutionMap = GetMap(readOnly: false, out dependencies)
		};
		dependencies = new NoisePollutionClearJob
		{
			m_PollutionMap = jobData.m_PollutionMap
		}.Schedule(dependsOn: IJobParallelForExtensions.Schedule(jobData, m_Map.Length, 4, dependencies), arrayLength: m_Map.Length, innerloopBatchCount: 64);
		AddWriter(dependencies);
	}

	[Preserve]
	public NoisePollutionSystem()
	{
	}
}
