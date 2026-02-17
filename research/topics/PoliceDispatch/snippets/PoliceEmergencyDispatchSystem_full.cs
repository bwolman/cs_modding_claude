// Decompiled from Game.dll using ilspycmd v9.1
// Full source for Game.Simulation.PoliceEmergencyDispatchSystem
// Handles pathfinding and dispatching police cars to AccidentSite entities

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PoliceEmergencyDispatchSystem : GameSystemBase
{
	[BurstCompile]
	private struct PoliceDispatchJob : IJobChunk
	{
		// DistrictIterator - spatial district lookup for entities without CurrentDistrict
		private struct DistrictIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public float2 m_Position;
			public ComponentLookup<District> m_DistrictData;
			public BufferLookup<Game.Areas.Node> m_Nodes;
			public BufferLookup<Triangle> m_Triangles;
			public Entity m_Result;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Position) && m_DistrictData.HasComponent(areaItem.m_Area))
				{
					DynamicBuffer<Game.Areas.Node> nodes = m_Nodes[areaItem.m_Area];
					DynamicBuffer<Triangle> dynamicBuffer = m_Triangles[areaItem.m_Area];
					if (dynamicBuffer.Length > areaItem.m_Triangle && MathUtils.Intersect(AreaUtils.GetTriangle2(nodes, dynamicBuffer[areaItem.m_Triangle]), m_Position, out var _))
					{
						m_Result = areaItem.m_Area;
					}
				}
			}
		}

		// ValidateSite - THE key validation method
		// Checks: AccidentSite exists, RequirePolice set, Secured NOT set
		private bool ValidateSite(Entity entity, Entity site)
		{
			if (!m_AccidentSiteData.TryGetComponent(site, out var componentData))
			{
				return false;
			}
			if ((componentData.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.RequirePolice)) != AccidentSiteFlags.RequirePolice)
			{
				return false;
			}
			if (componentData.m_PoliceRequest != entity)
			{
				if (m_PoliceEmergencyRequestData.HasComponent(componentData.m_PoliceRequest))
				{
					return false;
				}
				componentData.m_PoliceRequest = entity;
				m_AccidentSiteData[site] = componentData;
			}
			return true;
		}

		// FindVehicleSource - THE critical pathfinding setup
		// This is where mod-created requests often fail due to district resolution
		private void FindVehicleSource(int jobIndex, Entity requestEntity, Entity site, Entity target, PolicePurpose purpose)
		{
			Entity entity = Entity.Null;
			// District resolution: CurrentDistrict first, then spatial lookup
			if (m_CurrentDistrictData.HasComponent(target))
			{
				entity = m_CurrentDistrictData[target].m_District;
			}
			else if (m_TransformData.HasComponent(target))
			{
				DistrictIterator iterator = new DistrictIterator
				{
					m_Position = m_TransformData[target].m_Position.xz,
					m_DistrictData = m_DistrictData,
					m_Nodes = m_Nodes,
					m_Triangles = m_Triangles
				};
				m_AreaTree.Iterate(ref iterator);
				entity = iterator.m_Result;
			}
			// If entity is still Entity.Null here, pathfinding will fail to find any station

			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,  // 400 km/h -- no speed limit for cost calc
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 0f, 0f, 0f),  // Distance only
				m_Methods = PathMethod.Road,
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.PolicePatrol,  // Matches stations/cars with matching purpose
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Entity = entity,  // District entity
				m_Value = (int)purpose
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car
			};
			if (m_AccidentSiteData.HasComponent(site))
			{
				destination.m_Type = SetupTargetType.AccidentLocation;
				destination.m_Value2 = 30f;  // 30m arrival radius
				destination.m_Entity = site;
			}
			else
			{
				destination.m_Type = SetupTargetType.CurrentLocation;
				destination.m_Entity = target;
			}
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}

		// DispatchVehicle - resolves pathfinding result to vehicle entity
		private void DispatchVehicle(int jobIndex, Entity entity, PathInformation pathInformation)
		{
			Entity entity2 = pathInformation.m_Origin;
			if (m_ParkedCarData.HasComponent(entity2) && m_OwnerData.TryGetComponent(entity2, out var componentData))
			{
				entity2 = componentData.m_Owner;  // Resolve ParkedCar -> Owner (station)
			}
			VehicleDispatch value = new VehicleDispatch(entity, entity2);
			m_VehicleDispatches.Enqueue(value);
			m_CommandBuffer.AddComponent(jobIndex, entity, new Dispatched(entity2));
		}
	}

	// System update: every 16 frames
	public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;
}
