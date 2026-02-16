// Source: Game.dll -> Game.Simulation.GameModeNaturalResourcesAdjustSystem
// Decompiled with ILSpy

using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs.Modes;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GameModeNaturalResourcesAdjustSystem : GameSystemBase
{
	[BurstCompile]
	private struct BoostInitialNaturalResourcesJob : IJobParallelFor
	{
		public CellMapData<NaturalResourceCell> m_CellData;
		public float m_BoostMultiplier;

		public void Execute(int index)
		{
			NaturalResourceCell value = m_CellData.m_Buffer[index];
			value.m_Fertility.m_Base = (ushort)math.min((int)((float)(int)value.m_Fertility.m_Base * m_BoostMultiplier), 65535);
			value.m_Ore.m_Base = (ushort)math.min((int)((float)(int)value.m_Ore.m_Base * m_BoostMultiplier), 65535);
			value.m_Oil.m_Base = (ushort)math.min((int)((float)(int)value.m_Oil.m_Base * m_BoostMultiplier), 65535);
			m_CellData.m_Buffer[index] = value;
		}
	}

	[BurstCompile]
	private struct RefillNaturalResourcesJob : IJobParallelFor
	{
		public CellMapData<NaturalResourceCell> m_CellData;
		public ModeSettingData m_GlobalData;

		public void Execute(int index)
		{
			NaturalResourceCell value = m_CellData.m_Buffer[index];
			value.m_Oil.m_Used = (ushort)math.max(0f,
				(float)(int)value.m_Oil.m_Used -
				(float)(int)value.m_Oil.m_Base * ((float)m_GlobalData.m_PercentOilRefillAmountPerDay / 100f) / (float)kUpdatesPerDay);
			value.m_Ore.m_Used = (ushort)math.max(0f,
				(float)(int)value.m_Ore.m_Used -
				(float)(int)value.m_Ore.m_Base * ((float)m_GlobalData.m_PercentOreRefillAmountPerDay / 100f) / (float)kUpdatesPerDay);
			m_CellData.m_Buffer[index] = value;
		}
	}

	public static readonly int kUpdatesPerDay = 128;

	// Only active when game mode settings enable natural resource adjustment
	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable && singleton.m_EnableAdjustNaturalResources)
		{
			if (serializationContext.purpose == Purpose.NewGame)
			{
				BoostStartGameNaturalResources(singleton.m_InitialNaturalResourceBoostMultiplier);
			}
			base.Enabled = true;
		}
		else
		{
			base.Enabled = false;
		}
	}
}
