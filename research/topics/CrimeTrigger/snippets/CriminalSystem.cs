// Full source: ~950 lines. Key logic preserved with annotations.
// Decompiled from: Game.dll -> Game.Simulation.CriminalSystem

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Events;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CriminalSystem : GameSystemBase
{
	// Internal data struct for queuing crime effects (theft + wellbeing impact)
	private struct CrimeData
	{
		public Entity m_Source;   // Household or company being robbed / citizen affected
		public Entity m_Target;   // Criminal's household (receives stolen money)
		public int m_StealAmount; // Money to transfer from source to target
		public int m_EffectAmount; // CrimeVictim wellbeing impact on source citizens
	}

	[BurstCompile]
	private struct CriminalJob : IJobChunk
	{
		// ===== Criminal Lifecycle State Machine =====
		//
		// State: Flags == 0
		//   → Remove Criminal component (no longer a criminal)
		//
		// State: Prisoner
		//   → In prison: decrement m_JailTime each tick
		//   → When JailTime == 0: clear flags, remove Criminal component
		//   → If prison is Inactive: release immediately
		//
		// State: Arrested (no Sentenced)
		//   → In police station: decrement m_JailTime each tick
		//   → When JailTime == 0: roll m_PrisonProbability
		//     → If wins: set Sentenced flag, compute prison time, fire CitizenGotSentencedToPrison
		//     → If loses: clear Arrested, release
		//   → If police station is Inactive: release immediately
		//
		// State: Arrested + Sentenced
		//   → In police station waiting for prisoner transport
		//   → If transport available (PublicTransport vehicle at target, Boarding): GoToPrison
		//   → If no transport: decrement JailTime, release when 0
		//
		// State: Planning
		//   → Add TripNeeded with Purpose.Crime (sends citizen traveling to crime target)
		//   → Roll CriminalMonitorProbability → may set Monitored flag
		//   → Transition: Planning → Preparing
		//
		// State: Preparing
		//   → Waiting for citizen to reach crime destination (has TripNeeded Purpose.Crime)
		//   → When TripNeeded is consumed (arrived): clear Preparing
		//
		// State: Robber (at destination, not planning/preparing/arrested)
		//   → If citizen is in a building with AccidentSite:
		//     → If site is Secured and police car at target: GoToJail (arrested)
		//     → If site has CrimeScene but not secured: commit crime (steal money, apply effects)
		//       → GetCrimeSource: random Renter in building
		//       → GetStealAmount: absolute + relative income from victim's money
		//       → AddCrimeEffects: m_HomeCrimeEffect on household members, m_WorkplaceCrimeEffect on employees
		//       → TryEscape: clear trips, add Purpose.Escape
		//   → If in building with no AccidentSite: AddCrimeScene

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			// Processes citizens with Criminal component matching current UpdateFrame
			// Dispatches to appropriate state handler based on m_Flags
		}

		private void AddCrimeScene(int jobIndex, Entity _event, Entity building)
		{
			// Creates AddAccidentSite event entity to mark building as a crime scene
			AddAccidentSite component = new AddAccidentSite
			{
				m_Event = _event,
				m_Target = building,
				m_Flags = AccidentSiteFlags.CrimeScene
			};
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_AddAccidentSiteArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component);
		}

		private void GoToJail(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs, Entity vehicle)
		{
			// Adds TripNeeded with Purpose.GoingToJail targeting the police car
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.GoingToJail,
				m_TargetAgent = vehicle
			});
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
		}

		private void GoToPrison(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs, Entity vehicle)
		{
			// Adds TripNeeded with Purpose.GoingToPrison targeting the transport vehicle
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.GoingToPrison,
				m_TargetAgent = vehicle
			});
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
		}

		private void TryEscape(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs)
		{
			// Criminal escapes after committing crime (if not arrested)
			tripNeededs.Clear();
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.Escape
			});
			m_CommandBuffer.RemoveComponent<ResourceBuyer>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<AttendingMeeting>(jobIndex, entity);
		}

		private int GetStealAmount(ref Random random, Entity source, Game.Prefabs.CrimeData crimeData)
		{
			// Calculates money stolen from the crime source (household/company)
			// = relative% of source's money + absolute random amount
			float num = 0f;
			if (m_Resources.HasBuffer(source))
			{
				DynamicBuffer<Resources> resources = m_Resources[source];
				int money = EconomyUtils.GetResources(Resource.Money, resources);
				if (money > 0)
				{
					num += math.lerp(crimeData.m_CrimeIncomeRelative.min, crimeData.m_CrimeIncomeRelative.max, random.NextFloat(1f)) * (float)money;
				}
				num += math.lerp(crimeData.m_CrimeIncomeAbsolute.min, crimeData.m_CrimeIncomeAbsolute.max, random.NextFloat(1f));
			}
			return (int)num;
		}

		private void AddCrimeEffects(Entity source)
		{
			// Queues CrimeVictim effects for citizens affected by crime:
			//   - Household members of robbed entity: m_HomeCrimeEffect
			//   - Employees of robbed entity: m_WorkplaceCrimeEffect
		}
	}

	// ===== CrimeJob (IJob) =====
	// Processes queued CrimeData after CriminalJob completes:
	//   - Transfers money from source to criminal's household (m_StealAmount)
	//   - Applies CrimeVictim.m_Effect to affected citizens (cumulative, max 255)
	//   - CrimeVictim is an IEnableableComponent: enabled when crime effect active

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16; // Very frequent updates (every 16 frames)
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// Criminal query: Citizen + UpdateFrame + Criminal, excluding Deleted + Temp
		// PoliceConfig query: PoliceConfigurationData
		// AddAccidentSite archetype: Event + AddAccidentSite
	}
}
