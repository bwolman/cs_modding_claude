#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GroundWaterSystem : CellMapSystem<GroundWater>, IJobSerializable
{
	[BurstCompile]
	private struct GroundWaterTickJob : IJob
	{
		public NativeArray<GroundWater> m_GroundWaterMap;

		public WaterPipeParameterData m_Parameters;

		private void HandlePollution(int index, int otherIndex, NativeArray<int2> tmp)
		{
			GroundWater groundWater = m_GroundWaterMap[index];
			GroundWater groundWater2 = m_GroundWaterMap[otherIndex];
			ref int2 reference = ref tmp.ElementAt(index);
			ref int2 reference2 = ref tmp.ElementAt(otherIndex);
			int num = groundWater.m_Polluted + groundWater2.m_Polluted;
			int num2 = groundWater.m_Amount + groundWater2.m_Amount;
			int num3 = math.clamp((((num2 > 0) ? (groundWater.m_Amount * num / num2) : 0) - groundWater.m_Polluted) / 4, -(groundWater2.m_Amount - groundWater2.m_Polluted) / 4, (groundWater.m_Amount - groundWater.m_Polluted) / 4);
			reference.y += num3;
			reference2.y -= num3;
			Assert.IsTrue(0 <= groundWater.m_Polluted + reference.y);
			Assert.IsTrue(groundWater.m_Polluted + reference.y <= groundWater.m_Amount);
			Assert.IsTrue(0 <= groundWater2.m_Polluted + reference2.y);
			Assert.IsTrue(groundWater2.m_Polluted + reference2.y <= groundWater2.m_Amount);
		}

		private void HandleFlow(int index, int otherIndex, NativeArray<int2> tmp)
		{
			GroundWater groundWater = m_GroundWaterMap[index];
			GroundWater groundWater2 = m_GroundWaterMap[otherIndex];
			ref int2 reference = ref tmp.ElementAt(index);
			ref int2 reference2 = ref tmp.ElementAt(otherIndex);
			Assert.IsTrue(groundWater2.m_Polluted + reference2.y <= groundWater2.m_Amount + reference2.x);
			Assert.IsTrue(groundWater.m_Polluted + reference.y <= groundWater.m_Amount + reference.x);
			float num = ((groundWater.m_Amount + reference.x != 0) ? (1f * (float)(groundWater.m_Polluted + reference.y) / (float)(groundWater.m_Amount + reference.x)) : 0f);
			float num2 = ((groundWater2.m_Amount + reference2.x != 0) ? (1f * (float)(groundWater2.m_Polluted + reference2.y) / (float)(groundWater2.m_Amount + reference2.x)) : 0f);
			int num3 = groundWater.m_Amount - groundWater.m_Max;
			int num4 = math.clamp((groundWater2.m_Amount - groundWater2.m_Max - num3) / 4, -groundWater.m_Amount / 4, groundWater2.m_Amount / 4);
			reference.x += num4;
			reference2.x -= num4;
			int num5 = 0;
			if (num4 > 0)
			{
				num5 = (int)((float)num4 * num2);
			}
			else if (num4 < 0)
			{
				num5 = (int)((float)num4 * num);
			}
			reference.y += num5;
			reference2.y -= num5;
			Assert.IsTrue(0 <= groundWater.m_Amount + reference.x);
			Assert.IsTrue(groundWater.m_Amount + reference.x <= groundWater.m_Max);
			Assert.IsTrue(0 <= groundWater2.m_Amount + reference2.x);
			Assert.IsTrue(groundWater2.m_Amount + reference2.x <= groundWater2.m_Max);
			Assert.IsTrue(0 <= groundWater.m_Polluted + reference.y);
			Assert.IsTrue(groundWater.m_Polluted + reference.y <= groundWater.m_Amount + reference.x);
			Assert.IsTrue(0 <= groundWater2.m_Polluted + reference2.y);
			Assert.IsTrue(groundWater2.m_Polluted + reference2.y <= groundWater2.m_Amount + reference2.x);
		}

		public void Execute()
		{
			NativeArray<int2> tmp = new NativeArray<int2>(m_GroundWaterMap.Length, Allocator.Temp);
			for (int i = 0; i < m_GroundWaterMap.Length; i++)
			{
				int num = i % kTextureSize;
				int num2 = i / kTextureSize;
				if (num < kTextureSize - 1)
				{
					HandlePollution(i, i + 1, tmp);
				}
				if (num2 < kTextureSize - 1)
				{
					HandlePollution(i, i + kTextureSize, tmp);
				}
			}
			for (int j = 0; j < m_GroundWaterMap.Length; j++)
			{
				int num3 = j % kTextureSize;
				int num4 = j / kTextureSize;
				if (num3 < kTextureSize - 1)
				{
					HandleFlow(j, j + 1, tmp);
				}
				if (num4 < kTextureSize - 1)
				{
					HandleFlow(j, j + kTextureSize, tmp);
				}
			}
			for (int k = 0; k < m_GroundWaterMap.Length; k++)
			{
				GroundWater value = m_GroundWaterMap[k];
				value.m_Amount = (short)math.min(value.m_Amount + tmp[k].x + Mathf.CeilToInt(m_Parameters.m_GroundwaterReplenish * (float)value.m_Max), value.m_Max);
				value.m_Polluted = (short)math.clamp(value.m_Polluted + tmp[k].y - m_Parameters.m_GroundwaterPurification, 0, value.m_Amount);
				m_GroundWaterMap[k] = value;
			}
			tmp.Dispose();
		}
	}

	public const int kMaxGroundWater = 10000;

	public const int kMinGroundWaterThreshold = 500;

	public static readonly int kTextureSize = 256;

	private EntityQuery m_ParameterQuery;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 128;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 64;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<GroundWater>.GetCellCenter(index, kTextureSize);
	}

	public static bool TryGetCell(float3 position, out int2 cell)
	{
		cell = CellMapSystem<GroundWater>.GetCell(position, CellMapSystem<GroundWater>.kMapSize, kTextureSize);
		return IsValidCell(cell);
	}

	public static bool IsValidCell(int2 cell)
	{
		if (cell.x >= 0 && cell.y >= 0 && cell.x < kTextureSize)
		{
			return cell.y < kTextureSize;
		}
		return false;
	}

	public static GroundWater GetGroundWater(float3 position, NativeArray<GroundWater> groundWaterMap)
	{
		float2 @float = CellMapSystem<GroundWater>.GetCellCoords(position, CellMapSystem<GroundWater>.kMapSize, kTextureSize) - new float2(0.5f, 0.5f);
		int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
		int2 cell2 = new int2(cell.x + 1, cell.y);
		int2 cell3 = new int2(cell.x, cell.y + 1);
		int2 cell4 = new int2(cell.x + 1, cell.y + 1);
		GroundWater groundWater = GetGroundWater(groundWaterMap, cell);
		GroundWater groundWater2 = GetGroundWater(groundWaterMap, cell2);
		GroundWater groundWater3 = GetGroundWater(groundWaterMap, cell3);
		GroundWater groundWater4 = GetGroundWater(groundWaterMap, cell4);
		float sx = @float.x - (float)cell.x;
		float sy = @float.y - (float)cell.y;
		return new GroundWater
		{
			m_Amount = (short)math.round(Bilinear(groundWater.m_Amount, groundWater2.m_Amount, groundWater3.m_Amount, groundWater4.m_Amount, sx, sy)),
			m_Polluted = (short)math.round(Bilinear(groundWater.m_Polluted, groundWater2.m_Polluted, groundWater3.m_Polluted, groundWater4.m_Polluted, sx, sy)),
			m_Max = (short)math.round(Bilinear(groundWater.m_Max, groundWater2.m_Max, groundWater3.m_Max, groundWater4.m_Max, sx, sy))
		};
	}

	public static void ConsumeGroundWater(float3 position, NativeArray<GroundWater> groundWaterMap, int amount)
	{
		Assert.IsTrue(amount >= 0);
		float2 @float = CellMapSystem<GroundWater>.GetCellCoords(position, CellMapSystem<GroundWater>.kMapSize, kTextureSize) - new float2(0.5f, 0.5f);
		int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
		int2 cell2 = new int2(cell.x + 1, cell.y);
		int2 cell3 = new int2(cell.x, cell.y + 1);
		int2 cell4 = new int2(cell.x + 1, cell.y + 1);
		GroundWater gw = GetGroundWater(groundWaterMap, cell);
		GroundWater gw2 = GetGroundWater(groundWaterMap, cell2);
		GroundWater gw3 = GetGroundWater(groundWaterMap, cell3);
		GroundWater gw4 = GetGroundWater(groundWaterMap, cell4);
		float sx = @float.x - (float)cell.x;
		float sy = @float.y - (float)cell.y;
		float num = math.ceil(Bilinear(gw.m_Amount, 0, 0, 0, sx, sy));
		float num2 = math.ceil(Bilinear(0, gw2.m_Amount, 0, 0, sx, sy));
		float num3 = math.ceil(Bilinear(0, 0, gw3.m_Amount, 0, sx, sy));
		float num4 = math.ceil(Bilinear(0, 0, 0, gw4.m_Amount, sx, sy));
		float totalAvailable = num + num2 + num3 + num4;
		float totalConsumed = math.min(amount, totalAvailable);
		if (totalAvailable < (float)amount)
		{
			UnityEngine.Debug.LogWarning($"Trying to consume more groundwater than available! amount: {amount}, available: {totalAvailable}");
		}
		ConsumeFraction(ref gw, num);
		ConsumeFraction(ref gw2, num2);
		ConsumeFraction(ref gw3, num3);
		ConsumeFraction(ref gw4, num4);
		Assert.IsTrue(Mathf.Approximately(totalAvailable, 0f));
		Assert.IsTrue(Mathf.Approximately(totalConsumed, 0f));
		SetGroundWater(groundWaterMap, cell, gw);
		SetGroundWater(groundWaterMap, cell2, gw2);
		SetGroundWater(groundWaterMap, cell3, gw3);
		SetGroundWater(groundWaterMap, cell4, gw4);
		void ConsumeFraction(ref GroundWater reference, float cellAvailable)
		{
			if (!(totalAvailable < 0.5f))
			{
				float num5 = cellAvailable / totalAvailable;
				totalAvailable -= cellAvailable;
				float num6 = math.max(y: math.max(0f, totalConsumed - totalAvailable), x: math.round(num5 * totalConsumed));
				Assert.IsTrue(num6 <= (float)reference.m_Amount);
				reference.Consume((int)num6);
				totalConsumed -= num6;
			}
		}
	}

	private static GroundWater GetGroundWater(NativeArray<GroundWater> groundWaterMap, int2 cell)
	{
		if (!IsValidCell(cell))
		{
			return default(GroundWater);
		}
		return groundWaterMap[cell.x + kTextureSize * cell.y];
	}

	private static void SetGroundWater(NativeArray<GroundWater> groundWaterMap, int2 cell, GroundWater gw)
	{
		if (IsValidCell(cell))
		{
			groundWaterMap[cell.x + kTextureSize * cell.y] = gw;
		}
	}

	private static float Bilinear(short v00, short v10, short v01, short v11, float sx, float sy)
	{
		return math.lerp(math.lerp(v00, v10, sx), math.lerp(v01, v11, sx), sy);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		CreateTextures(kTextureSize);
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		GroundWaterTickJob jobData = new GroundWaterTickJob
		{
			m_GroundWaterMap = m_Map,
			m_Parameters = m_ParameterQuery.GetSingleton<WaterPipeParameterData>()
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies, base.Dependency));
		AddWriter(base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
	}

	public override JobHandle SetDefaults(Context context)
	{
		if (context.purpose == Purpose.NewGame && context.version < Version.timoSerializationFlow)
		{
			for (int i = 0; i < m_Map.Length; i++)
			{
				float num = (float)(i % kTextureSize) / (float)kTextureSize;
				float num2 = (float)(i / kTextureSize) / (float)kTextureSize;
				short num3 = (short)Mathf.RoundToInt(10000f * math.saturate((Mathf.PerlinNoise(32f * num, 32f * num2) - 0.6f) / 0.4f));
				GroundWater value = new GroundWater
				{
					m_Amount = num3,
					m_Max = num3
				};
				m_Map[i] = value;
			}
			return default(JobHandle);
		}
		return base.SetDefaults(context);
	}

	[Preserve]
	public GroundWaterSystem()
	{
	}
}
