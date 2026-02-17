// Decompiled from Game.dll using ilspycmd v9.1
// Full source for Game.Simulation.PolicePatrolDispatchSystem
// Handles patrol dispatch -- DOES NOT set CarFlags.Emergency (no lights/sirens)

using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PolicePatrolDispatchSystem : GameSystemBase
{
	[BurstCompile]
	private struct PoliceDispatchJob : IJobChunk
	{
		// ValidateTarget - checks CrimeProducer.m_Crime >= tolerance
		// This is the key difference from emergency dispatch
		private bool ValidateTarget(Entity entity, Entity target)
		{
			if (!m_CrimeProducerData.TryGetComponent(target, out var componentData))
			{
				return false;
			}
			if (componentData.m_Crime < m_PoliceConfigurationData.m_CrimeAccumulationTolerance)
			{
				return false;
			}
			if (componentData.m_PatrolRequest != entity)
			{
				if (m_PolicePatrolRequestData.HasComponent(componentData.m_PatrolRequest))
				{
					return false;
				}
				componentData.m_PatrolRequest = entity;
				m_CrimeProducerData[target] = componentData;
			}
			return true;
		}

		// ValidateReversed for patrol cars - STRICTER than emergency
		// Requires: Empty, not ShiftEnded, not EstimatedShiftEnd, not Disabled
		private bool ValidateReversed(Entity entity, Entity source)
		{
			if (m_PoliceStationData.TryGetComponent(source, out var componentData))
			{
				if ((componentData.m_Flags & (PoliceStationFlags.HasAvailablePatrolCars | PoliceStationFlags.HasAvailablePoliceHelicopters)) == 0 || (componentData.m_PurposeMask & PolicePurpose.Patrol) == 0)
				{
					return false;
				}
				// ... station validation ...
				return true;
			}
			if (m_PoliceCarData.TryGetComponent(source, out var componentData2))
			{
				// Must be Empty AND none of: ShiftEnded, EstimatedShiftEnd, Disabled
				if ((componentData2.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Empty | PoliceCarFlags.EstimatedShiftEnd | PoliceCarFlags.Disabled)) != PoliceCarFlags.Empty || componentData2.m_RequestCount > 1 || (componentData2.m_PurposeMask & PolicePurpose.Patrol) == 0 || m_ParkedCarData.HasComponent(source))
				{
					return false;
				}
				// ... car validation ...
				return true;
			}
			return false;
		}

		// FindVehicleSource - patrol pathfinding
		// Key differences from emergency:
		// - MaxSpeed: 277.77777f (1000 km/h) vs 111.111115f (400 km/h)
		// - Weights: (1,1,1,1) vs (1,0,0,0) -- patrol considers all factors
		// - Methods: Road + Flying (helicopters) vs Road only
		// - m_Value = 1 (Patrol purpose) vs (int)purpose
		private void FindVehicleSource(int jobIndex, Entity requestEntity, Entity targetEntity)
		{
			Entity entity = Entity.Null;
			if (m_CurrentDistrictData.HasComponent(targetEntity))
			{
				entity = m_CurrentDistrictData[targetEntity].m_District;
			}
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = (PathMethod.Road | PathMethod.Flying),
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.PolicePatrol,
				m_Methods = (PathMethod.Road | PathMethod.Flying),
				m_RoadTypes = (RoadTypes.Car | RoadTypes.Helicopter),
				m_FlyingTypes = RoadTypes.Helicopter,
				m_Entity = entity,
				m_Value = 1  // PolicePurpose.Patrol
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.Flying),
				m_RoadTypes = RoadTypes.Car,
				m_FlyingTypes = RoadTypes.Helicopter,
				m_Entity = targetEntity
			};
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}
	}

	// System update: every 16 frames
	public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
}
