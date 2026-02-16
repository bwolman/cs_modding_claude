// Full source: ~579 lines. Key logic preserved with annotations.
// Decompiled from: Game.dll -> Game.Simulation.CrimeCheckSystem

using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CrimeCheckSystem : GameSystemBase
{
	[BurstCompile]
	private struct CrimeCheckJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EventPrefabChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<Criminal> m_CriminalType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceConfigurationData;

		[ReadOnly]
		public bool m_DebugFullCrimeMode;

		public RandomSeed m_RandomSeed;

		public Entity m_City;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			// Only processes citizens matching current UpdateFrame (1 update per day, 16 partitions)
			// For each citizen (excluding Children and Elderly):
			//   If not already a criminal with active event: calls TryAddCrime
		}

		private void TryAddCrime(int jobIndex, ref Random random, Entity entity, Citizen citizen, bool isCriminal, Entity household, Entity property, DynamicBuffer<CityModifier> cityModifiers)
		{
			// Crime probability based on wellbeing:
			//   If wellbeing <= 25: t = wellbeing / 25  (linear 0..1)
			//   If wellbeing > 25:  t = ((100 - wellbeing) / 75)^2  (quadratic falloff)
			//
			// For each CrimeData prefab with RandomTargetType == Citizen:
			//   First-time: probability = lerp(occurrence.min, occurrence.max, t)
			//   Repeat:     probability = lerp(recurrence.min, recurrence.max, t)
			//   Modified by CityModifier.CrimeProbability
			//
			// Population scaling:
			//   randomRange = max(population / m_CrimePopulationReduction * 100, 100)
			//   Roll: random.NextFloat(randomRange) < probability
			//
			// Welfare coverage check (repeat criminals only):
			//   If property has welfare coverage and random < coverage * m_WelfareCrimeRecurrenceFactor: skip

			float num;
			if (citizen.m_WellBeing <= 25)
			{
				num = (float)(int)citizen.m_WellBeing / 25f;
			}
			else
			{
				num = (float)(100 - citizen.m_WellBeing) / 75f;
				num *= num;
			}
			// ... iterates CrimeData prefabs, rolls probability, calls CreateCrimeEvent
		}

		private void CreateCrimeEvent(int jobIndex, Entity targetEntity, Entity eventPrefab, EventData eventData)
		{
			// Creates a crime event entity using the CrimeData prefab's archetype
			// The archetype includes: Game.Events.Crime (tag) + TargetElement (buffer)
			// Sets PrefabRef to the crime event prefab
			// Adds the criminal citizen as a TargetElement
			// Fires TriggerType.CitizenCommitedCrime

			Entity val = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, val, new PrefabRef(eventPrefab));
			m_CommandBuffer.SetBuffer<TargetElement>(jobIndex, val).Add(new TargetElement(targetEntity));
		}
	}

	public readonly int kUpdatesPerDay = 1; // Once per game day

	public bool debugFullCrimeMode; // When true, bypasses probability check

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16); // = 16384 frames
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// Citizen query: Citizen + UpdateFrame, excluding HealthProblem + Worker + Student + Deleted + Temp
		//   (only unemployed, non-student, healthy citizens are checked)
		// Event query: CrimeData, excluding Locked
		// PoliceConfiguration query: PoliceConfigurationData, excluding Locked
	}
}
