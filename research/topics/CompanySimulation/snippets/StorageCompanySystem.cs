using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
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
public class StorageCompanySystem : GameSystemBase, IPostDeserialize
{
	[BurstCompile]
	private struct StorageJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_CompanyResourceType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_Limits;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		public BufferLookup<StorageTransferRequest> m_StorageTransferRequests;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_Trucks;

		[ReadOnly]
		public ComponentLookup<Target> m_Targets;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<GuestVehicle> m_GuestVehicles;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		public uint m_SimulationFrame;

		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<PropertyRenter> nativeArray3 = chunk.GetNativeArray(ref m_PropertyRenterType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_CompanyResourceType);
			BufferAccessor<TradeCost> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TradeCostType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				if (m_Prefabs.HasComponent(nativeArray3[i].m_Property))
				{
					DynamicBuffer<Resources> resourceBuffer = bufferAccessor[i];
					DynamicBuffer<TradeCost> tradeCosts = bufferAccessor2[i];
					Entity prefab = nativeArray2[i].m_Prefab;
					StorageLimitData limitData = m_Limits[prefab];
					StorageCompanyData storageCompanyData = m_StorageCompanyDatas[prefab];
					Entity prefab2 = m_Prefabs[nativeArray3[i].m_Property].m_Prefab;
					SpawnableBuildingData spawnableData = (m_SpawnableBuildingDatas.HasComponent(prefab2) ? m_SpawnableBuildingDatas[prefab2] : new SpawnableBuildingData
					{
						m_Level = 1
					});
					BuildingData buildingData = (m_BuildingDatas.HasComponent(prefab2) ? m_BuildingDatas[prefab2] : new BuildingData
					{
						m_LotSize = new int2(1, 1)
					});
					if (!m_GuestVehicles.HasBuffer(entity))
					{
						m_CommandBuffer.AddBuffer<GuestVehicle>(unfilteredChunkIndex, entity);
					}
					ProcessStorage(unfilteredChunkIndex, entity, nativeArray3[i].m_Property, storageCompanyData.m_StoredResources, storageCompanyData, resourceBuffer, m_StorageTransferRequests[entity], limitData, spawnableData, buildingData, m_DeliveryTruckSelectData, m_SimulationFrame, tradeCosts, m_CommandBuffer, station: false, hasConnectedRoute: false, 0, ref random, ref m_StorageCompanies, ref m_OwnedVehicles, ref m_StorageTransferRequests, ref m_Trucks, ref m_Targets, ref m_LayoutElements, ref m_PropertyRenters, ref m_OutsideConnections);
				}
				else
				{
					m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, entity);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct StationStorageJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_CompanyResourceType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_Limits;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		public BufferLookup<StorageTransferRequest> m_StorageTransferRequests;

		public BufferLookup<TripNeeded> m_TripNeededsBuffers;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_Trucks;

		[ReadOnly]
		public ComponentLookup<Target> m_Targets;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectBuffers;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineData;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRouteBuffers;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public BufferLookup<RouteVehicle> m_RouteVehicles;

		[ReadOnly]
		public BufferLookup<Resources> m_ResourceBuffers;

		[ReadOnly]
		public ComponentLookup<Connected> m_Connecteds;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransports;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypointBufs;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> m_CurrentRoutes;

		[ReadOnly]
		public ComponentLookup<Waypoint> m_Waypoints;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		public uint m_SimulationFrame;

		public RandomSeed m_RandomSeed;

		public int m_UpdateInterval;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_CompanyResourceType);
			BufferAccessor<TradeCost> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TradeCostType);
			BufferAccessor<InstalledUpgrade> bufferAccessor3 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			Resource resource = EconomyUtils.GetResource((int)(m_SimulationFrame / m_UpdateInterval) % EconomyUtils.ResourceCount);
			for (int i = 0; i < chunk.Count; i++)
			{
				int incomingAmount = 0;
				Entity entity = nativeArray[i];
				DynamicBuffer<Resources> resourceBuffer = bufferAccessor[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				StorageLimitData data = m_Limits[prefab];
				StorageCompanyData data2 = m_StorageCompanyDatas[prefab];
				DynamicBuffer<TradeCost> tradeCosts = bufferAccessor2[i];
				if (bufferAccessor3.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor3[i], ref m_PrefabRefData, ref m_Limits);
					UpgradeUtils.CombineStats(ref data2, bufferAccessor3[i], ref m_PrefabRefData, ref m_StorageCompanyDatas);
				}
				bool hasConnectedRoute = false;
				CheckConnectedRoute(entity, resource, ref hasConnectedRoute, ref incomingAmount, entity);
				if (m_Buildings.HasComponent(entity) && BuildingUtils.CheckOption(m_Buildings[entity], BuildingOption.Inactive))
				{
					if (m_TripNeededsBuffers.HasBuffer(entity))
					{
						m_TripNeededsBuffers[entity].Clear();
					}
					if (m_StorageTransferRequests.HasBuffer(entity))
					{
						m_StorageTransferRequests[entity].Clear();
					}
				}
				else
				{
					ProcessStorage(unfilteredChunkIndex, entity, entity, resource, data2, resourceBuffer, m_StorageTransferRequests[entity], data, new SpawnableBuildingData
					{
						m_Level = 1
					}, new BuildingData
					{
						m_LotSize = new int2(1, 1)
					}, m_DeliveryTruckSelectData, m_SimulationFrame, tradeCosts, m_CommandBuffer, station: true, hasConnectedRoute, incomingAmount, ref random, ref m_StorageCompanies, ref m_OwnedVehicles, ref m_StorageTransferRequests, ref m_Trucks, ref m_Targets, ref m_LayoutElements, ref m_PropertyRenters, ref m_OutsideConnections);
				}
			}
		}

		private void CheckConnectedRoute(Entity entity, Resource resource, ref bool hasConnectedRoute, ref int incomingAmount, Entity stationEntity)
		{
			if (m_ConnectedRouteBuffers.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity waypoint = bufferData[i].m_Waypoint;
					if (!m_Owners.HasComponent(waypoint))
					{
						continue;
					}
					Entity owner = m_Owners[waypoint].m_Owner;
					if (!m_RouteVehicles.HasBuffer(owner) || !m_PrefabRefData.HasComponent(owner) || !m_TransportLineData.HasComponent(m_PrefabRefData[owner].m_Prefab) || !m_TransportLineData[m_PrefabRefData[owner].m_Prefab].m_CargoTransport)
					{
						continue;
					}
					hasConnectedRoute = true;
					DynamicBuffer<RouteVehicle> dynamicBuffer = m_RouteVehicles[owner];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity vehicle = dynamicBuffer[j].m_Vehicle;
						if (!m_Targets.HasComponent(vehicle) || !m_ResourceBuffers.HasBuffer(vehicle))
						{
							continue;
						}
						bool flag = false;
						Entity entity2 = m_Targets[vehicle].m_Target;
						if (m_Connecteds.HasComponent(entity2))
						{
							entity2 = m_Connecteds[entity2].m_Connected;
						}
						Owner componentData;
						while (m_Owners.TryGetComponent(entity2, out componentData))
						{
							entity2 = componentData.m_Owner;
						}
						Game.Vehicles.CargoTransport componentData2;
						if (entity2 == stationEntity)
						{
							flag = true;
						}
						else if (m_CargoTransports.TryGetComponent(vehicle, out componentData2) && (componentData2.m_State & CargoTransportFlags.Boarding) != 0 && m_CurrentRoutes.HasComponent(vehicle))
						{
							Entity route = m_CurrentRoutes[vehicle].m_Route;
							DynamicBuffer<RouteWaypoint> dynamicBuffer2 = m_RouteWaypointBufs[route];
							int num = m_Waypoints[m_Targets[vehicle].m_Target].m_Index + 1;
							num = math.select(num, 0, num >= dynamicBuffer2.Length);
							Entity entity3 = dynamicBuffer2[num].m_Waypoint;
							if (m_Connecteds.HasComponent(entity3))
							{
								entity3 = m_Connecteds[entity3].m_Connected;
							}
							Owner componentData3;
							while (m_Owners.TryGetComponent(entity3, out componentData3))
							{
								entity3 = componentData3.m_Owner;
							}
							if (entity3 == stationEntity)
							{
								flag = true;
							}
						}
						if (!flag)
						{
							continue;
						}
						DynamicBuffer<Resources> resources = m_ResourceBuffers[vehicle];
						incomingAmount += EconomyUtils.GetResources(resource, resources);
						if (!m_LayoutElements.HasBuffer(vehicle))
						{
							continue;
						}
						DynamicBuffer<LayoutElement> dynamicBuffer3 = m_LayoutElements[vehicle];
						for (int k = 0; k < dynamicBuffer3.Length; k++)
						{
							Entity vehicle2 = dynamicBuffer3[k].m_Vehicle;
							if (m_ResourceBuffers.HasBuffer(vehicle2))
							{
								resources = m_ResourceBuffers[vehicle2];
								incomingAmount += EconomyUtils.GetResources(resource, resources);
							}
						}
					}
				}
			}
			if (m_SubObjectBuffers.TryGetBuffer(entity, out var bufferData2))
			{
				for (int l = 0; l < bufferData2.Length; l++)
				{
					CheckConnectedRoute(bufferData2[l].m_SubObject, resource, ref hasConnectedRoute, ref incomingAmount, stationEntity);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct OCStationStorageJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_CompanyResourceType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_Limits;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		public BufferLookup<StorageTransferRequest> m_StorageTransferRequests;

		[ReadOnly]
		public ComponentLookup<Target> m_Targets;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectBuffers;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineData;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRouteBuffers;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public BufferLookup<RouteVehicle> m_RouteVehicles;

		[ReadOnly]
		public BufferLookup<Resources> m_ResourceBuffers;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<Connected> m_Connecteds;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public uint m_SimulationFrame;

		public RandomSeed m_RandomSeed;

		public int m_UpdateInterval;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_CompanyResourceType);
			BufferAccessor<TradeCost> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TradeCostType);
			Resource resource = EconomyUtils.GetResource((int)(m_SimulationFrame / m_UpdateInterval) % EconomyUtils.ResourceCount);
			for (int i = 0; i < chunk.Count; i++)
			{
				int incomingAmount = 0;
				Entity entity = nativeArray[i];
				DynamicBuffer<Resources> resourceBuffer = bufferAccessor[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				StorageLimitData limitData = m_Limits[prefab];
				StorageCompanyData storageCompanyData = m_StorageCompanyDatas[prefab];
				DynamicBuffer<TradeCost> tradeCosts = bufferAccessor2[i];
				bool hasConnectedRoute = false;
				CheckConnectedRoute(entity, resource, ref hasConnectedRoute, ref incomingAmount);
				if (hasConnectedRoute)
				{
					bool flag = CheckIfOCIsBeforeOrAfterStation(checkBefore: true, m_ConnectedRouteBuffers[entity], ref m_RouteWaypoints, ref m_Connecteds, ref m_OutsideConnections);
					bool flag2 = CheckIfOCIsBeforeOrAfterStation(checkBefore: false, m_ConnectedRouteBuffers[entity], ref m_RouteWaypoints, ref m_Connecteds, ref m_OutsideConnections);
					if (!flag || !flag2)
					{
						OCProcessStorage(unfilteredChunkIndex, flag, flag2, entity, entity, resource, storageCompanyData, resourceBuffer, m_StorageTransferRequests[entity], limitData, m_SimulationFrame, tradeCosts, m_CommandBuffer, incomingAmount, ref random, ref m_StorageCompanies, ref m_StorageTransferRequests);
					}
				}
			}
		}

		private bool OCProcessStorage(int chunkIndex, bool isBeforeStation, bool isAfterStation, Entity company, Entity building, Resource resource, StorageCompanyData storageCompanyData, DynamicBuffer<Resources> resourceBuffer, DynamicBuffer<StorageTransferRequest> requests, StorageLimitData limitData, uint simulationFrame, DynamicBuffer<TradeCost> tradeCosts, EntityCommandBuffer.ParallelWriter commandBuffer, int incomingAmount, ref Random random, ref ComponentLookup<Game.Companies.StorageCompany> storageCompanies, ref BufferLookup<StorageTransferRequest> storageTransferRequests)
		{
			bool flag = false;
			int num = EconomyUtils.CountResources(storageCompanyData.m_StoredResources);
			int num2 = ((num == 0) ? limitData.m_Limit : (limitData.m_Limit / 2 / num));
			if ((storageCompanyData.m_StoredResources & resource) != Resource.NoResource)
			{
				int resources = EconomyUtils.GetResources(resource, resourceBuffer);
				int num3 = resources;
				for (int i = 0; i < requests.Length; i++)
				{
					StorageTransferRequest value = requests[i];
					if (value.m_Resource != resource)
					{
						continue;
					}
					if (!storageCompanies.HasComponent(value.m_Target) || !storageTransferRequests.HasBuffer(value.m_Target))
					{
						requests.RemoveAtSwapBack(i);
						i--;
						continue;
					}
					bool flag2 = (value.m_Flags & StorageTransferFlags.Incoming) != 0;
					if (flag2)
					{
						int num4 = 0;
						DynamicBuffer<StorageTransferRequest> dynamicBuffer = storageTransferRequests[value.m_Target];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							StorageTransferRequest storageTransferRequest = dynamicBuffer[j];
							if ((storageTransferRequest.m_Target == company || storageTransferRequest.m_Target == building) && storageTransferRequest.m_Resource == resource && (storageTransferRequest.m_Flags & StorageTransferFlags.Incoming) == 0)
							{
								num4 += storageTransferRequest.m_Amount;
							}
						}
						int num5 = 0;
						if (num4 < value.m_Amount && incomingAmount > 0)
						{
							int num6 = math.min(value.m_Amount - num4, incomingAmount);
							num5 += num6;
							incomingAmount -= num6;
						}
						if (num5 + num4 == 0)
						{
							requests.RemoveAtSwapBack(i);
							i--;
							continue;
						}
						if (num5 + num4 < value.m_Amount)
						{
							value.m_Amount = num5 + num4;
							requests[i] = value;
						}
					}
					else
					{
						int num7 = 0;
						DynamicBuffer<StorageTransferRequest> dynamicBuffer2 = storageTransferRequests[value.m_Target];
						for (int k = 0; k < dynamicBuffer2.Length; k++)
						{
							StorageTransferRequest storageTransferRequest2 = dynamicBuffer2[k];
							if ((storageTransferRequest2.m_Target == company || storageTransferRequest2.m_Target == building) && storageTransferRequest2.m_Resource == resource && (storageTransferRequest2.m_Flags & StorageTransferFlags.Incoming) != 0)
							{
								num7 = storageTransferRequest2.m_Amount;
								break;
							}
						}
						if (num7 == 0)
						{
							requests.RemoveAtSwapBack(i);
							i--;
							continue;
						}
						if (num7 < value.m_Amount)
						{
							value.m_Amount = num7;
							requests[i] = value;
						}
					}
					num3 += (flag2 ? value.m_Amount : (-value.m_Amount));
				}
				TradeCost tradeCost = EconomyUtils.GetTradeCost(resource, tradeCosts);
				long lastTradeRequestTime = EconomyUtils.GetLastTradeRequestTime(tradeCosts);
				if (tradeCost.m_LastTransferRequestTime == 0L)
				{
					tradeCost.m_LastTransferRequestTime = simulationFrame - kTransferCooldown / 2;
					EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
				}
				if (simulationFrame - lastTradeRequestTime >= kTransferCooldown + random.NextInt(storageCompanyData.m_TransportInterval.x, storageCompanyData.m_TransportInterval.y) || tradeCost.m_LastTransferRequestTime == 0L)
				{
					if (resources > num2 && num3 > num2 && isBeforeStation)
					{
						int x = resources - num2;
						x = math.max(x, kStationMinimalTransferAmount);
						commandBuffer.AddComponent(chunkIndex, company, new StorageTransfer
						{
							m_Resource = resource,
							m_Amount = x
						});
						tradeCost.m_LastTransferRequestTime = simulationFrame;
						EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
						flag = true;
					}
					else if (resources < num2 && num3 < num2 && isAfterStation)
					{
						StorageTransfer component = new StorageTransfer
						{
							m_Resource = resource
						};
						int x2 = num2 - resources;
						x2 = math.max(x2, kStationMinimalTransferAmount);
						component.m_Amount = -x2;
						commandBuffer.AddComponent(chunkIndex, company, component);
						tradeCost.m_LastTransferRequestTime = simulationFrame;
						flag = true;
					}
					if (random.NextInt(kCostFadeProbability) == 0)
					{
						tradeCost.m_BuyCost *= 0.99f;
						tradeCost.m_SellCost *= 0.99f;
						if (!flag)
						{
							EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
						}
					}
					if (flag)
					{
						EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
					}
				}
			}
			return flag;
		}

		private void CheckConnectedRoute(Entity entity, Resource resource, ref bool hasConnectedRoute, ref int incomingAmount)
		{
			if (m_ConnectedRouteBuffers.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity waypoint = bufferData[i].m_Waypoint;
					if (!m_Owners.HasComponent(waypoint))
					{
						continue;
					}
					Entity owner = m_Owners[waypoint].m_Owner;
					if (!m_RouteVehicles.HasBuffer(owner) || !m_PrefabRefData.HasComponent(owner) || !m_TransportLineData.HasComponent(m_PrefabRefData[owner].m_Prefab) || !m_TransportLineData[m_PrefabRefData[owner].m_Prefab].m_CargoTransport)
					{
						continue;
					}
					hasConnectedRoute = true;
					DynamicBuffer<RouteVehicle> dynamicBuffer = m_RouteVehicles[owner];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity vehicle = dynamicBuffer[j].m_Vehicle;
						if (!m_Targets.HasComponent(vehicle) || !m_ResourceBuffers.HasBuffer(vehicle))
						{
							continue;
						}
						Entity entity2 = m_Targets[vehicle].m_Target;
						if (m_Connecteds.HasComponent(entity2))
						{
							entity2 = m_Connecteds[entity2].m_Connected;
						}
						if (m_Owners.HasComponent(entity2))
						{
							entity2 = m_Owners[entity2].m_Owner;
						}
						if (!(entity2 == entity))
						{
							continue;
						}
						DynamicBuffer<Resources> resources = m_ResourceBuffers[vehicle];
						incomingAmount += EconomyUtils.GetResources(resource, resources);
						if (!m_LayoutElements.HasBuffer(vehicle))
						{
							continue;
						}
						DynamicBuffer<LayoutElement> dynamicBuffer2 = m_LayoutElements[vehicle];
						for (int k = 0; k < dynamicBuffer2.Length; k++)
						{
							Entity vehicle2 = dynamicBuffer2[k].m_Vehicle;
							if (m_ResourceBuffers.HasBuffer(vehicle2))
							{
								resources = m_ResourceBuffers[vehicle2];
								incomingAmount += EconomyUtils.GetResources(resource, resources);
							}
						}
					}
				}
			}
			if (m_SubObjectBuffers.TryGetBuffer(entity, out var bufferData2))
			{
				for (int l = 0; l < bufferData2.Length; l++)
				{
					CheckConnectedRoute(bufferData2[l].m_SubObject, resource, ref hasConnectedRoute, ref incomingAmount);
				}
			}
		}

		private bool CheckIfOCIsBeforeOrAfterStation(bool checkBefore, DynamicBuffer<ConnectedRoute> connectedRoutes, ref BufferLookup<RouteWaypoint> routeWaypoints, ref ComponentLookup<Connected> connects, ref ComponentLookup<Game.Objects.OutsideConnection> outsideConnections)
		{
			if (connectedRoutes.Length == 0)
			{
				return false;
			}
			Entity waypoint = connectedRoutes[0].m_Waypoint;
			if (!m_Owners.HasComponent(waypoint))
			{
				return false;
			}
			Entity owner = m_Owners[waypoint].m_Owner;
			if (!routeWaypoints.TryGetBuffer(owner, out var bufferData))
			{
				return false;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				if (!(bufferData[i].m_Waypoint != waypoint))
				{
					int num = ((!checkBefore) ? 1 : (-1));
					int index = (i + num + bufferData.Length) % bufferData.Length;
					Entity waypoint2 = bufferData[index].m_Waypoint;
					if (!connects.TryGetComponent(waypoint2, out var componentData))
					{
						return false;
					}
					return !outsideConnections.HasComponent(componentData.m_Connected);
				}
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public BufferTypeHandle<TradeCost> __Game_Companies_TradeCost_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

		public BufferLookup<StorageTransferRequest> __Game_Companies_StorageTransferRequest_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public BufferLookup<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteVehicle> __Game_Routes_RouteVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Companies_TradeCost_RW_BufferTypeHandle = state.GetBufferTypeHandle<TradeCost>();
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(isReadOnly: true);
			__Game_Companies_StorageTransferRequest_RW_BufferLookup = state.GetBufferLookup<StorageTransferRequest>();
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RW_BufferLookup = state.GetBufferLookup<TripNeeded>();
			__Game_Routes_ConnectedRoute_RO_BufferLookup = state.GetBufferLookup<ConnectedRoute>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_Routes_RouteVehicle_RO_BufferLookup = state.GetBufferLookup<RouteVehicle>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Vehicles_CargoTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.CargoTransport>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_CurrentRoute_RO_ComponentLookup = state.GetComponentLookup<CurrentRoute>(isReadOnly: true);
			__Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(isReadOnly: true);
		}
	}

	private static readonly int kTransferCooldown = 400;

	private static readonly int kCostFadeProbability = 256;

	private static readonly float kMaxTransportUnitCost = 0.03f;

	public static readonly int kStorageLowStockAmount = 25000;

	public static readonly int kStationLowStockAmount = 100000;

	public static readonly int kStorageExportStartAmount = 100000;

	public static readonly int kStationExportStartAmount = 200000;

	private static readonly int kStorageMinimalTransferAmount = 10000;

	private static readonly int kStationMinimalTransferAmount = 30000;

	private SimulationSystem m_SimulationSystem;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private EntityQuery m_CompanyGroup;

	private EntityQuery m_StationGroup;

	private EntityQuery m_OCStationGroup;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CompanyGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadOnly<CompanyData>(), ComponentType.Exclude<StorageTransfer>(), ComponentType.Exclude<Deleted>());
		m_StationGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<CompanyData>(), ComponentType.Exclude<StorageTransfer>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>());
		m_OCStationGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<CompanyData>(), ComponentType.Exclude<StorageTransfer>(), ComponentType.Exclude<CityServiceUpkeep>(), ComponentType.Exclude<Deleted>());
		RequireAnyForUpdate(m_CompanyGroup, m_StationGroup, m_OCStationGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		JobHandle jobHandle = JobChunkExtensions.Schedule(new StorageJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CompanyResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_TradeCostType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Limits = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageTransferRequests = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_StorageTransferRequest_RW_BufferLookup, ref base.CheckedStateRef),
			m_Targets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Trucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GuestVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_UpdateFrameIndex = updateFrameWithInterval,
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next()
		}, m_CompanyGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		JobHandle jobHandle2 = JobChunkExtensions.Schedule(new StationStorageJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CompanyResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TradeCostType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Limits = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageTransferRequests = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_StorageTransferRequest_RW_BufferLookup, ref base.CheckedStateRef),
			m_TripNeededsBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferLookup, ref base.CheckedStateRef),
			m_Targets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Trucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedRouteBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjectBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Connecteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CargoTransports = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CargoTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteWaypointBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_CurrentRoutes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Waypoints = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_UpdateInterval = GetUpdateInterval(SystemUpdatePhase.GameSimulation)
		}, m_StationGroup, JobHandle.CombineDependencies(jobHandle, base.Dependency));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		JobHandle jobHandle3 = JobChunkExtensions.Schedule(new OCStationStorageJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CompanyResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TradeCostType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Limits = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageTransferRequests = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_StorageTransferRequest_RW_BufferLookup, ref base.CheckedStateRef),
			m_Targets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjectBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedRouteBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Connecteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_UpdateInterval = GetUpdateInterval(SystemUpdatePhase.GameSimulation)
		}, m_OCStationGroup, JobHandle.CombineDependencies(jobHandle2, base.Dependency));
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle3);
		base.Dependency = JobHandle.CombineDependencies(jobHandle, jobHandle2, jobHandle3);
	}

	private static bool RemoveFromRequests(Resource resource, int amount, Entity owner, Entity target1, Entity target2, ref BufferLookup<StorageTransferRequest> storageTransferRequests)
	{
		DynamicBuffer<StorageTransferRequest> dynamicBuffer = storageTransferRequests[owner];
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			StorageTransferRequest value = dynamicBuffer[i];
			if ((value.m_Target == target1 || value.m_Target == target2) && value.m_Resource == resource && (value.m_Flags & StorageTransferFlags.Incoming) == 0)
			{
				if (value.m_Amount > amount)
				{
					value.m_Amount -= amount;
					dynamicBuffer[i] = value;
					return true;
				}
				amount -= value.m_Amount;
				dynamicBuffer.RemoveAtSwapBack(i);
				i--;
			}
		}
		return amount == 0;
	}

	private static bool ProcessStorage(int chunkIndex, Entity company, Entity building, Resource resource, StorageCompanyData storageCompanyData, DynamicBuffer<Resources> resourceBuffer, DynamicBuffer<StorageTransferRequest> requests, StorageLimitData limitData, SpawnableBuildingData spawnableData, BuildingData buildingData, DeliveryTruckSelectData truckSelectData, uint simulationFrame, DynamicBuffer<TradeCost> tradeCosts, EntityCommandBuffer.ParallelWriter commandBuffer, bool station, bool hasConnectedRoute, int incomingAmount, ref Random random, ref ComponentLookup<Game.Companies.StorageCompany> storageCompanies, ref BufferLookup<OwnedVehicle> ownedVehicles, ref BufferLookup<StorageTransferRequest> storageTransferRequests, ref ComponentLookup<Game.Vehicles.DeliveryTruck> trucks, ref ComponentLookup<Target> targets, ref BufferLookup<LayoutElement> layoutElements, ref ComponentLookup<PropertyRenter> propertyRenters, ref ComponentLookup<Game.Objects.OutsideConnection> outsideConnections)
	{
		bool flag = false;
		int num = EconomyUtils.CountResources(storageCompanyData.m_StoredResources);
		if (num == 0)
		{
			return false;
		}
		int num2 = limitData.GetAdjustedLimitForWarehouse(spawnableData, buildingData) / num;
		if ((storageCompanyData.m_StoredResources & resource) != Resource.NoResource)
		{
			int resources = EconomyUtils.GetResources(resource, resourceBuffer);
			int num3 = resources;
			for (int i = 0; i < requests.Length; i++)
			{
				StorageTransferRequest value = requests[i];
				if (value.m_Resource != resource)
				{
					continue;
				}
				if (!storageCompanies.HasComponent(value.m_Target) || !storageTransferRequests.HasBuffer(value.m_Target) || (!propertyRenters.HasComponent(value.m_Target) && !outsideConnections.HasComponent(value.m_Target)))
				{
					requests.RemoveAtSwapBack(i);
					i--;
					continue;
				}
				bool flag2 = (value.m_Flags & StorageTransferFlags.Incoming) != 0;
				if (flag2)
				{
					int num4 = 0;
					DynamicBuffer<StorageTransferRequest> dynamicBuffer = storageTransferRequests[value.m_Target];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						StorageTransferRequest storageTransferRequest = dynamicBuffer[j];
						if ((storageTransferRequest.m_Target == company || storageTransferRequest.m_Target == building) && storageTransferRequest.m_Resource == resource && (storageTransferRequest.m_Flags & StorageTransferFlags.Incoming) == 0)
						{
							num4 += storageTransferRequest.m_Amount;
						}
					}
					int num5 = 0;
					if (ownedVehicles.HasBuffer(value.m_Target))
					{
						DynamicBuffer<OwnedVehicle> dynamicBuffer2 = ownedVehicles[value.m_Target];
						for (int k = 0; k < dynamicBuffer2.Length; k++)
						{
							Entity vehicle = dynamicBuffer2[k].m_Vehicle;
							if (!trucks.HasComponent(vehicle) || !targets.HasComponent(vehicle))
							{
								continue;
							}
							Game.Vehicles.DeliveryTruck deliveryTruck = trucks[vehicle];
							Entity target = targets[vehicle].m_Target;
							if (!(target == company) && !(target == building))
							{
								continue;
							}
							int num6 = 0;
							if (deliveryTruck.m_Resource == resource)
							{
								num6 += deliveryTruck.m_Amount;
							}
							if (layoutElements.HasBuffer(vehicle))
							{
								DynamicBuffer<LayoutElement> dynamicBuffer3 = layoutElements[vehicle];
								for (int l = 0; l < dynamicBuffer3.Length; l++)
								{
									Entity vehicle2 = dynamicBuffer3[l].m_Vehicle;
									if (trucks.HasComponent(vehicle2))
									{
										deliveryTruck = trucks[vehicle2];
										if (deliveryTruck.m_Resource == resource)
										{
											num6 += deliveryTruck.m_Amount;
										}
									}
								}
							}
							num5 += num6;
						}
					}
					if (station && num4 + num5 < value.m_Amount && incomingAmount > 0)
					{
						int num7 = math.min(value.m_Amount - num5 - num4, incomingAmount);
						num5 += num7;
						incomingAmount -= num7;
					}
					if (num5 + num4 == 0)
					{
						requests.RemoveAtSwapBack(i);
						i--;
						continue;
					}
					if (num5 + num4 < value.m_Amount)
					{
						value.m_Amount = num5 + num4;
						requests[i] = value;
					}
				}
				else
				{
					int num8 = 0;
					DynamicBuffer<StorageTransferRequest> dynamicBuffer4 = storageTransferRequests[value.m_Target];
					for (int m = 0; m < dynamicBuffer4.Length; m++)
					{
						StorageTransferRequest storageTransferRequest2 = dynamicBuffer4[m];
						if ((storageTransferRequest2.m_Target == company || storageTransferRequest2.m_Target == building) && storageTransferRequest2.m_Resource == resource && (storageTransferRequest2.m_Flags & StorageTransferFlags.Incoming) != 0)
						{
							num8 = storageTransferRequest2.m_Amount;
							break;
						}
					}
					if (num8 == 0)
					{
						requests.RemoveAtSwapBack(i);
						i--;
						continue;
					}
					if (num8 < value.m_Amount)
					{
						value.m_Amount = num8;
						requests[i] = value;
					}
				}
				num3 += (flag2 ? value.m_Amount : (-value.m_Amount));
			}
			int num9 = num2 - resources;
			TradeCost tradeCost = EconomyUtils.GetTradeCost(resource, tradeCosts);
			EconomyUtils.GetLastTradeRequestTime(tradeCosts);
			if (station && tradeCost.m_LastTransferRequestTime == 0L)
			{
				tradeCost.m_LastTransferRequestTime = simulationFrame - kTransferCooldown / 2;
				EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
			}
			if (simulationFrame - tradeCost.m_LastTransferRequestTime >= kTransferCooldown + random.NextInt(storageCompanyData.m_TransportInterval.x, storageCompanyData.m_TransportInterval.y) || tradeCost.m_LastTransferRequestTime == 0L)
			{
				int num10 = (station ? kStationExportStartAmount : kStorageExportStartAmount);
				num10 = (int)math.min(math.ceil((float)num2 * 0.8f / 10000f) * 10000f, num10);
				int num11 = (station ? kStationLowStockAmount : kStorageLowStockAmount);
				num11 = (int)math.min(math.ceil((float)num2 * 0.5f / 10000f) * 10000f, num11);
				int num12 = (station ? kStationMinimalTransferAmount : kStorageMinimalTransferAmount);
				num12 = (int)math.min(math.ceil((float)num2 * 0.5f / 10000f) * 10000f, num12);
				int num13 = (num10 - num11) / 2 + num11;
				if (resources > num10 && num3 > num10)
				{
					int num14 = 0;
					int max = 0;
					if (!station)
					{
						truckSelectData.GetCapacityRange(resource, out var _, out max);
						truckSelectData.TrySelectItem(ref random, resource, math.min(resources - num13, max), out var item);
						if (item.m_Capacity > 0)
						{
							max = item.m_Capacity * math.max(max / item.m_Capacity, 1);
						}
						num14 = item.m_Cost;
					}
					else
					{
						max = kStationMinimalTransferAmount;
					}
					if (station || (float)num14 / (float)math.min(resources, max) < kMaxTransportUnitCost)
					{
						commandBuffer.AddComponent(chunkIndex, company, new StorageTransfer
						{
							m_Resource = resource,
							m_Amount = max
						});
						tradeCost.m_LastTransferRequestTime = simulationFrame;
						EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
						flag = true;
					}
				}
				else if (resources < num11 && num3 < num11)
				{
					if (station && !hasConnectedRoute)
					{
						return false;
					}
					StorageTransfer component = new StorageTransfer
					{
						m_Resource = resource
					};
					int num15 = math.min((int)((float)num9 * 0.9f), math.max(num13 - resources, num12));
					component.m_Amount = -num15;
					if (!station)
					{
						truckSelectData.TrySelectItem(ref random, resource, num15, out var item2);
						if (item2.m_Capacity > 0)
						{
							component.m_Amount = -math.max(num15 / item2.m_Capacity, 1) * item2.m_Capacity;
						}
					}
					commandBuffer.AddComponent(chunkIndex, company, component);
					tradeCost.m_LastTransferRequestTime = simulationFrame;
					flag = true;
				}
				if (random.NextInt(kCostFadeProbability) == 0)
				{
					tradeCost.m_BuyCost *= 0.99f;
					tradeCost.m_SellCost *= 0.99f;
					if (!flag)
					{
						EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
					}
				}
				if (flag)
				{
					EconomyUtils.SetTradeCost(resource, tradeCost, tradeCosts, keepLastTime: false);
				}
			}
		}
		return flag;
	}

	public void PostDeserialize(Context context)
	{
		if (!(context.version < Version.storageConditionReset))
		{
			return;
		}
		NativeArray<Entity> nativeArray = m_CompanyGroup.ToEntityArray(Allocator.Temp);
		foreach (Entity item in nativeArray)
		{
			base.EntityManager.GetBuffer<TradeCost>(item).Clear();
		}
		nativeArray.Dispose();
		nativeArray = m_StationGroup.ToEntityArray(Allocator.Temp);
		foreach (Entity item2 in nativeArray)
		{
			base.EntityManager.GetBuffer<TradeCost>(item2).Clear();
		}
		nativeArray.Dispose();
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
	public StorageCompanySystem()
	{
	}
}
