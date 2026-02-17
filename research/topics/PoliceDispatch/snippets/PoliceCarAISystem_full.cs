// Decompiled from Game.dll using ilspycmd v9.1
// Full source for Game.Simulation.PoliceCarAISystem
// THE key system for police car behavior, emergency flag management, and dispatch processing

using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Events;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PoliceCarAISystem : GameSystemBase
{
	private struct PoliceAction
	{
		public PoliceActionType m_Type;
		public Entity m_Target;
		public Entity m_Request;
		public float m_CrimeReductionRate;
		public int m_DispatchIndex;
	}

	private enum PoliceActionType
	{
		ReduceCrime,
		AddPatrolRequest,
		SecureAccidentSite,
		BumpDispatchIndex
	}

	[BurstCompile]
	private struct PoliceCarTickJob : IJobChunk
	{
		// ... TypeHandles and Lookups omitted for brevity (see full decompiled source) ...

		// KEY METHOD: SelectNextDispatch - sets CarFlags.Emergency for emergency requests
		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, DynamicBuffer<Passenger> passengers, ref Game.Vehicles.PoliceCar policeCar, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if ((policeCar.m_State & PoliceCarFlags.Returning) == 0 && policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
				policeCar.m_RequestCount--;
			}
			while (policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				Entity entity = Entity.Null;
				PoliceCarFlags policeCarFlags = (PoliceCarFlags)0u;
				if (m_PolicePatrolRequestData.HasComponent(request))
				{
					if (!passengers.IsCreated || passengers.Length == 0)
					{
						entity = m_PolicePatrolRequestData[request].m_Target;
					}
				}
				else if (m_PoliceEmergencyRequestData.HasComponent(request))
				{
					entity = m_PoliceEmergencyRequestData[request].m_Site;
					policeCarFlags |= PoliceCarFlags.AccidentTarget;
				}
				if (!m_PrefabRefData.HasComponent(entity))
				{
					serviceDispatches.RemoveAt(0);
					policeCar.m_EstimatedShift -= policeCar.m_EstimatedShift / (uint)policeCar.m_RequestCount;
					policeCar.m_RequestCount--;
					continue;
				}
				policeCar.m_State &= ~(PoliceCarFlags.Returning | PoliceCarFlags.AccidentTarget | PoliceCarFlags.AtTarget | PoliceCarFlags.Cancelled);
				policeCar.m_State |= policeCarFlags;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_ServiceRequestData.HasComponent(policeCar.m_TargetRequest))
				{
					e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(policeCar.m_TargetRequest, Entity.Null, completed: true));
				}
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = policeCar.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformationData[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath, m_SlaveLaneData, m_OwnerData, m_SubLanes))
						{
							if ((policeCarFlags & PoliceCarFlags.AccidentTarget) != 0)
							{
								// EMERGENCY: lights and sirens ON
								car.m_Flags &= ~CarFlags.AnyLaneTarget;
								car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
							}
							else
							{
								// PATROL: lights and sirens OFF
								car.m_Flags &= ~CarFlags.Emergency;
								car.m_Flags |= CarFlags.StayOnRoad | CarFlags.AnyLaneTarget | CarFlags.UsePublicTransportLanes;
							}
							target.m_Target = entity;
							VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
							m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity); // TRIGGERS LIGHTS/SIRENS VISUAL UPDATE
							return true;
						}
					}
				}
				VehicleUtils.SetTarget(ref pathOwner, ref target, entity);
				return true;
			}
			return false;
		}

		// ResetPath - also sets Emergency flag based on dispatch type
		private void ResetPath(int jobIndex, Entity vehicleEntity, ref Unity.Mathematics.Random random, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.PoliceCar policeCar, ref Car carData, ref CarCurrentLane currentLane, ref PathOwner pathOwner)
		{
			// ... path reset logic ...
			if ((policeCar.m_State & PoliceCarFlags.Returning) == 0 && policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_PolicePatrolRequestData.HasComponent(request))
				{
					carData.m_Flags &= ~CarFlags.Emergency;
					carData.m_Flags |= CarFlags.StayOnRoad | CarFlags.AnyLaneTarget;
				}
				else if (m_PoliceEmergencyRequestData.HasComponent(request))
				{
					carData.m_Flags &= ~CarFlags.AnyLaneTarget;
					carData.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad;
				}
			}
			else
			{
				carData.m_Flags &= ~(CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.AnyLaneTarget);
			}
			carData.m_Flags |= CarFlags.UsePublicTransportLanes;
			m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
		}

		// SecureAccidentSite - stops vehicle at accident, sets Secured flag
		private bool SecureAccidentSite(int jobIndex, Entity entity, bool isStopped, ref Game.Vehicles.PoliceCar policeCar, ref CarCurrentLane currentLaneData, DynamicBuffer<Passenger> passengers, DynamicBuffer<ServiceDispatch> serviceDispatches)
		{
			if (policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_PoliceEmergencyRequestData.HasComponent(request))
				{
					PoliceEmergencyRequest policeEmergencyRequest = m_PoliceEmergencyRequestData[request];
					if (m_AccidentSiteData.HasComponent(policeEmergencyRequest.m_Site))
					{
						policeCar.m_State |= PoliceCarFlags.AtTarget;
						if (!isStopped)
						{
							StopVehicle(jobIndex, entity, ref currentLaneData);
						}
						if ((m_AccidentSiteData[policeEmergencyRequest.m_Site].m_Flags & AccidentSiteFlags.Secured) == 0)
						{
							m_ActionQueue.Enqueue(new PoliceAction
							{
								m_Type = PoliceActionType.SecureAccidentSite,
								m_Target = policeEmergencyRequest.m_Site
							});
						}
						return false;
					}
				}
			}
			return true;
		}

		// IsCloseEnough - 30m proximity check for blocked emergency vehicles
		private bool IsCloseEnough(Game.Objects.Transform transform, ref Target target)
		{
			if (m_TransformData.HasComponent(target.m_Target))
			{
				Game.Objects.Transform transform2 = m_TransformData[target.m_Target];
				return math.distance(transform.m_Position, transform2.m_Position) <= 30f;
			}
			if (m_AccidentSiteData.HasComponent(target.m_Target))
			{
				AccidentSite accidentSite = m_AccidentSiteData[target.m_Target];
				if (m_TargetElements.HasBuffer(accidentSite.m_Event))
				{
					DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[accidentSite.m_Event];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity entity = dynamicBuffer[i].m_Entity;
						if (m_InvolvedInAccidentData.HasComponent(entity) && m_TransformData.HasComponent(entity))
						{
							InvolvedInAccident involvedInAccident = m_InvolvedInAccidentData[entity];
							Game.Objects.Transform transform3 = m_TransformData[entity];
							if (involvedInAccident.m_Event == accidentSite.m_Event && math.distance(transform.m_Position, transform3.m_Position) <= 30f)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		// RequestTargetIfNeeded - creates reverse dispatch requests
		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Vehicles.PoliceCar policeCar)
		{
			if (m_ServiceRequestData.HasComponent(policeCar.m_TargetRequest))
			{
				return;
			}
			if ((policeCar.m_PurposeMask & PolicePurpose.Patrol) != 0 && (policeCar.m_State & (PoliceCarFlags.Empty | PoliceCarFlags.EstimatedShiftEnd)) == PoliceCarFlags.Empty)
			{
				uint num = math.max(512u, 16u);
				if ((m_SimulationFrameIndex & (num - 1)) == 5)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PolicePatrolRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new PolicePatrolRequest(entity, 1f));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
			else if ((policeCar.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)) != 0)
			{
				uint num2 = math.max(64u, 16u);
				if ((m_SimulationFrameIndex & (num2 - 1)) == 5)
				{
					Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_PoliceEmergencyRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e2, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e2, new PoliceEmergencyRequest(entity, Entity.Null, 1f, policeCar.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)));
					m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(4u));
				}
			}
		}

		// ParkCar - clears Emergency flag when parking
		private void ParkCar(int jobIndex, Entity entity, Owner owner, ref Game.Vehicles.PoliceCar policeCar, ref Car car, ref CarCurrentLane currentLane)
		{
			car.m_Flags &= ~CarFlags.Emergency;
			policeCar.m_State = PoliceCarFlags.Empty;
			// ... parking logic ...
		}
	}

	// System update: every 16 frames, offset 5
	public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
	public override int GetUpdateOffset(SystemUpdatePhase phase) => 5;
}
