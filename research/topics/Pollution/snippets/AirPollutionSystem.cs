using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AirPollutionSystem : CellMapSystem<AirPollution>, IJobSerializable
{
	[BurstCompile]
	private struct AirPollutionMoveJob : IJob
	{
		public NativeArray<AirPollution> m_PollutionMap;

		[ReadOnly]
		public NativeArray<Wind> m_WindMap;

		public PollutionParameterData m_PollutionParameters;

		public RandomSeed m_Random;

		public uint m_Frame;

		public void Execute()
		{
			NativeArray<AirPollution> nativeArray = new NativeArray<AirPollution>(m_PollutionMap.Length, Allocator.Temp);
			Random random = m_Random.GetRandom((int)m_Frame);
			for (int i = 0; i < m_PollutionMap.Length; i++)
			{
				float3 cellCenter = GetCellCenter(i);
				Wind wind = WindSystem.GetWind(cellCenter, m_WindMap);
				short pollution = GetPollution(cellCenter - m_PollutionParameters.m_WindAdvectionSpeed * new float3(wind.m_Wind.x, 0f, wind.m_Wind.y), m_PollutionMap).m_Pollution;
				nativeArray[i] = new AirPollution
				{
					m_Pollution = pollution
				};
			}
			float value = (float)m_PollutionParameters.m_AirFade / (float)kUpdatesPerDay;
			for (int j = 0; j < kTextureSize; j++)
			{
				for (int k = 0; k < kTextureSize; k++)
				{
					int num = j * kTextureSize + k;
					int pollution2 = nativeArray[num].m_Pollution;
					pollution2 += ((k > 0) ? (nativeArray[num - 1].m_Pollution >> kSpread) : 0);
					pollution2 += ((k < kTextureSize - 1) ? (nativeArray[num + 1].m_Pollution >> kSpread) : 0);
					pollution2 += ((j > 0) ? (nativeArray[num - kTextureSize].m_Pollution >> kSpread) : 0);
					pollution2 += ((j < kTextureSize - 1) ? (nativeArray[num + kTextureSize].m_Pollution >> kSpread) : 0);
					pollution2 -= (nativeArray[num].m_Pollution >> kSpread - 2) + MathUtils.RoundToIntRandom(ref random, value);
					pollution2 = math.clamp(pollution2, 0, 32767);
					m_PollutionMap[num] = new AirPollution
					{
						m_Pollution = (short)pollution2
					};
				}
			}
			nativeArray.Dispose();
		}
	}

	private static readonly int kSpread = 3;

	public static readonly int kTextureSize = 256;

	public static readonly int kUpdatesPerDay = 128;

	private WindSystem m_WindSystem;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_PollutionParameterQuery;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<AirPollution>.GetCellCenter(index, kTextureSize);
	}

	public static AirPollution GetPollution(float3 position, NativeArray<AirPollution> pollutionMap)
	{
		AirPollution result = default(AirPollution);
		float num = (float)CellMapSystem<AirPollution>.kMapSize / (float)kTextureSize;
		int2 cell = CellMapSystem<AirPollution>.GetCell(position - new float3(num / 2f, 0f, num / 2f), CellMapSystem<AirPollution>.kMapSize, kTextureSize);
		float2 @float = CellMapSystem<AirPollution>.GetCellCoords(position, CellMapSystem<AirPollution>.kMapSize, kTextureSize) - new float2(0.5f, 0.5f);
		cell = math.clamp(cell, 0, kTextureSize - 2);
		short pollution = pollutionMap[cell.x + kTextureSize * cell.y].m_Pollution;
		short pollution2 = pollutionMap[cell.x + 1 + kTextureSize * cell.y].m_Pollution;
		short pollution3 = pollutionMap[cell.x + kTextureSize * (cell.y + 1)].m_Pollution;
		short pollution4 = pollutionMap[cell.x + 1 + kTextureSize * (cell.y + 1)].m_Pollution;
		result.m_Pollution = (short)math.round(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
		return result;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		CreateTextures(kTextureSize);
		m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PollutionParameterQuery = GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
		RequireForUpdate(m_PollutionParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		AirPollutionMoveJob jobData = new AirPollutionMoveJob
		{
			m_PollutionMap = m_Map,
			m_WindMap = m_WindSystem.GetMap(readOnly: true, out dependencies),
			m_PollutionParameters = m_PollutionParameterQuery.GetSingleton<PollutionParameterData>(),
			m_Random = RandomSeed.Next(),
			m_Frame = m_SimulationSystem.frameIndex
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(dependencies, m_WriteDependencies, m_ReadDependencies, base.Dependency));
		m_WindSystem.AddReader(base.Dependency);
		AddWriter(base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
	}

	[Preserve]
	public AirPollutionSystem()
	{
	}
}
