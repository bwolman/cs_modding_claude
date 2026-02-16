// Full source: ~800 lines. Key logic summarized below with critical code paths preserved.
// Decompiled from: Game.dll -> Game.Simulation.FireSimulationSystem

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Events;
using Game.Notifications;
using Game.Objects;
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
public class FireSimulationSystem : GameSystemBase
{
	// ===== FireSimulationJob (IJobChunk) =====
	// Processes entities with OnFire component. Two responsibilities:
	//
	// 1. DAMAGE ESCALATION:
	//    - If structural damage (m_Damage.y) >= 1.0: fire decays at 2x escalation rate
	//    - Otherwise: fire intensity increases by escalation rate per tick
	//    - Intensity clamped to [0, 100]
	//    - Structural damage per tick = min(0.5, intensity * dt / structuralIntegrity)
	//    - When total damage reaches 1.0: creates Destroy event, shows burned-down notification
	//    - When intensity drops to 0: removes OnFire, removes fire notification
	//    - Sets building Efficiency.Fire factor to 0 while burning
	//    - dt constant: 1.0666667f (64 frames / 60 fps)
	//
	// 2. RESCUE REQUEST:
	//    - InitializeRequestFrame(): calculates delay before requesting fire rescue
	//      - Base: random in [ResponseTimeRange.min, ResponseTimeRange.max] (default 3-30 seconds)
	//      - Darkness modifier: +10% at night (m_DarknessResponseTimeModifier)
	//      - Telecom modifier: -15% with good coverage (m_TelecomResponseTimeModifier)
	//      - District modifier: BuildingFireResponseTime
	//      - Tree modifier: ForestFireResponseTime local effect
	//      - Converted to frames: (seconds * 60) - 32 - 128
	//    - RequestFireRescueIfNeeded(): when frame threshold reached, creates FireRescueRequest
	//      with RequestGroup(4) and shows fire notification icon

	// ===== FireSpreadCheckJob (IJobChunk) =====
	// For each burning entity, attempts to spread fire to nearby objects.
	//
	// Spread probability per tick: intensity * sqrt(spreadProbability * 0.01) * dt
	// If roll succeeds:
	//   - Searches ObjectSearchTree for buildings/trees within spreadRange
	//   - For each candidate: calculates fire hazard (building or tree path)
	//   - Spread chance per target: fireHazard * (range - distance) * probability / (100 * range)
	//   - Creates Ignite event with startIntensity and requestFrame + 64
	//
	// ObjectSpreadIterator:
	//   - Uses NativeQuadTree<Entity, QuadTreeBoundsXZ> for spatial search
	//   - Building path: checks distance minus half building size
	//   - Tree path: checks distance minus half tree size
	//   - Both use FireHazardData.GetFireHazard() for hazard calculation

	private const uint UPDATE_INTERVAL = 64u;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64; // Every 64 frames
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// Query: OnFire, excluding Deleted and Temp
		// Creates archetypes for:
		//   - FireRescueRequest (ServiceRequest + FireRescueRequest + RequestGroup)
		//   - DamageEvent (Event + Damage)
		//   - DestroyEvent (Event + Destroy)
		//   - IgniteEvent (Event + Ignite)
	}

	[Preserve]
	protected override void OnUpdate()
	{
		// Schedules FireSimulationJob then FireSpreadCheckJob sequentially.
		// FireSpreadCheckJob depends on FireSimulationJob completing.
		// Both read FireConfigurationData singleton and FireHazardData.
	}
}
