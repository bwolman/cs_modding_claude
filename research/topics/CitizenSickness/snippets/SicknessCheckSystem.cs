using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Events;
using Game.Prefabs;
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
public class SicknessCheckSystem : GameSystemBase
{
	[BurstCompile]
	private struct SicknessCheckJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EventPrefabChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Citizen> m_CitizenType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblems;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public RandomSeed m_RandomSeed;

		public EntityArchetype m_AddProblemArchetype;

		public Entity m_City;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			// Only processes citizens matching current UpdateFrame (1 update per day, 16 partitions)
			// For each healthy citizen (no HealthProblem component):
			//   Calls TryAddHealthProblem
		}

		private void TryAddHealthProblem(int jobIndex, ref Random random, Entity entity, Citizen citizen, Entity household, DynamicBuffer<CityModifier> cityModifiers)
		{
			// Sickness probability curve:
			//   t = saturate(pow(2, 10 - health * 0.1) * 0.001)
			//   At health=255: t ≈ 0 (virtually immune)
			//   At health=100: t ≈ 0.001 (very low chance)
			//   At health=50:  t ≈ 0.032
			//   At health=10:  t ≈ 0.5
			//   At health=0:   t = 1.0 (guaranteed)
			//
			// For each HealthEventData prefab with RandomTargetType == Citizen:
			//   probability = lerp(min, max, t)
			//   For Disease type: modified by CityModifier.DiseaseProbability
			//   Roll: random.NextFloat(100) < probability
			//
			// If roll succeeds: CreateHealthEvent

			float t = math.saturate(math.pow(2f, 10f - (float)(int)citizen.m_Health * 0.1f) * 0.001f);
			// ... iterates HealthEventData prefabs, rolls probability
		}

		private void CreateHealthEvent(int jobIndex, ref Random random, Entity targetEntity, Entity eventPrefab, Entity household, Citizen citizen, EventData eventData, HealthEventData healthData)
		{
			// If healthData.m_RequireTracking: creates full event entity with archetype + TargetElement
			// Otherwise: creates lightweight AddHealthProblem command entity

			// Maps HealthEventType to HealthProblemFlags:
			//   Disease -> Sick
			//   Injury  -> Injured
			//   Death   -> Dead

			// Transport probability (needs ambulance):
			//   prob = lerp(transportMax, transportMin, health * 0.01)
			//   Higher health = closer to transportMin (less likely to need transport)

			// NoHealthcare flag:
			//   threshold = 10/health - fee/2 * income
			//   If random < threshold: adds NoHealthcare (citizen won't seek hospital)

			HealthProblemFlags flags = HealthProblemFlags.None;
			switch (healthData.m_HealthEventType)
			{
			case HealthEventType.Disease:
				flags |= HealthProblemFlags.Sick;
				break;
			case HealthEventType.Injury:
				flags |= HealthProblemFlags.Injured;
				break;
			case HealthEventType.Death:
				flags |= HealthProblemFlags.Dead;
				break;
			}

			float transportProb = math.lerp(healthData.m_TransportProbability.max, healthData.m_TransportProbability.min, (float)(int)citizen.m_Health * 0.01f);
			if (random.NextFloat(100f) < transportProb)
			{
				flags |= HealthProblemFlags.RequireTransport;
			}

			// Creates AddHealthProblem entity
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_AddProblemArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new AddHealthProblem
			{
				m_Event = Entity.Null,
				m_Target = targetEntity,
				m_Flags = flags
			});
		}
	}

	public readonly int kUpdatesPerDay = 1; // Once per game day

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16); // = 16384 frames
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// Citizen query: Citizen + UpdateFrame, excluding HealthProblem + Deleted + Temp
		//   (only healthy citizens are checked)
		// Event query: HealthEventData, excluding Locked
		// AddProblem archetype: Event + AddHealthProblem
	}
}
