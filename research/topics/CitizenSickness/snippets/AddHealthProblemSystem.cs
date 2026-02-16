// Full source: ~835 lines. Key logic preserved with annotations.
// Decompiled from: Game.dll -> Game.Events.AddHealthProblemSystem

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class AddHealthProblemSystem : GameSystemBase
{
	// ===== FindCitizensInBuildingJob (IJobChunk) =====
	// Iterates ALL citizens with CurrentBuilding component.
	// For each citizen whose CurrentBuilding matches m_Building:
	//   - Creates AddHealthProblem with m_Flags (e.g. InDanger, Trapped, Sick)
	//   - If m_DeathProbability > 0: rolls death chance, adds Dead | RequireTransport flags
	//   - Enqueues to m_AddQueue for processing by AddHealthProblemJob
	//
	// Used in two contexts:
	//   1. Ignite events on buildings → flags = InDanger, deathProb = 0
	//   2. Destroy events on buildings → flags = Trapped, deathProb = m_BuildingDestoryDeathRate

	[BurstCompile]
	private struct FindCitizensInBuildingJob : IJobChunk
	{
		[ReadOnly] public Entity m_Event;
		[ReadOnly] public Entity m_Building;
		[ReadOnly] public HealthProblemFlags m_Flags;
		[ReadOnly] public float m_DeathProbability;
		[ReadOnly] public RandomSeed m_RandomSeed;
		[ReadOnly] public EntityTypeHandle m_EntityType;
		[ReadOnly] public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;
		[ReadOnly] public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
		[ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
		public ParallelWriter<AddHealthProblem> m_AddQueue;
		public ParallelWriter<StatisticsEvent> m_StatisticsEventQueue;
		public ParallelWriter<TriggerAction> m_TriggerBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
			NativeArray<CurrentBuilding> buildings = chunk.GetNativeArray(ref m_CurrentBuildingType);
			NativeArray<HouseholdMember> households = chunk.GetNativeArray(ref m_HouseholdMemberType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < buildings.Length; i++)
			{
				if (buildings[i].m_CurrentBuilding == m_Building)
				{
					var problem = new AddHealthProblem
					{
						m_Event = m_Event,
						m_Target = entities[i],
						m_Flags = m_Flags
					};
					if (m_DeathProbability > 0f && random.NextFloat(1f) < m_DeathProbability)
					{
						problem.m_Flags |= HealthProblemFlags.Dead | HealthProblemFlags.RequireTransport;
						Entity household = (households.Length != 0) ? households[i].m_Household : Entity.Null;
						DeathCheckSystem.PerformAfterDeathActions(entities[i], household, m_TriggerBuffer, m_StatisticsEventQueue, ref m_HouseholdCitizens);
					}
					m_AddQueue.Enqueue(problem);
				}
			}
		}
	}

	// ===== AddHealthProblemJob (IJob) =====
	// Processes both direct AddHealthProblem event entities AND the queued results from
	// FindCitizensInBuildingJob. For each target citizen:
	//   - If already has HealthProblem: merges flags (Dead takes priority, then RequireTransport)
	//   - If new: adds HealthProblem component via EntityCommandBuffer
	//   - Dead/Injured + RequireTransport: stops citizen movement (clears pathfinding)
	//   - Fires trigger events: CitizenGotSick, CitizenGotInjured, CitizenGotTrapped, CitizenGotInDanger
	//   - Creates journal data for Sick/Dead/Injured

	// ===== OnUpdate =====
	// 1. Iterates Ignite events targeting buildings → FindCitizensInBuildingJob(InDanger, deathProb=0)
	// 2. Iterates Destroy events targeting buildings → FindCitizensInBuildingJob(Trapped, deathProb=m_BuildingDestoryDeathRate)
	// 3. Schedules AddHealthProblemJob to process all results + direct AddHealthProblem events

	// Citizen query: [Citizen, CurrentBuilding], excluding [Deleted]
	// Event query: [Event] AND any of [AddHealthProblem, Ignite, Destroy]

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// m_AddHealthProblemQuery: Event + any of (AddHealthProblem, Ignite, Destroy)
		// m_CitizenQuery: Citizen + CurrentBuilding, excluding Deleted
		// m_HealthcareSettingsQuery: HealthcareParameterData singleton
	}
}
