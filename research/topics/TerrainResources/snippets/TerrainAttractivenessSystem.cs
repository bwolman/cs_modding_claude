// Source: Game.dll -> Game.Simulation.TerrainAttractivenessSystem
// Decompiled with ILSpy (key sections)

using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
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
public class TerrainAttractivenessSystem : CellMapSystem<TerrainAttractiveness>, IJobSerializable
{
	[BurstCompile]
	private struct TerrainAttractivenessPrepareJob : IJobParallelForBatch
	{
		[ReadOnly] public TerrainHeightData m_TerrainData;
		[ReadOnly] public WaterSurfaceData<SurfaceWater> m_WaterData;
		[ReadOnly] public CellMapData<ZoneAmbienceCell> m_ZoneAmbienceData;
		public NativeArray<float3> m_AttractFactorData;

		public void Execute(int startIndex, int count)
		{
			for (int i = startIndex; i < startIndex + count; i++)
			{
				float3 cellCenter = GetCellCenter(i);
				// x = water depth, y = terrain height, z = forest ambience
				m_AttractFactorData[i] = new float3(
					WaterUtils.SampleDepth(ref m_WaterData, cellCenter),
					TerrainUtils.SampleHeight(ref m_TerrainData, cellCenter),
					ZoneAmbienceSystem.GetZoneAmbience(GroupAmbienceType.Forest, cellCenter, m_ZoneAmbienceData.m_Buffer, 1f));
			}
		}
	}

	[BurstCompile]
	private struct TerrainAttractivenessJob : IJobParallelForBatch
	{
		[ReadOnly] public NativeArray<float3> m_AttractFactorData;
		[ReadOnly] public float m_Scale;
		public NativeArray<TerrainAttractiveness> m_AttractivenessMap;
		public AttractivenessParameterData m_AttractivenessParameters;

		public void Execute(int startIndex, int count)
		{
			for (int i = startIndex; i < startIndex + count; i++)
			{
				float3 cellCenter = GetCellCenter(i);
				float2 bonuses = 0;
				int num = Mathf.CeilToInt(math.max(
					m_AttractivenessParameters.m_ForestDistance,
					m_AttractivenessParameters.m_ShoreDistance) / m_Scale);

				// Scan neighbors within distance to find forest/shore proximity
				for (int j = -num; j <= num; j++)
				{
					for (int k = -num; k <= num; k++)
					{
						int idx = /* clamped neighbor index */;
						float3 factors = m_AttractFactorData[idx];
						float dist = math.distance(GetCellCenter(idx), cellCenter);
						// Forest: linear falloff by distance, weighted by zone ambience
						bonuses.x = math.max(bonuses.x,
							math.saturate(1f - dist / m_AttractivenessParameters.m_ForestDistance) * factors.z);
						// Shore: binary (water depth > 2), linear falloff by distance
						bonuses.y = math.max(bonuses.y,
							math.saturate(1f - dist / m_AttractivenessParameters.m_ShoreDistance) *
							((factors.x > 2f) ? 1f : 0f));
					}
				}

				m_AttractivenessMap[i] = new TerrainAttractiveness
				{
					m_ForestBonus = bonuses.x,
					m_ShoreBonus = bonuses.y
				};
			}
		}
	}

	public static readonly int kTextureSize = 128;
	public static readonly int kUpdatesPerDay = 16;

	// Evaluates total attractiveness from all terrain factors
	public static float EvaluateAttractiveness(float terrainHeight, TerrainAttractiveness attractiveness, AttractivenessParameterData parameters)
	{
		float forest = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
		float shore = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
		float height = math.min(parameters.m_HeightBonus.z,
			math.max(0f, terrainHeight - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
		return forest + shore + height;
	}
}
