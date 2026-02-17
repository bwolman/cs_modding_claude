// Decompiled from Game.dll using ilspycmd v9.1
// Full source for Game.Simulation.PoliceStationAISystem
// Manages police station state, vehicle spawning, and reverse dispatch creation

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PoliceStationAISystem : GameSystemBase
{
	[BurstCompile]
	private struct PoliceStationTickJob : IJobChunk
	{
		// RequestTargetIfNeeded - station creates reverse requests when vehicles available
		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.PoliceStation policeStation, int availablePatrolCars, int availablePoliceHelicopters)
		{
			if (m_ServiceRequestData.HasComponent(policeStation.m_TargetRequest))
			{
				return;
			}
			if ((policeStation.m_PurposeMask & PolicePurpose.Patrol) != 0)
			{
				uint num = math.max(512u, 256u);
				if ((m_SimulationFrameIndex & (num - 1)) == 128)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PolicePatrolRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new PolicePatrolRequest(entity, availablePatrolCars + availablePoliceHelicopters));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
			else if ((policeStation.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)) != 0 && availablePatrolCars > 0)
			{
				Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_PoliceEmergencyRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e2, new ServiceRequest(reversed: true));
				m_CommandBuffer.SetComponent(jobIndex, e2, new PoliceEmergencyRequest(entity, Entity.Null, availablePatrolCars, policeStation.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)));
				m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(4u));
			}
		}

		// SpawnVehicle - creates new police car when dispatched from station
		// Sets initial PoliceCar state with AccidentTarget for emergency requests
		private void SpawnVehicle(int jobIndex, ref Random random, Entity entity, Entity request, RoadTypes roadType, ref Game.Buildings.PoliceStation policeStation, ref int availableVehicles, ref StackList<Entity> parkedVehicles, bool outside)
		{
			if (availableVehicles <= 0) return;

			PoliceCarFlags policeCarFlags = PoliceCarFlags.Empty;
			Entity entity2;
			PolicePurpose purposeMask;
			if (m_PolicePatrolRequestData.TryGetComponent(request, out var componentData))
			{
				entity2 = componentData.m_Target;
				purposeMask = policeStation.m_PurposeMask & PolicePurpose.Patrol;
			}
			else if (m_PoliceEmergencyRequestData.TryGetComponent(request, out var componentData2))
			{
				entity2 = componentData2.m_Site;
				purposeMask = policeStation.m_PurposeMask & componentData2.m_Purpose;
				policeCarFlags |= PoliceCarFlags.AccidentTarget;  // Emergency flag for new vehicle
			}
			else return;

			// ... vehicle creation and path setup ...
			m_CommandBuffer.SetComponent(jobIndex, /*vehicle*/ Entity.Null, new Game.Vehicles.PoliceCar(policeCarFlags, 1, policeStation.m_PurposeMask & purposeMask));
		}

		// Tick - manages occupants (criminal processing) and vehicle availability
		private void Tick(int jobIndex, Entity entity, ref Random random, ref Game.Buildings.PoliceStation policeStation, PoliceStationData prefabPoliceStationData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<ServiceDispatch> dispatches, DynamicBuffer<Occupant> occupants, float efficiency, float immediateEfficiency, bool outside)
		{
			// Count available vehicles, manage disabled state
			// Process sentenced criminals -> request prisoner transport
			// Spawn vehicles for queued dispatches
			// Create reverse requests for available vehicles
		}
	}

	// System update: every 256 frames, offset 128
	public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;
	public override int GetUpdateOffset(SystemUpdatePhase phase) => 128;
}
