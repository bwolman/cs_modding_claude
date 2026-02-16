// Source: Game.dll -> Game.Simulation.NaturalResourceSystem
// Decompiled with ILSpy (key sections only -- full type is ~350 lines)

using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Simulation;

[CompilerGenerated]
public class NaturalResourceSystem : CellMapSystem<NaturalResourceCell>, IJobSerializable, IPostDeserialize
{
	// --- Constants ---
	public const int MAX_BASE_RESOURCES = 10000;
	public const int FERTILITY_REGENERATION_RATE = 800;
	public const int FISH_REGENERATION_RATE = 800;
	public const int UPDATES_PER_DAY = 32;
	public const int EDITOR_ROWS_PER_TICK = 4;
	public static readonly int kTextureSize = 256;

	// --- System references ---
	public ToolSystem m_ToolSystem;
	public SimulationSystem m_SimulationSystem;
	public GroundPollutionSystem m_GroundPollutionSystem;
	public NoisePollutionSystem m_NoisePollutionSystem;
	public TerrainSystem m_TerrainSystem;
	public WaterSystem m_WaterSystem;
	public GroundWaterSystem m_GroundWaterSystem;
	public Game.Areas.SearchSystem m_AreaSearchSystem;
	public Game.Objects.SearchSystem m_ObjectSearchSystem;
	public CitySystem m_CitySystem;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		if (phase != SystemUpdatePhase.GameSimulation)
		{
			return 1;
		}
		return 8192;
	}

	// --- Initial resource distribution via Perlin noise ---
	public override JobHandle SetDefaults(Context context)
	{
		JobHandle result = base.SetDefaults(context);
		if (context.purpose == Purpose.NewGame)
		{
			result.Complete();
			float3 float4 = default(float3);
			for (int i = 0; i < m_Map.Length; i++)
			{
				float num = (float)(i % kTextureSize) / (float)kTextureSize;
				float num2 = (float)(i / kTextureSize) / (float)kTextureSize;
				float3 @float = new float3(6.1f, 13.9f, 10.7f);
				float3 float2 = num * @float;
				float3 float3 = num2 * @float;
				float4.x = Mathf.PerlinNoise(float2.x, float3.x);
				float4.y = Mathf.PerlinNoise(float2.y, float3.y);
				float4.z = Mathf.PerlinNoise(float2.z, float3.z);
				float4 = (float4 - new float3(0.4f, 0.7f, 0.7f)) * new float3(5f, 10f, 10f);
				float4 = 10000f * math.saturate(float4);
				NaturalResourceCell value = new NaturalResourceCell
				{
					m_Fertility = { m_Base = (ushort)float4.x },
					m_Ore = { m_Base = (ushort)float4.y },
					m_Oil = { m_Base = (ushort)float4.z }
				};
				m_Map[i] = value;
			}
		}
		return result;
	}

	// --- Static resource lookup methods ---
	public static NaturalResourceAmount GetFertilityAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Fertility);
	}

	public static NaturalResourceAmount GetOilAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Oil);
	}

	public static NaturalResourceAmount GetOreAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Ore);
	}

	public static NaturalResourceAmount GetFishAmount(float3 position, NativeArray<NaturalResourceCell> map)
	{
		return GetResource(position, map, (NaturalResourceCell c) => c.m_Fish);
	}

	// Bilinear interpolation for resource lookup
	private static NaturalResourceAmount GetResource(float3 position, NativeArray<NaturalResourceCell> map, Func<NaturalResourceCell, NaturalResourceAmount> getter)
	{
		float num = (float)CellMapSystem<NaturalResourceCell>.kMapSize / (float)kTextureSize;
		int2 cell = CellMapSystem<NaturalResourceCell>.GetCell(position - new float3(num / 2f, 0f, num / 2f), CellMapSystem<NaturalResourceCell>.kMapSize, kTextureSize);
		float2 cellCoords = CellMapSystem<NaturalResourceCell>.GetCellCoords(position, CellMapSystem<NaturalResourceCell>.kMapSize, kTextureSize) - new float2(0.5f, 0.5f);
		cell = math.clamp(cell, 0, kTextureSize - 2);
		NaturalResourceAmount p1 = getter(map[cell.x + kTextureSize * cell.y]);
		NaturalResourceAmount p2 = getter(map[cell.x + 1 + kTextureSize * cell.y]);
		NaturalResourceAmount p3 = getter(map[cell.x + kTextureSize * (cell.y + 1)]);
		NaturalResourceAmount p4 = getter(map[cell.x + 1 + kTextureSize * (cell.y + 1)]);
		return new NaturalResourceAmount
		{
			m_Base = FilteringValue(p1.m_Base, p2.m_Base, p3.m_Base, p4.m_Base),
			m_Used = FilteringValue(p1.m_Used, p2.m_Used, p3.m_Used, p4.m_Used)
		};

		ushort FilteringValue(ushort v1, ushort v2, ushort v3, ushort v4)
		{
			return (ushort)math.round(math.lerp(
				math.lerp((int)v1, (int)v2, cellCoords.x - (float)cell.x),
				math.lerp((int)v3, (int)v4, cellCoords.x - (float)cell.x),
				cellCoords.y - (float)cell.y));
		}
	}
}
