using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Tools;
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
public class PostVanAISystem : GameSystemBase
{
	private enum MailActionType
	{
		AddRequest,
		HandleBuilding,
		HandleMailBox,
		UnloadAll,
		ClearLane,
		BumpDispatchIndex
	}

	private struct MailAction
	{
		public MailActionType m_Type;

		public Entity m_Vehicle;

		public Entity m_Target;

		public Entity m_Request;

		public int m_DeliverAmount;

		public int m_CollectAmount;
	}

	[BurstCompile]
	private struct PostVanTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		public ComponentTypeHandle<Game.Vehicles.PostVan> m_PostVanType;

		public ComponentTypeHandle<Car> m_CarType;

		public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<CarNavigationLane> m_CarNavigationLaneType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<PostVanData> m_PrefabPostVanData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<MailAccumulationData> m_MailAccumulationData;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjectData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> m_PostVanRequestData;

		[ReadOnly]
		public ComponentLookup<MailProducer> m_MailProducerData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PostFacility> m_PostFacilityData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.MailBox> m_MailBoxData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_PostVanRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedCarRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		[ReadOnly]
		public PostConfigurationData m_PostConfigurationData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<MailAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray4 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<CarCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.PostVan> nativeArray6 = chunk.GetNativeArray(ref m_PostVanType);
			NativeArray<Car> nativeArray7 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<Target> nativeArray8 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray9 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<CarNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				PathInformation pathInformation = nativeArray4[i];
				Game.Vehicles.PostVan postVan = nativeArray6[i];
				Car car = nativeArray7[i];
				CarCurrentLane currentLane = nativeArray5[i];
				PathOwner pathOwner = nativeArray9[i];
				Target target = nativeArray8[i];
				DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor2[i];
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, prefabRef, pathInformation, navigationLanes, serviceDispatches, ref random, ref postVan, ref car, ref currentLane, ref pathOwner, ref target);
				nativeArray6[i] = postVan;
				nativeArray7[i] = car;
				nativeArray5[i] = currentLane;
				nativeArray9[i] = pathOwner;
				nativeArray8[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Random random, ref Game.Vehicles.PostVan postVan, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			PostVanData prefabPostVanData = m_PrefabPostVanData[prefabRef.m_Prefab];
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, vehicleEntity, pathInformation, serviceDispatches, prefabPostVanData, ref random, ref postVan, ref car, ref currentLane, ref pathOwner);
			}
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (postVan.m_State & PostVanFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					UnloadMail(vehicleEntity, owner.m_Owner, ref postVan);
					return;
				}
				ReturnToFacility(owner, serviceDispatches, ref postVan, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if ((postVan.m_State & PostVanFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					UnloadMail(vehicleEntity, owner.m_Owner, ref postVan);
					return;
				}
				TryHandleBuildings(jobIndex, vehicleEntity, prefabPostVanData, ref postVan, ref currentLane, target.m_Target);
				TryHandleBuilding(jobIndex, vehicleEntity, prefabPostVanData, ref postVan, target.m_Target);
				TryHandleMailBox(vehicleEntity, prefabPostVanData, ref postVan, ref target);
				CheckServiceDispatches(vehicleEntity, serviceDispatches, prefabPostVanData, ref postVan, ref pathOwner);
				if (!SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, prefabPostVanData, ref postVan, ref car, ref currentLane, ref pathOwner, ref target))
				{
					ReturnToFacility(owner, serviceDispatches, ref postVan, ref pathOwner, ref target);
				}
			}
			else
			{
				if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
				{
					if ((postVan.m_State & PostVanFlags.Returning) != 0)
					{
						ParkCar(jobIndex, vehicleEntity, owner, ref postVan, ref car, ref currentLane);
						UnloadMail(vehicleEntity, owner.m_Owner, ref postVan);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					}
					return;
				}
				if (VehicleUtils.WaypointReached(currentLane))
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
					TryHandleBuildings(jobIndex, vehicleEntity, prefabPostVanData, ref postVan, ref currentLane, Entity.Null);
				}
			}
			if (postVan.m_CollectedMail >= prefabPostVanData.m_MailCapacity)
			{
				postVan.m_State |= PostVanFlags.CollectFull | PostVanFlags.EstimatedFull;
			}
			else if (postVan.m_CollectedMail + postVan.m_CollectEstimate >= prefabPostVanData.m_MailCapacity)
			{
				postVan.m_State |= PostVanFlags.EstimatedFull;
				postVan.m_State &= ~PostVanFlags.CollectFull;
			}
			else
			{
				postVan.m_State &= ~(PostVanFlags.CollectFull | PostVanFlags.EstimatedFull);
			}
			if (postVan.m_DeliveringMail <= 0)
			{
				postVan.m_State |= PostVanFlags.DeliveryEmpty | PostVanFlags.EstimatedEmpty;
			}
			else if (postVan.m_DeliveringMail - postVan.m_DeliveryEstimate <= 0)
			{
				postVan.m_State |= PostVanFlags.EstimatedEmpty;
				postVan.m_State &= ~PostVanFlags.DeliveryEmpty;
			}
			else
			{
				postVan.m_State &= ~(PostVanFlags.DeliveryEmpty | PostVanFlags.EstimatedEmpty);
			}
			if ((postVan.m_State & (PostVanFlags.Returning | PostVanFlags.Delivering)) == PostVanFlags.Delivering && (postVan.m_State & (PostVanFlags.DeliveryEmpty | PostVanFlags.Disabled)) != 0)
			{
				ReturnToFacility(owner, serviceDispatches, ref postVan, ref pathOwner, ref target);
			}
			if ((postVan.m_State & (PostVanFlags.Returning | PostVanFlags.Collecting)) == PostVanFlags.Collecting && (postVan.m_State & (PostVanFlags.CollectFull | PostVanFlags.Disabled)) != 0)
			{
				ReturnToFacility(owner, serviceDispatches, ref postVan, ref pathOwner, ref target);
			}
			if ((postVan.m_State & (PostVanFlags.DeliveryEmpty | PostVanFlags.CollectFull)) != (PostVanFlags.DeliveryEmpty | PostVanFlags.CollectFull) && (postVan.m_State & PostVanFlags.Disabled) == 0)
			{
				CheckServiceDispatches(vehicleEntity, serviceDispatches, prefabPostVanData, ref postVan, ref pathOwner);
				if ((postVan.m_State & PostVanFlags.Returning) != 0)
				{
					SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, prefabPostVanData, ref postVan, ref car, ref currentLane, ref pathOwner, ref target);
				}
				if (postVan.m_RequestCount <= 1 && (postVan.m_State & (PostVanFlags.EstimatedEmpty | PostVanFlags.EstimatedFull)) != (PostVanFlags.EstimatedEmpty | PostVanFlags.EstimatedFull))
				{
					RequestTargetIfNeeded(jobIndex, vehicleEntity, ref postVan);
				}
			}
			else
			{
				serviceDispatches.Clear();
			}
			CheckBuildings(prefabPostVanData, ref postVan, ref currentLane, navigationLanes);
			if (VehicleUtils.RequireNewPath(pathOwner))
			{
				FindNewPath(vehicleEntity, prefabRef, ref postVan, ref currentLane, ref pathOwner, ref target);
			}
			else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) == 0)
			{
				CheckParkingSpace(vehicleEntity, ref random, ref currentLane, ref pathOwner, navigationLanes);
			}
		}

		private void CheckParkingSpace(Entity entity, ref Random random, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			DynamicBuffer<PathElement> path = m_PathElements[entity];
			ComponentLookup<Blocker> blockerData = default(ComponentLookup<Blocker>);
			VehicleUtils.ValidateParkingSpace(entity, ref random, ref currentLane, ref pathOwner, navigationLanes, path, ref m_ParkedCarData, ref blockerData, ref m_CurveData, ref m_UnspawnedData, ref m_ParkingLaneData, ref m_GarageLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabParkingLaneData, ref m_PrefabObjectGeometryData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false, ignoreDisabled: false, boardingOnly: false);
		}

		private void ParkCar(int jobIndex, Entity entity, Owner owner, ref Game.Vehicles.PostVan postVan, ref Car car, ref CarCurrentLane currentLane)
		{
			postVan.m_State = (PostVanFlags)0u;
			if (m_PostFacilityData.TryGetComponent(owner.m_Owner, out var componentData) && (componentData.m_Flags & (PostFacilityFlags.CanDeliverMailWithVan | PostFacilityFlags.CanCollectMailWithVan)) == 0)
			{
				postVan.m_State |= PostVanFlags.Disabled;
			}
			m_CommandBuffer.RemoveComponent(jobIndex, entity, in m_MovingToParkedCarRemoveTypes);
			m_CommandBuffer.AddComponent(jobIndex, entity, in m_MovingToParkedAddTypes);
			m_CommandBuffer.SetComponent(jobIndex, entity, new ParkedCar(currentLane.m_Lane, currentLane.m_CurvePosition.x));
			if (m_ParkingLaneData.HasComponent(currentLane.m_Lane) && currentLane.m_ChangeLane == Entity.Null)
			{
				m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, currentLane.m_Lane);
			}
			else if (m_GarageLaneData.HasComponent(currentLane.m_Lane))
			{
				m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, currentLane.m_Lane);
				m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(currentLane.m_ChangeLane, entity));
			}
			else
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(currentLane.m_ChangeLane, entity));
			}
		}

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, ref Game.Vehicles.PostVan postVan, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = carData.m_MaxSpeed,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = (PathMethod.Road | PathMethod.SpecialParking),
				m_ParkingTarget = VehicleUtils.GetParkingSource(vehicleEntity, currentLane, ref m_ParkingLaneData, ref m_ConnectionLaneData),
				m_ParkingDelta = currentLane.m_CurvePosition.z,
				m_ParkingSize = VehicleUtils.GetParkingSize(vehicleEntity, ref m_PrefabRefData, ref m_PrefabObjectGeometryData),
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.SpecialParking),
				m_RoadTypes = RoadTypes.Car
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Entity = target.m_Target
			};
			if ((postVan.m_State & PostVanFlags.Returning) != 0)
			{
				destination.m_Methods |= PathMethod.SpecialParking;
				destination.m_RandomCost = 30f;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private void CheckServiceDispatches(Entity vehicleEntity, DynamicBuffer<ServiceDispatch> serviceDispatches, PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, ref PathOwner pathOwner)
		{
			if (serviceDispatches.Length <= postVan.m_RequestCount)
			{
				return;
			}
			float num = -1f;
			Entity entity = Entity.Null;
			PathElement pathElement = default(PathElement);
			bool flag = false;
			int num2 = 0;
			if (postVan.m_RequestCount >= 1 && (postVan.m_State & PostVanFlags.Returning) == 0)
			{
				DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
				num2 = 1;
				if (pathOwner.m_ElementIndex < dynamicBuffer.Length)
				{
					pathElement = dynamicBuffer[dynamicBuffer.Length - 1];
					flag = true;
				}
			}
			for (int i = num2; i < postVan.m_RequestCount; i++)
			{
				Entity request = serviceDispatches[i].m_Request;
				if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
				{
					pathElement = bufferData[bufferData.Length - 1];
					flag = true;
				}
			}
			for (int j = postVan.m_RequestCount; j < serviceDispatches.Length; j++)
			{
				Entity request2 = serviceDispatches[j].m_Request;
				if (!m_PostVanRequestData.HasComponent(request2))
				{
					continue;
				}
				PostVanRequest postVanRequest = m_PostVanRequestData[request2];
				if (flag && m_PathElements.TryGetBuffer(request2, out var bufferData2) && bufferData2.Length != 0)
				{
					PathElement pathElement2 = bufferData2[0];
					if (pathElement2.m_Target != pathElement.m_Target || pathElement2.m_TargetDelta.x != pathElement.m_TargetDelta.y)
					{
						continue;
					}
				}
				if (m_PrefabRefData.HasComponent(postVanRequest.m_Target) && (float)(int)postVanRequest.m_Priority > num)
				{
					num = (int)postVanRequest.m_Priority;
					entity = request2;
				}
			}
			if (entity != Entity.Null)
			{
				serviceDispatches[postVan.m_RequestCount++] = new ServiceDispatch(entity);
				if (postVan.m_DeliveringMail > 0 || postVan.m_CollectedMail < prefabPostVanData.m_MailCapacity)
				{
					PreAddDeliveryRequests(entity, ref postVan);
				}
			}
			if (serviceDispatches.Length > postVan.m_RequestCount)
			{
				serviceDispatches.RemoveRange(postVan.m_RequestCount, serviceDispatches.Length - postVan.m_RequestCount);
			}
		}

		private void PreAddDeliveryRequests(Entity request, ref Game.Vehicles.PostVan postVan)
		{
			if (!m_PathElements.TryGetBuffer(request, out var bufferData))
			{
				return;
			}
			int dispatchIndex = BumpDispachIndex(request);
			Entity entity = Entity.Null;
			for (int i = 0; i < bufferData.Length; i++)
			{
				PathElement pathElement = bufferData[i];
				if (!m_EdgeLaneData.HasComponent(pathElement.m_Target))
				{
					entity = Entity.Null;
					continue;
				}
				Owner owner = m_OwnerData[pathElement.m_Target];
				if (!(owner.m_Owner == entity))
				{
					entity = owner.m_Owner;
					if (HasSidewalk(owner.m_Owner))
					{
						AddBuildingRequests(owner.m_Owner, request, dispatchIndex, ref postVan.m_CollectEstimate, ref postVan.m_DeliveryEstimate);
					}
				}
			}
		}

		private bool HasSidewalk(Entity owner)
		{
			if (m_SubLanes.HasBuffer(owner))
			{
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_PedestrianLaneData.HasComponent(subLane))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Vehicles.PostVan postVan)
		{
			if (!m_PostVanRequestData.HasComponent(postVan.m_TargetRequest))
			{
				uint num = math.max(512u, 16u);
				if ((m_SimulationFrameIndex & (num - 1)) == 9)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PostVanRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new PostVanRequest(entity, (PostVanRequestFlags)0, 1));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if ((postVan.m_State & PostVanFlags.Returning) == 0 && postVan.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
				postVan.m_RequestCount--;
			}
			while (postVan.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				PostVanRequest postVanRequest = default(PostVanRequest);
				if (m_PostVanRequestData.HasComponent(request))
				{
					postVanRequest = m_PostVanRequestData[request];
				}
				if (!m_PrefabRefData.HasComponent(postVanRequest.m_Target))
				{
					serviceDispatches.RemoveAt(0);
					postVan.m_CollectEstimate -= postVan.m_CollectEstimate / postVan.m_RequestCount;
					postVan.m_DeliveryEstimate -= postVan.m_DeliveryEstimate / postVan.m_RequestCount;
					postVan.m_RequestCount--;
					continue;
				}
				postVan.m_State &= ~PostVanFlags.Returning;
				car.m_Flags |= CarFlags.UsePublicTransportLanes;
				if ((postVanRequest.m_Flags & PostVanRequestFlags.Deliver) != 0)
				{
					postVan.m_State |= PostVanFlags.Delivering;
				}
				else
				{
					postVan.m_State &= ~PostVanFlags.Delivering;
				}
				if ((postVanRequest.m_Flags & PostVanRequestFlags.Collect) != 0)
				{
					postVan.m_State |= PostVanFlags.Collecting;
				}
				else
				{
					postVan.m_State &= ~PostVanFlags.Collecting;
				}
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_PostVanRequestData.HasComponent(postVan.m_TargetRequest))
				{
					e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(postVan.m_TargetRequest, Entity.Null, completed: true));
				}
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = postVan.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformationData[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath, m_SlaveLaneData, m_OwnerData, m_SubLanes, out var appendedCount))
						{
							if (postVan.m_DeliveringMail > 0 || postVan.m_CollectedMail < prefabPostVanData.m_MailCapacity)
							{
								int dispatchIndex = BumpDispachIndex(request);
								int num2 = dynamicBuffer.Length - appendedCount;
								int collectAmount = 0;
								int deliveryAmount = 0;
								for (int i = 0; i < num2; i++)
								{
									PathElement pathElement = dynamicBuffer[i];
									if (m_PedestrianLaneData.HasComponent(pathElement.m_Target))
									{
										AddBuildingRequests(m_OwnerData[pathElement.m_Target].m_Owner, request, dispatchIndex, ref collectAmount, ref deliveryAmount);
									}
								}
								if (appendedCount > 0)
								{
									NativeArray<PathElement> nativeArray = new NativeArray<PathElement>(appendedCount, Allocator.Temp);
									for (int j = 0; j < appendedCount; j++)
									{
										nativeArray[j] = dynamicBuffer[num2 + j];
									}
									dynamicBuffer.RemoveRange(num2, appendedCount);
									Entity lastOwner = Entity.Null;
									for (int k = 0; k < nativeArray.Length; k++)
									{
										AddPathElement(dynamicBuffer, nativeArray[k], request, dispatchIndex, ref lastOwner, ref collectAmount, ref deliveryAmount);
									}
									nativeArray.Dispose();
								}
								if (postVan.m_RequestCount == 1)
								{
									postVan.m_CollectEstimate = collectAmount;
									postVan.m_DeliveryEstimate = deliveryAmount;
								}
							}
							car.m_Flags |= CarFlags.StayOnRoad;
							postVan.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
							target.m_Target = postVanRequest.m_Target;
							VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
							return true;
						}
					}
				}
				VehicleUtils.SetTarget(ref pathOwner, ref target, postVanRequest.m_Target);
				return true;
			}
			return false;
		}

		private void ReturnToFacility(Owner ownerData, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.PostVan postVan, ref PathOwner pathOwnerData, ref Target targetData)
		{
			serviceDispatches.Clear();
			postVan.m_RequestCount = 0;
			postVan.m_CollectEstimate = 0;
			postVan.m_DeliveryEstimate = 0;
			postVan.m_State |= PostVanFlags.Returning;
			postVan.m_State &= ~(PostVanFlags.Delivering | PostVanFlags.Collecting);
			VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, ownerData.m_Owner);
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, PostVanData prefabPostVanData, ref Random random, ref Game.Vehicles.PostVan postVan, ref Car carData, ref CarCurrentLane currentLane, ref PathOwner pathOwner)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
			VehicleUtils.ResetParkingLaneStatus(vehicleEntity, ref currentLane, ref pathOwner, path, ref m_EntityLookup, ref m_CurveData, ref m_ParkingLaneData, ref m_CarLaneData, ref m_ConnectionLaneData, ref m_SpawnLocationData, ref m_PrefabRefData, ref m_PrefabSpawnLocationData);
			VehicleUtils.SetParkingCurvePos(vehicleEntity, ref random, currentLane, pathOwner, path, ref m_ParkedCarData, ref m_UnspawnedData, ref m_CurveData, ref m_ParkingLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, ref m_PrefabParkingLaneData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false);
			currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Checked;
			if ((postVan.m_State & PostVanFlags.Returning) == 0 && postVan.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_PostVanRequestData.HasComponent(request) && (postVan.m_DeliveringMail > 0 || postVan.m_CollectedMail < prefabPostVanData.m_MailCapacity))
				{
					NativeArray<PathElement> nativeArray = new NativeArray<PathElement>(path.Length, Allocator.Temp);
					nativeArray.CopyFrom(path.AsNativeArray());
					path.Clear();
					int dispatchIndex = BumpDispachIndex(request);
					Entity lastOwner = Entity.Null;
					int collectAmount = 0;
					int deliveryAmount = 0;
					for (int i = 0; i < nativeArray.Length; i++)
					{
						AddPathElement(path, nativeArray[i], request, dispatchIndex, ref lastOwner, ref collectAmount, ref deliveryAmount);
					}
					nativeArray.Dispose();
					if (postVan.m_RequestCount == 1)
					{
						postVan.m_CollectEstimate = collectAmount;
						postVan.m_DeliveryEstimate = deliveryAmount;
					}
				}
				carData.m_Flags |= CarFlags.StayOnRoad;
			}
			else
			{
				carData.m_Flags &= ~CarFlags.StayOnRoad;
			}
			carData.m_Flags |= CarFlags.UsePublicTransportLanes;
			postVan.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
		}

		private void AddPathElement(DynamicBuffer<PathElement> path, PathElement pathElement, Entity request, int dispatchIndex, ref Entity lastOwner, ref int collectAmount, ref int deliveryAmount)
		{
			if (!m_EdgeLaneData.HasComponent(pathElement.m_Target))
			{
				path.Add(pathElement);
				lastOwner = Entity.Null;
				return;
			}
			Owner owner = m_OwnerData[pathElement.m_Target];
			if (owner.m_Owner == lastOwner)
			{
				path.Add(pathElement);
				return;
			}
			lastOwner = owner.m_Owner;
			float curvePos = pathElement.m_TargetDelta.y;
			if (FindClosestSidewalk(pathElement.m_Target, owner.m_Owner, ref curvePos, out var sidewalk))
			{
				AddBuildingRequests(owner.m_Owner, request, dispatchIndex, ref collectAmount, ref deliveryAmount);
				path.Add(pathElement);
				path.Add(new PathElement(sidewalk, curvePos));
			}
			else
			{
				path.Add(pathElement);
			}
		}

		private bool FindClosestSidewalk(Entity lane, Entity owner, ref float curvePos, out Entity sidewalk)
		{
			bool result = false;
			sidewalk = Entity.Null;
			if (m_SubLanes.HasBuffer(owner))
			{
				float3 position = MathUtils.Position(m_CurveData[lane].m_Bezier, curvePos);
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
				float num = float.MaxValue;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_PedestrianLaneData.HasComponent(subLane))
					{
						float t;
						float num2 = MathUtils.Distance(MathUtils.Line(m_CurveData[subLane].m_Bezier), position, out t);
						if (num2 < num)
						{
							curvePos = t;
							sidewalk = subLane;
							num = num2;
							result = true;
						}
					}
				}
			}
			return result;
		}

		private int BumpDispachIndex(Entity request)
		{
			int result = 0;
			if (m_PostVanRequestData.TryGetComponent(request, out var componentData))
			{
				result = componentData.m_DispatchIndex + 1;
				m_ActionQueue.Enqueue(new MailAction
				{
					m_Type = MailActionType.BumpDispatchIndex,
					m_Request = request
				});
			}
			return result;
		}

		private void AddBuildingRequests(Entity edgeEntity, Entity request, int dispatchIndex, ref int collectAmount, ref int deliveryAmount)
		{
			if (!m_ConnectedBuildings.HasBuffer(edgeEntity))
			{
				return;
			}
			DynamicBuffer<ConnectedBuilding> dynamicBuffer = m_ConnectedBuildings[edgeEntity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity building = dynamicBuffer[i].m_Building;
				if (m_MailProducerData.TryGetComponent(building, out var componentData))
				{
					collectAmount += componentData.m_SendingMail;
					deliveryAmount += componentData.receivingMail;
					m_ActionQueue.Enqueue(new MailAction
					{
						m_Type = MailActionType.AddRequest,
						m_Request = request,
						m_Target = building,
						m_DeliverAmount = dispatchIndex
					});
				}
			}
		}

		private void CheckBuildings(PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			if ((postVan.m_State & PostVanFlags.ClearChecked) != 0)
			{
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Waypoint) != 0)
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Checked;
				}
				postVan.m_State &= ~PostVanFlags.ClearChecked;
			}
			if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Waypoint | Game.Vehicles.CarLaneFlags.Checked)) == Game.Vehicles.CarLaneFlags.Waypoint)
			{
				if ((postVan.m_DeliveringMail <= 0 && postVan.m_CollectedMail > prefabPostVanData.m_MailCapacity) || !CheckBuildings(prefabPostVanData, ref postVan, currentLane.m_Lane))
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
					if (m_SlaveLaneData.HasComponent(currentLane.m_Lane))
					{
						currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.FixedLane;
					}
				}
				currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Checked;
			}
			if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Waypoint) != 0)
			{
				return;
			}
			for (int i = 0; i < navigationLanes.Length; i++)
			{
				ref CarNavigationLane reference = ref navigationLanes.ElementAt(i);
				if ((reference.m_Flags & Game.Vehicles.CarLaneFlags.Waypoint) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Checked) == 0)
				{
					if ((postVan.m_DeliveringMail <= 0 && postVan.m_CollectedMail > prefabPostVanData.m_MailCapacity) || !CheckBuildings(prefabPostVanData, ref postVan, reference.m_Lane))
					{
						reference.m_Flags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
						if (m_SlaveLaneData.HasComponent(reference.m_Lane))
						{
							reference.m_Flags &= ~Game.Vehicles.CarLaneFlags.FixedLane;
						}
					}
					currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Checked;
				}
				if ((reference.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.Waypoint)) != Game.Vehicles.CarLaneFlags.Reserved)
				{
					break;
				}
			}
		}

		private bool CheckBuildings(PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, Entity laneEntity)
		{
			if (m_EdgeLaneData.HasComponent(laneEntity) && m_OwnerData.TryGetComponent(laneEntity, out var componentData) && m_ConnectedBuildings.TryGetBuffer(componentData.m_Owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity building = bufferData[i].m_Building;
					if (m_MailProducerData.TryGetComponent(building, out var componentData2) && ((postVan.m_DeliveringMail > 0 && componentData2.receivingMail > 0) || (postVan.m_CollectedMail < prefabPostVanData.m_MailCapacity && componentData2.m_SendingMail > 0 && RequireCollect(m_PrefabRefData[building].m_Prefab))))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void TryHandleBuildings(int jobIndex, Entity vehicleEntity, PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, ref CarCurrentLane currentLaneData, Entity ignoreBuilding)
		{
			if (postVan.m_DeliveringMail > 0 || postVan.m_CollectedMail < prefabPostVanData.m_MailCapacity)
			{
				TryHandleBuildings(jobIndex, vehicleEntity, prefabPostVanData, ref postVan, currentLaneData.m_Lane, ignoreBuilding);
			}
		}

		private void TryHandleBuildings(int jobIndex, Entity vehicleEntity, PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, Entity laneEntity, Entity ignoreBuilding)
		{
			if (!m_EdgeLaneData.HasComponent(laneEntity) || !m_OwnerData.TryGetComponent(laneEntity, out var componentData) || !m_ConnectedBuildings.TryGetBuffer(componentData.m_Owner, out var bufferData))
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity building = bufferData[i].m_Building;
				if (building != ignoreBuilding)
				{
					flag |= TryHandleBuilding(jobIndex, vehicleEntity, prefabPostVanData, ref postVan, building);
				}
			}
			if (flag)
			{
				m_ActionQueue.Enqueue(new MailAction
				{
					m_Type = MailActionType.ClearLane,
					m_Vehicle = vehicleEntity,
					m_Target = laneEntity
				});
			}
		}

		private bool TryHandleBuilding(int jobIndex, Entity vehicleEntity, PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, Entity building)
		{
			if (m_MailProducerData.TryGetComponent(building, out var componentData))
			{
				bool flag = postVan.m_DeliveringMail > 0 && componentData.receivingMail > 0;
				bool flag2 = postVan.m_CollectedMail < prefabPostVanData.m_MailCapacity && componentData.m_SendingMail > 0 && RequireCollect(m_PrefabRefData[building].m_Prefab);
				if (flag || flag2)
				{
					m_ActionQueue.Enqueue(new MailAction
					{
						m_Type = MailActionType.HandleBuilding,
						m_Vehicle = vehicleEntity,
						m_Target = building,
						m_DeliverAmount = math.select(0, prefabPostVanData.m_MailCapacity, flag),
						m_CollectAmount = math.select(0, prefabPostVanData.m_MailCapacity, flag2)
					});
					if (flag && componentData.receivingMail >= m_PostConfigurationData.m_MailAccumulationTolerance)
					{
						QuantityUpdated(jobIndex, building);
					}
					return true;
				}
			}
			return false;
		}

		private bool RequireCollect(Entity prefab)
		{
			ServiceObjectData componentData3;
			MailAccumulationData componentData4;
			if (m_SpawnableBuildingData.TryGetComponent(prefab, out var componentData))
			{
				if (m_MailAccumulationData.TryGetComponent(componentData.m_ZonePrefab, out var componentData2))
				{
					return componentData2.m_RequireCollect;
				}
			}
			else if (m_ServiceObjectData.TryGetComponent(prefab, out componentData3) && m_MailAccumulationData.TryGetComponent(componentData3.m_Service, out componentData4))
			{
				return componentData4.m_RequireCollect;
			}
			return false;
		}

		private void QuantityUpdated(int jobIndex, Entity buildingEntity)
		{
			if (!m_SubObjects.TryGetBuffer(buildingEntity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				if (m_QuantityData.HasComponent(subObject))
				{
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
				}
			}
		}

		private void TryHandleMailBox(Entity vehicleEntity, PostVanData prefabPostVanData, ref Game.Vehicles.PostVan postVan, ref Target targetData)
		{
			if (postVan.m_CollectedMail < prefabPostVanData.m_MailCapacity && m_MailBoxData.TryGetComponent(targetData.m_Target, out var componentData) && componentData.m_MailAmount > 0)
			{
				m_ActionQueue.Enqueue(new MailAction
				{
					m_Type = MailActionType.HandleMailBox,
					m_Vehicle = vehicleEntity,
					m_Target = targetData.m_Target,
					m_CollectAmount = prefabPostVanData.m_MailCapacity
				});
			}
		}

		private void UnloadMail(Entity vehicleEntity, Entity facility, ref Game.Vehicles.PostVan postVan)
		{
			if ((postVan.m_DeliveringMail > 0 || postVan.m_CollectedMail > 0) && m_PostFacilityData.HasComponent(facility))
			{
				m_ActionQueue.Enqueue(new MailAction
				{
					m_Type = MailActionType.UnloadAll,
					m_Vehicle = vehicleEntity,
					m_Target = facility
				});
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct MailActionJob : IJob
	{
		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		public ComponentLookup<Game.Vehicles.PostVan> m_PostVanData;

		public ComponentLookup<PostVanRequest> m_PostVanRequestData;

		public ComponentLookup<MailProducer> m_MailProducerData;

		public ComponentLookup<Game.Routes.MailBox> m_MailBoxData;

		public BufferLookup<Resources> m_Resources;

		public NativeQueue<MailAction> m_ActionQueue;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		public void Execute()
		{
			MailAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				switch (item.m_Type)
				{
				case MailActionType.AddRequest:
				{
					MailProducer value2 = m_MailProducerData[item.m_Target];
					value2.m_MailRequest = item.m_Request;
					value2.m_DispatchIndex = (byte)item.m_DeliverAmount;
					m_MailProducerData[item.m_Target] = value2;
					break;
				}
				case MailActionType.HandleBuilding:
				{
					Game.Vehicles.PostVan value3 = m_PostVanData[item.m_Vehicle];
					MailProducer value4 = m_MailProducerData[item.m_Target];
					int num = math.max(0, math.min(item.m_DeliverAmount, math.min(value3.m_DeliveringMail, value4.receivingMail)));
					int num2 = math.max(0, math.min(item.m_CollectAmount - value3.m_CollectedMail, value4.m_SendingMail));
					if (num != 0 || num2 != 0)
					{
						value3.m_DeliveringMail -= num;
						value3.m_CollectedMail += num2;
						value3.m_DeliveryEstimate = math.max(0, value3.m_DeliveryEstimate - num);
						value3.m_CollectEstimate = math.max(0, value3.m_CollectEstimate - num2);
						value4.receivingMail -= num;
						value4.mailDelivered |= num != 0;
						value4.m_SendingMail = (ushort)(value4.m_SendingMail - num2);
						m_PostVanData[item.m_Vehicle] = value3;
						m_MailProducerData[item.m_Target] = value4;
						if (num != 0)
						{
							m_StatisticsEventQueue.Enqueue(new StatisticsEvent
							{
								m_Statistic = StatisticType.DeliveredMail,
								m_Change = num
							});
						}
						if (num2 != 0)
						{
							m_StatisticsEventQueue.Enqueue(new StatisticsEvent
							{
								m_Statistic = StatisticType.CollectedMail,
								m_Change = num2
							});
						}
					}
					break;
				}
				case MailActionType.HandleMailBox:
				{
					Game.Vehicles.PostVan value6 = m_PostVanData[item.m_Vehicle];
					Game.Routes.MailBox value7 = m_MailBoxData[item.m_Target];
					int num3 = math.min(item.m_CollectAmount - value6.m_CollectedMail, value7.m_MailAmount);
					if (num3 > 0)
					{
						value6.m_CollectedMail += num3;
						value7.m_MailAmount -= num3;
						m_PostVanData[item.m_Vehicle] = value6;
						m_MailBoxData[item.m_Target] = value7;
						m_StatisticsEventQueue.Enqueue(new StatisticsEvent
						{
							m_Statistic = StatisticType.CollectedMail,
							m_Change = num3
						});
					}
					break;
				}
				case MailActionType.UnloadAll:
				{
					Game.Vehicles.PostVan value5 = m_PostVanData[item.m_Vehicle];
					DynamicBuffer<Resources> resources = m_Resources[item.m_Target];
					if (value5.m_DeliveringMail > 0)
					{
						EconomyUtils.AddResources(Resource.LocalMail, value5.m_DeliveringMail, resources);
						value5.m_DeliveringMail = 0;
					}
					if (value5.m_CollectedMail > 0)
					{
						EconomyUtils.AddResources(Resource.UnsortedMail, value5.m_CollectedMail, resources);
						value5.m_CollectedMail = 0;
					}
					m_PostVanData[item.m_Vehicle] = value5;
					break;
				}
				case MailActionType.ClearLane:
				{
					if (!m_LaneObjects.TryGetBuffer(item.m_Target, out var bufferData))
					{
						break;
					}
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity laneObject = bufferData[i].m_LaneObject;
						if (laneObject != item.m_Vehicle && m_PostVanData.TryGetComponent(laneObject, out var componentData))
						{
							componentData.m_State |= PostVanFlags.ClearChecked;
							m_PostVanData[laneObject] = componentData;
						}
					}
					break;
				}
				case MailActionType.BumpDispatchIndex:
				{
					PostVanRequest value = m_PostVanRequestData[item.m_Request];
					value.m_DispatchIndex++;
					m_PostVanRequestData[item.m_Request] = value;
					break;
				}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.PostVan> __Game_Vehicles_PostVan_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Car> __Game_Vehicles_Car_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostVanData> __Game_Prefabs_PostVanData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailAccumulationData> __Game_Prefabs_MailAccumulationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> __Game_Simulation_PostVanRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PostFacility> __Game_Buildings_PostFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.MailBox> __Game_Routes_MailBox_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		public ComponentLookup<Game.Vehicles.PostVan> __Game_Vehicles_PostVan_RW_ComponentLookup;

		public ComponentLookup<PostVanRequest> __Game_Simulation_PostVanRequest_RW_ComponentLookup;

		public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RW_ComponentLookup;

		public ComponentLookup<Game.Routes.MailBox> __Game_Routes_MailBox_RW_ComponentLookup;

		public BufferLookup<Resources> __Game_Economy_Resources_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Vehicles_PostVan_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.PostVan>();
			__Game_Vehicles_Car_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Car>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_PostVanData_RO_ComponentLookup = state.GetComponentLookup<PostVanData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_MailAccumulationData_RO_ComponentLookup = state.GetComponentLookup<MailAccumulationData>(isReadOnly: true);
			__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_PostVanRequest_RO_ComponentLookup = state.GetComponentLookup<PostVanRequest>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(isReadOnly: true);
			__Game_Buildings_PostFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.PostFacility>(isReadOnly: true);
			__Game_Routes_MailBox_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.MailBox>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__Game_Vehicles_PostVan_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PostVan>();
			__Game_Simulation_PostVanRequest_RW_ComponentLookup = state.GetComponentLookup<PostVanRequest>();
			__Game_Buildings_MailProducer_RW_ComponentLookup = state.GetComponentLookup<MailProducer>();
			__Game_Routes_MailBox_RW_ComponentLookup = state.GetComponentLookup<Game.Routes.MailBox>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Resources>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_VehicleQuery;

	private EntityQuery m_PostConfigurationQuery;

	private EntityArchetype m_PostVanRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_MovingToParkedCarRemoveTypes;

	private ComponentTypeSet m_MovingToParkedAddTypes;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 9;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Game.Vehicles.PostVan>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_PostConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PostConfigurationData>());
		m_PostVanRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PostVanRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Event>());
		m_MovingToParkedCarRemoveTypes = new ComponentTypeSet(new ComponentType[13]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>()
		});
		m_MovingToParkedAddTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<Stopped>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<MailAction> actionQueue = new NativeQueue<MailAction>(Allocator.Persistent);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new PostVanTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PostVanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PostVan_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPostVanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PostVanData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailAccumulationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MailAccumulationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PostVanRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PostVanRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PostFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PostFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailBoxData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_MailBox_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_PostVanRequestArchetype = m_PostVanRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedCarRemoveTypes = m_MovingToParkedCarRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_PostConfigurationData = m_PostConfigurationQuery.GetSingleton<PostConfigurationData>(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		}, m_VehicleQuery, base.Dependency);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		JobHandle deps;
		JobHandle jobHandle2 = IJobExtensions.Schedule(new MailActionJob
		{
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PostVanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PostVan_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PostVanRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PostVanRequest_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MailProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MailProducer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MailBoxData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_MailBox_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue,
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter()
		}, JobHandle.CombineDependencies(jobHandle, deps));
		m_CityStatisticsSystem.AddWriter(jobHandle2);
		actionQueue.Dispose(jobHandle2);
		base.Dependency = jobHandle2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public PostVanAISystem()
	{
	}
}
