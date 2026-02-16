// Full source: ~400 lines. Key logic preserved with annotations.
// Decompiled from: Game.dll -> Game.Simulation.CrimeAccumulationSystem

using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CrimeAccumulationSystem : GameSystemBase
{
	[BurstCompile]
	private struct CrimeAccumulationJob : IJobChunk
	{
		// ===== Crime Accumulation on Buildings =====
		// Iterates buildings with CrimeProducer component.
		// Each building accumulates crime based on:
		//   - Zone's m_CrimeRate (from CrimeAccumulationData on zone prefab)
		//   - Police coverage on the building's road edge
		//   - District/city modifiers (CityModifierType.CrimeAccumulation)
		//
		// Formula:
		//   crimeIncrease = crimeRate * (10 - policeCoverage) / 10 * m_CrimePoliceCoverageFactor
		//   Modified by district and city CrimeAccumulation modifiers
		//   Clamped to [0, m_MaxCrimeAccumulation]
		//
		// When m_Crime > m_CrimeAccumulationTolerance:
		//   Creates PolicePatrolRequest if none exists
		//   (triggers police dispatch to the building)

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentLookup<CrimeAccumulationData> m_CrimeAccumulationData;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceConfigurationData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			// For each building with CrimeProducer:
			//   1. Look up zone prefab's CrimeAccumulationData.m_CrimeRate
			//   2. Get police coverage from building's road edge
			//   3. Calculate crime increase with police coverage factor
			//   4. Apply district/city modifiers
			//   5. Accumulate into CrimeProducer.m_Crime (clamped)
			//   6. If crime > tolerance: create PolicePatrolRequest
		}
	}

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / 16; // = 16384 frames (once per game day / 16)
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// Building query: CrimeProducer + Building + PrefabRef + UpdateFrame, excluding Deleted + Temp
		// PoliceConfiguration query: PoliceConfigurationData
	}
}
