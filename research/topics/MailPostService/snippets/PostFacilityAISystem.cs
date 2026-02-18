#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PostFacilityAISystem : GameSystemBase
{
	private struct PostFacilityAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public static PostFacilityAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new PostFacilityAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled
			};
		}
	}

	[BurstCompile]
	private struct PostFacilityTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public ComponentTypeHandle<Game.Buildings.PostFacility> m_PostFacilityType;

		public ComponentTypeHandle<Game.Routes.MailBox> m_MailBoxType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Game.Economy.Resources> m_ResourcesType;

		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		public BufferTypeHandle<GuestVehicle> m_GuestVehicleType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> m_PostVanRequestData;

		[ReadOnly]
		public ComponentLookup<MailTransferRequest> m_MailTransferRequestData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PostVan> m_PostVanData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<ReturnLoad> m_ReturnLoadData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PostFacilityData> m_PrefabPostFacilityData;

		[ReadOnly]
		public ComponentLookup<MailBoxData> m_PrefabMailBoxData;

		[ReadOnly]
		public ComponentLookup<PostVanData> m_PrefabPostVanData;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> m_PrefabDeliveryTruckData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_PrefabObjectData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public PostVanSelectData m_PostVanSelectData;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public PostConfigurationData m_PostConfigurationData;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public EntityArchetype m_PostVanRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_MailTransferRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<PostFacilityAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.PostFacility> nativeArray3 = chunk.GetNativeArray(ref m_PostFacilityType);
			NativeArray<Game.Routes.MailBox> nativeArray4 = chunk.GetNativeArray(ref m_MailBoxType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<GuestVehicle> bufferAccessor4 = chunk.GetBufferAccessor(ref m_GuestVehicleType);
			BufferAccessor<ServiceDispatch> bufferAccessor5 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor6 = chunk.GetBufferAccessor(ref m_ResourcesType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Game.Buildings.PostFacility postFacility = nativeArray3[i];
				DynamicBuffer<OwnedVehicle> ownedVehicles = bufferAccessor3[i];
				DynamicBuffer<GuestVehicle> guestVehicles = bufferAccessor4[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor5[i];
				DynamicBuffer<Game.Economy.Resources> resources = bufferAccessor6[i];
				PostFacilityData data = m_PrefabPostFacilityData[prefabRef.m_Prefab];
				MailBoxData componentData = default(MailBoxData);
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabPostFacilityData);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				if (CollectionUtils.TryGet(nativeArray4, i, out var value))
				{
					m_PrefabMailBoxData.TryGetComponent(prefabRef.m_Prefab, out componentData);
				}
				Tick(unfilteredChunkIndex, entity, ref random, ref postFacility, ref value, data, componentData, ownedVehicles, guestVehicles, dispatches, resources, efficiency, immediateEfficiency);
				nativeArray3[i] = postFacility;
				CollectionUtils.TrySet(nativeArray4, i, value);
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Unity.Mathematics.Random random, ref Game.Buildings.PostFacility postFacility, ref Game.Routes.MailBox mailBox, PostFacilityData prefabPostFacilityData, MailBoxData prefabMailBoxData, DynamicBuffer<OwnedVehicle> ownedVehicles, DynamicBuffer<GuestVehicle> guestVehicles, DynamicBuffer<ServiceDispatch> dispatches, DynamicBuffer<Game.Economy.Resources> resources, float efficiency, float immediateEfficiency)
		{
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabPostFacilityData.m_PostVanCapacity);
			int num = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabPostFacilityData.m_PostVanCapacity);
			int availableDeliveryVans = vehicleCapacity;
			int availableDeliveryTrucks = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabPostFacilityData.m_PostTruckCapacity);
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			StackList<Entity> parkedPostVans = stackalloc Entity[ownedVehicles.Length];
			for (int i = 0; i < ownedVehicles.Length; i++)
			{
				Entity vehicle = ownedVehicles[i].m_Vehicle;
				Game.Vehicles.DeliveryTruck componentData3;
				if (m_PostVanData.TryGetComponent(vehicle, out var componentData))
				{
					if (m_ParkedCarData.TryGetComponent(vehicle, out var componentData2))
					{
						if (!m_EntityLookup.Exists(componentData2.m_Lane))
						{
							m_CommandBuffer.AddComponent<Deleted>(jobIndex, vehicle);
						}
						else
						{
							parkedPostVans.AddNoResize(vehicle);
						}
						continue;
					}
					PrefabRef prefabRef = m_PrefabRefData[vehicle];
					PostVanData postVanData = m_PrefabPostVanData[prefabRef.m_Prefab];
					availableDeliveryVans--;
					num3 += componentData.m_DeliveringMail;
					num2 += postVanData.m_MailCapacity;
					bool flag = --num < 0;
					if ((componentData.m_State & PostVanFlags.Disabled) != 0 != flag)
					{
						m_ActionQueue.Enqueue(PostFacilityAction.SetDisabled(vehicle, flag));
					}
				}
				else if (m_DeliveryTruckData.TryGetComponent(vehicle, out componentData3))
				{
					if ((componentData3.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
					{
						continue;
					}
					if (m_LayoutElements.TryGetBuffer(vehicle, out var bufferData) && bufferData.Length != 0)
					{
						for (int j = 0; j < bufferData.Length; j++)
						{
							Entity vehicle2 = bufferData[j].m_Vehicle;
							if (!m_DeliveryTruckData.TryGetComponent(vehicle2, out var componentData4))
							{
								continue;
							}
							if ((componentData3.m_State & DeliveryTruckFlags.Buying) != 0)
							{
								if ((componentData4.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
								{
									num2 += componentData4.m_Amount;
								}
								else if ((componentData4.m_Resource & Resource.LocalMail) != Resource.NoResource)
								{
									num3 += componentData4.m_Amount;
								}
							}
							if (m_ReturnLoadData.TryGetComponent(vehicle2, out var componentData5))
							{
								if ((componentData5.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
								{
									num2 += componentData5.m_Amount;
								}
								else if ((componentData5.m_Resource & Resource.LocalMail) != Resource.NoResource)
								{
									num3 += componentData5.m_Amount;
								}
							}
						}
					}
					else
					{
						if ((componentData3.m_State & DeliveryTruckFlags.Buying) != 0)
						{
							if ((componentData3.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
							{
								num2 += componentData3.m_Amount;
							}
							else if ((componentData3.m_Resource & Resource.LocalMail) != Resource.NoResource)
							{
								num3 += componentData3.m_Amount;
							}
						}
						if (m_ReturnLoadData.TryGetComponent(vehicle, out var componentData6))
						{
							if ((componentData6.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
							{
								num2 += componentData6.m_Amount;
							}
							else if ((componentData6.m_Resource & Resource.LocalMail) != Resource.NoResource)
							{
								num3 += componentData6.m_Amount;
							}
						}
					}
					availableDeliveryTrucks--;
				}
				else if (!m_EntityLookup.Exists(vehicle))
				{
					ownedVehicles.RemoveAt(i--);
				}
			}
			for (int k = 0; k < guestVehicles.Length; k++)
			{
				Entity vehicle3 = guestVehicles[k].m_Vehicle;
				if (!m_TargetData.HasComponent(vehicle3) || m_TargetData[vehicle3].m_Target != entity)
				{
					guestVehicles.RemoveAt(k--);
				}
				else
				{
					if (!m_DeliveryTruckData.TryGetComponent(vehicle3, out var componentData7) || (componentData7.m_State & DeliveryTruckFlags.DummyTraffic) != 0)
					{
						continue;
					}
					if (m_LayoutElements.TryGetBuffer(vehicle3, out var bufferData2) && bufferData2.Length != 0)
					{
						for (int l = 0; l < bufferData2.Length; l++)
						{
							Entity vehicle4 = bufferData2[l].m_Vehicle;
							if (!m_DeliveryTruckData.TryGetComponent(vehicle4, out var componentData8))
							{
								continue;
							}
							if ((componentData7.m_State & DeliveryTruckFlags.Buying) != 0)
							{
								if ((componentData8.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
								{
									num4 += componentData8.m_Amount;
								}
								else if ((componentData8.m_Resource & Resource.LocalMail) != Resource.NoResource)
								{
									num5 += componentData8.m_Amount;
								}
								else if ((componentData8.m_Resource & Resource.OutgoingMail) != Resource.NoResource)
								{
									num6 += componentData8.m_Amount;
								}
							}
							else if ((componentData8.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
							{
								num2 += componentData8.m_Amount;
							}
							else if ((componentData8.m_Resource & Resource.LocalMail) != Resource.NoResource)
							{
								num3 += componentData8.m_Amount;
							}
							if (m_ReturnLoadData.TryGetComponent(vehicle4, out var componentData9))
							{
								if ((componentData9.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
								{
									num4 += componentData9.m_Amount;
								}
								else if ((componentData9.m_Resource & Resource.LocalMail) != Resource.NoResource)
								{
									num5 += componentData9.m_Amount;
								}
								else if ((componentData9.m_Resource & Resource.OutgoingMail) != Resource.NoResource)
								{
									num6 += componentData9.m_Amount;
								}
							}
						}
						continue;
					}
					if ((componentData7.m_State & DeliveryTruckFlags.Buying) != 0)
					{
						if ((componentData7.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
						{
							num4 += componentData7.m_Amount;
						}
						else if ((componentData7.m_Resource & Resource.LocalMail) != Resource.NoResource)
						{
							num5 += componentData7.m_Amount;
						}
						else if ((componentData7.m_Resource & Resource.OutgoingMail) != Resource.NoResource)
						{
							num6 += componentData7.m_Amount;
						}
					}
					else if ((componentData7.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
					{
						num2 += componentData7.m_Amount;
					}
					else if ((componentData7.m_Resource & Resource.LocalMail) != Resource.NoResource)
					{
						num3 += componentData7.m_Amount;
					}
					if (m_ReturnLoadData.TryGetComponent(vehicle3, out var componentData10))
					{
						if ((componentData10.m_Resource & Resource.UnsortedMail) != Resource.NoResource)
						{
							num4 += componentData10.m_Amount;
						}
						else if ((componentData10.m_Resource & Resource.LocalMail) != Resource.NoResource)
						{
							num5 += componentData10.m_Amount;
						}
						else if ((componentData10.m_Resource & Resource.OutgoingMail) != Resource.NoResource)
						{
							num6 += componentData10.m_Amount;
						}
					}
				}
			}
			postFacility.m_Flags &= ~(PostFacilityFlags.CanDeliverMailWithVan | PostFacilityFlags.CanCollectMailWithVan | PostFacilityFlags.HasAvailableTrucks | PostFacilityFlags.AcceptsUnsortedMail | PostFacilityFlags.DeliversLocalMail | PostFacilityFlags.AcceptsLocalMail | PostFacilityFlags.DeliversUnsortedMail);
			postFacility.m_AcceptMailPriority = 0f;
			postFacility.m_DeliverMailPriority = 0f;
			m_DeliveryTruckSelectData.GetCapacityRange(Resource.LocalMail, out var min, out var max);
			m_DeliveryTruckSelectData.GetCapacityRange(Resource.OutgoingMail, out var min2, out var max2);
			m_DeliveryTruckSelectData.GetCapacityRange(Resource.UnsortedMail, out var min3, out var max3);
			int x = prefabPostFacilityData.m_MailCapacity / 10;
			min = math.min(x, max);
			min2 = math.min(x, max2);
			min3 = math.min(x, max3);
			if (prefabPostFacilityData.m_SortingRate != 0)
			{
				float num7 = 0.0009765625f;
				int num8 = Mathf.RoundToInt(num7 * (float)prefabPostFacilityData.m_SortingRate);
				int num9 = EconomyUtils.GetResources(Resource.UnsortedMail, resources);
				int num10 = math.min(num9, Mathf.RoundToInt(efficiency * num7 * (float)prefabPostFacilityData.m_SortingRate));
				postFacility.m_ProcessingFactor = (byte)math.clamp((num10 * 100 + num8 - 1) / num8, 0, 255);
				int num12;
				int num13;
				if (num10 != 0)
				{
					int num11 = (num10 * m_PostConfigurationData.m_OutgoingMailPercentage + random.NextInt(100)) / 100;
					num9 = EconomyUtils.AddResources(Resource.UnsortedMail, -num10, resources);
					num12 = EconomyUtils.AddResources(Resource.LocalMail, num10 - num11, resources);
					num13 = EconomyUtils.AddResources(Resource.OutgoingMail, num11, resources);
				}
				else
				{
					num12 = EconomyUtils.GetResources(Resource.LocalMail, resources);
					num13 = EconomyUtils.GetResources(Resource.OutgoingMail, resources);
				}
				int num14 = num9 + num12 + num3 + num2;
				int num15 = prefabPostFacilityData.m_MailCapacity - num14;
				int num16 = math.min(mailBox.m_MailAmount, num15);
				if (num16 > 0)
				{
					mailBox.m_MailAmount -= num16;
					num9 = EconomyUtils.AddResources(Resource.UnsortedMail, num16, resources);
					num14 += num9;
					num15 -= num16;
				}
				num15 -= prefabMailBoxData.m_MailCapacity;
				num9 -= num4;
				num12 -= num5;
				num13 -= num6;
				int num17 = num9 + num2;
				for (int m = 0; m < dispatches.Length; m++)
				{
					Entity request = dispatches[m].m_Request;
					if (m_PostVanRequestData.HasComponent(request))
					{
						TrySpawnPostVan(jobIndex, ref random, entity, request, resources, ref postFacility, ref availableDeliveryVans, ref num12, ref num15, ref parkedPostVans);
						dispatches.RemoveAt(m--);
					}
					else if (m_MailTransferRequestData.HasComponent(request))
					{
						TrySpawnDeliveryTruck(jobIndex, ref random, entity, request, resources, ref availableDeliveryTrucks, ref num15);
						dispatches.RemoveAt(m--);
					}
					else if (!m_ServiceRequestData.HasComponent(request))
					{
						dispatches.RemoveAt(m--);
					}
				}
				if (num12 >= min || num13 >= min2)
				{
					MailTransferRequestFlags mailTransferRequestFlags;
					int amount;
					if (num12 >= num13)
					{
						postFacility.m_DeliverMailPriority = (float)num12 / (float)prefabPostFacilityData.m_MailCapacity;
						mailTransferRequestFlags = MailTransferRequestFlags.Receive | MailTransferRequestFlags.LocalMail;
						if (availableDeliveryTrucks <= 0)
						{
							mailTransferRequestFlags |= MailTransferRequestFlags.RequireTransport;
						}
						if (num15 >= min3)
						{
							mailTransferRequestFlags |= MailTransferRequestFlags.ReturnUnsortedMail;
						}
						amount = math.min(num12, max);
					}
					else
					{
						postFacility.m_DeliverMailPriority = (float)num13 / (float)prefabPostFacilityData.m_MailCapacity;
						mailTransferRequestFlags = MailTransferRequestFlags.Receive | MailTransferRequestFlags.OutgoingMail;
						if (availableDeliveryTrucks <= 0)
						{
							mailTransferRequestFlags |= MailTransferRequestFlags.RequireTransport;
						}
						if (num15 >= min)
						{
							mailTransferRequestFlags |= MailTransferRequestFlags.ReturnLocalMail;
						}
						amount = math.min(num13, max2);
					}
					if (m_MailTransferRequestData.HasComponent(postFacility.m_MailReceiveRequest))
					{
						if (m_MailTransferRequestData[postFacility.m_MailReceiveRequest].m_Flags != mailTransferRequestFlags)
						{
							Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
							m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(postFacility.m_MailReceiveRequest, Entity.Null, completed: true));
						}
						else
						{
							mailTransferRequestFlags = (MailTransferRequestFlags)0;
						}
					}
					if (mailTransferRequestFlags != 0)
					{
						Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_MailTransferRequestArchetype);
						m_CommandBuffer.SetComponent(jobIndex, e2, new MailTransferRequest(entity, mailTransferRequestFlags, postFacility.m_DeliverMailPriority, amount));
						m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(8u));
					}
				}
				if (num15 >= min3)
				{
					postFacility.m_AcceptMailPriority = 1f - (float)num17 / (float)prefabPostFacilityData.m_MailCapacity;
					MailTransferRequestFlags mailTransferRequestFlags2 = MailTransferRequestFlags.Deliver | MailTransferRequestFlags.RequireTransport | MailTransferRequestFlags.UnsortedMail;
					if (num12 >= min)
					{
						mailTransferRequestFlags2 |= MailTransferRequestFlags.ReturnLocalMail;
					}
					int amount2 = math.min(num15, max3);
					if (m_MailTransferRequestData.HasComponent(postFacility.m_MailDeliverRequest))
					{
						if (m_MailTransferRequestData[postFacility.m_MailDeliverRequest].m_Flags != mailTransferRequestFlags2)
						{
							Entity e3 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
							m_CommandBuffer.SetComponent(jobIndex, e3, new HandleRequest(postFacility.m_MailDeliverRequest, Entity.Null, completed: true));
						}
						else
						{
							mailTransferRequestFlags2 = (MailTransferRequestFlags)0;
						}
					}
					if (mailTransferRequestFlags2 != 0)
					{
						Entity e4 = m_CommandBuffer.CreateEntity(jobIndex, m_MailTransferRequestArchetype);
						m_CommandBuffer.SetComponent(jobIndex, e4, new MailTransferRequest(entity, mailTransferRequestFlags2, postFacility.m_AcceptMailPriority, amount2));
						m_CommandBuffer.SetComponent(jobIndex, e4, new RequestGroup(8u));
					}
				}
				if (num15 >= min3)
				{
					postFacility.m_Flags |= PostFacilityFlags.AcceptsUnsortedMail;
				}
				if (num12 >= min)
				{
					postFacility.m_Flags |= PostFacilityFlags.DeliversLocalMail;
				}
				if (availableDeliveryVans > 0)
				{
					if (num12 > 0)
					{
						postFacility.m_Flags |= PostFacilityFlags.CanDeliverMailWithVan;
					}
					if (num15 > 0)
					{
						postFacility.m_Flags |= PostFacilityFlags.CanCollectMailWithVan;
					}
				}
				if (availableDeliveryTrucks > 0)
				{
					postFacility.m_Flags |= PostFacilityFlags.HasAvailableTrucks;
				}
			}
			else
			{
				postFacility.m_ProcessingFactor = 0;
				int num18 = EconomyUtils.GetResources(Resource.UnsortedMail, resources);
				int resources2 = EconomyUtils.GetResources(Resource.LocalMail, resources);
				int num19 = num18 + resources2 + num3 + num2;
				int num20 = prefabPostFacilityData.m_MailCapacity - num19;
				int num21 = math.min(mailBox.m_MailAmount, num20);
				if (num21 > 0)
				{
					mailBox.m_MailAmount -= num21;
					num18 = EconomyUtils.AddResources(Resource.UnsortedMail, num21, resources);
					num19 += num18;
					num20 -= num21;
				}
				num20 -= prefabMailBoxData.m_MailCapacity;
				num18 -= num4;
				resources2 -= num5;
				int num22 = resources2 + num3;
				for (int n = 0; n < dispatches.Length; n++)
				{
					Entity request2 = dispatches[n].m_Request;
					if (m_PostVanRequestData.HasComponent(request2))
					{
						TrySpawnPostVan(jobIndex, ref random, entity, request2, resources, ref postFacility, ref availableDeliveryVans, ref resources2, ref num20, ref parkedPostVans);
						dispatches.RemoveAt(n--);
					}
					else if (m_MailTransferRequestData.HasComponent(request2))
					{
						TrySpawnDeliveryTruck(jobIndex, ref random, entity, request2, resources, ref availableDeliveryTrucks, ref num20);
						dispatches.RemoveAt(n--);
					}
					else if (!m_ServiceRequestData.HasComponent(request2))
					{
						dispatches.RemoveAt(n--);
					}
				}
				int num23 = math.max(0, min3 - num18);
				int num24 = (prefabPostFacilityData.m_MailCapacity >> 1) - num22;
				int num25 = math.min(num24, num20 - num23);
				if (num25 >= min)
				{
					postFacility.m_AcceptMailPriority = 1f - (float)num22 / (float)prefabPostFacilityData.m_MailCapacity;
					MailTransferRequestFlags mailTransferRequestFlags3 = MailTransferRequestFlags.Deliver | MailTransferRequestFlags.RequireTransport | MailTransferRequestFlags.LocalMail;
					if (num18 >= min3)
					{
						mailTransferRequestFlags3 |= MailTransferRequestFlags.ReturnUnsortedMail;
					}
					int amount3 = math.min(num25, max);
					if (m_MailTransferRequestData.HasComponent(postFacility.m_MailDeliverRequest))
					{
						if (m_MailTransferRequestData[postFacility.m_MailDeliverRequest].m_Flags != mailTransferRequestFlags3)
						{
							Entity e5 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
							m_CommandBuffer.SetComponent(jobIndex, e5, new HandleRequest(postFacility.m_MailDeliverRequest, Entity.Null, completed: true));
						}
						else
						{
							mailTransferRequestFlags3 = (MailTransferRequestFlags)0;
						}
					}
					if (mailTransferRequestFlags3 != 0)
					{
						Entity e6 = m_CommandBuffer.CreateEntity(jobIndex, m_MailTransferRequestArchetype);
						m_CommandBuffer.SetComponent(jobIndex, e6, new MailTransferRequest(entity, mailTransferRequestFlags3, postFacility.m_AcceptMailPriority, amount3));
						m_CommandBuffer.SetComponent(jobIndex, e6, new RequestGroup(8u));
					}
				}
				else if (num18 >= min3)
				{
					postFacility.m_DeliverMailPriority = (float)num18 / (float)prefabPostFacilityData.m_MailCapacity;
					MailTransferRequestFlags mailTransferRequestFlags4 = MailTransferRequestFlags.Receive | MailTransferRequestFlags.UnsortedMail;
					if (availableDeliveryTrucks <= 0)
					{
						mailTransferRequestFlags4 |= MailTransferRequestFlags.RequireTransport;
					}
					if (num24 >= min)
					{
						mailTransferRequestFlags4 |= MailTransferRequestFlags.ReturnLocalMail;
					}
					int amount4 = math.min(num18, max3);
					if (m_MailTransferRequestData.HasComponent(postFacility.m_MailReceiveRequest))
					{
						if (m_MailTransferRequestData[postFacility.m_MailReceiveRequest].m_Flags != mailTransferRequestFlags4)
						{
							Entity e7 = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
							m_CommandBuffer.SetComponent(jobIndex, e7, new HandleRequest(postFacility.m_MailReceiveRequest, Entity.Null, completed: true));
						}
						else
						{
							mailTransferRequestFlags4 = (MailTransferRequestFlags)0;
						}
					}
					if (mailTransferRequestFlags4 != 0)
					{
						Entity e8 = m_CommandBuffer.CreateEntity(jobIndex, m_MailTransferRequestArchetype);
						m_CommandBuffer.SetComponent(jobIndex, e8, new MailTransferRequest(entity, mailTransferRequestFlags4, postFacility.m_DeliverMailPriority, amount4));
						m_CommandBuffer.SetComponent(jobIndex, e8, new RequestGroup(8u));
					}
				}
				if (num25 >= min)
				{
					postFacility.m_Flags |= PostFacilityFlags.AcceptsLocalMail;
				}
				if (num18 >= min3)
				{
					postFacility.m_Flags |= PostFacilityFlags.DeliversUnsortedMail;
				}
				if (availableDeliveryVans > 0)
				{
					if (resources2 > 0)
					{
						postFacility.m_Flags |= PostFacilityFlags.CanDeliverMailWithVan;
					}
					if (num20 > 0)
					{
						postFacility.m_Flags |= PostFacilityFlags.CanCollectMailWithVan;
					}
				}
				if (availableDeliveryTrucks > 0)
				{
					postFacility.m_Flags |= PostFacilityFlags.HasAvailableTrucks;
				}
			}
			while (parkedPostVans.Length > math.max(0, prefabPostFacilityData.m_PostVanCapacity + availableDeliveryVans - vehicleCapacity))
			{
				int index = random.NextInt(parkedPostVans.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedPostVans[index]);
				parkedPostVans.RemoveAtSwapBack(index);
			}
			for (int num26 = 0; num26 < parkedPostVans.Length; num26++)
			{
				Entity entity2 = parkedPostVans[num26];
				Game.Vehicles.PostVan postVan = m_PostVanData[entity2];
				bool flag2 = (postFacility.m_Flags & (PostFacilityFlags.CanDeliverMailWithVan | PostFacilityFlags.CanCollectMailWithVan)) == 0;
				if ((postVan.m_State & PostVanFlags.Disabled) != 0 != flag2)
				{
					m_ActionQueue.Enqueue(PostFacilityAction.SetDisabled(entity2, flag2));
				}
			}
			if ((postFacility.m_Flags & (PostFacilityFlags.CanDeliverMailWithVan | PostFacilityFlags.CanCollectMailWithVan)) != 0)
			{
				RequestTargetIfNeeded(jobIndex, entity, ref postFacility, availableDeliveryVans);
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.PostFacility postFacility, int availablePostVans)
		{
			if (!m_ServiceRequestData.HasComponent(postFacility.m_TargetRequest))
			{
				uint num = math.max(512u, 256u);
				if ((m_SimulationFrameIndex & (num - 1)) == 176)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PostVanRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new PostVanRequest(entity, (PostVanRequestFlags)0, (ushort)availablePostVans));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
		}

		private bool TrySpawnPostVan(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Entity request, DynamicBuffer<Game.Economy.Resources> resources, ref Game.Buildings.PostFacility postFacility, ref int availableDeliveryVans, ref int localMail, ref int freeSpace, ref StackList<Entity> parkedPostVans)
		{
			int2 mailCapacity = new int2(1, math.max(localMail, freeSpace));
			if (availableDeliveryVans <= 0)
			{
				return false;
			}
			if (mailCapacity.y <= 0)
			{
				return false;
			}
			PostVanRequest postVanRequest = m_PostVanRequestData[request];
			if (!m_EntityLookup.Exists(postVanRequest.m_Target))
			{
				return false;
			}
			if (localMail <= 0 && (postVanRequest.m_Flags & PostVanRequestFlags.Deliver) != 0)
			{
				return false;
			}
			bool flag = (postVanRequest.m_Flags & PostVanRequestFlags.Collect) != 0;
			Entity entity2 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(request, out var componentData) && componentData.m_Origin != entity)
			{
				if (m_PrefabRefData.TryGetComponent(componentData.m_Origin, out var componentData2) && m_PrefabPostVanData.TryGetComponent(componentData2.m_Prefab, out var componentData3))
				{
					mailCapacity = componentData3.m_MailCapacity;
					if (flag && mailCapacity.y > freeSpace)
					{
						return false;
					}
				}
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedPostVans, componentData.m_Origin))
				{
					return false;
				}
				ParkedCar parkedCar = m_ParkedCarData[componentData.m_Origin];
				entity2 = componentData.m_Origin;
				m_CommandBuffer.RemoveComponent(jobIndex, entity2, in m_ParkedToMovingRemoveTypes);
				Game.Vehicles.CarLaneFlags flags = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
				m_CommandBuffer.AddComponent(jobIndex, entity2, in m_ParkedToMovingCarAddTypes);
				m_CommandBuffer.SetComponent(jobIndex, entity2, new CarCurrentLane(parkedCar, flags));
				if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar.m_Lane))
				{
					m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar.m_Lane);
				}
			}
			if (entity2 == Entity.Null)
			{
				Entity vehiclePrefab = m_PostVanSelectData.SelectVehicle(ref random, ref mailCapacity);
				if (flag && mailCapacity.y > freeSpace)
				{
					return false;
				}
				entity2 = m_PostVanSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, m_TransformData[entity], entity, vehiclePrefab, parked: false);
				if (entity2 == Entity.Null)
				{
					return false;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(entity));
			}
			int num = math.min(localMail, mailCapacity.y);
			availableDeliveryVans--;
			localMail -= num;
			freeSpace -= mailCapacity.y;
			EconomyUtils.AddResources(Resource.LocalMail, -num, resources);
			PostVanFlags postVanFlags = (PostVanFlags)0u;
			if ((postVanRequest.m_Flags & PostVanRequestFlags.Deliver) != 0)
			{
				postVanFlags |= PostVanFlags.Delivering;
			}
			if ((postVanRequest.m_Flags & PostVanRequestFlags.Collect) != 0)
			{
				postVanFlags |= PostVanFlags.Collecting;
			}
			m_CommandBuffer.SetComponent(jobIndex, entity2, new Game.Vehicles.PostVan(postVanFlags, 1, num));
			m_CommandBuffer.SetComponent(jobIndex, entity2, new Target(postVanRequest.m_Target));
			m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity2).Add(new ServiceDispatch(request));
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity2, completed: false));
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity2);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity2, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, entity2, componentData);
			}
			if (m_ServiceRequestData.HasComponent(postFacility.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(postFacility.m_TargetRequest, Entity.Null, completed: true));
			}
			return true;
		}

		private bool TrySpawnDeliveryTruck(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Entity request, DynamicBuffer<Game.Economy.Resources> resources, ref int availableDeliveryTrucks, ref int freeSpace)
		{
			if (availableDeliveryTrucks <= 0)
			{
				return false;
			}
			MailTransferRequest mailTransferRequest = m_MailTransferRequestData[request];
			PathInformation component = m_PathInformationData[request];
			if (!m_PrefabRefData.HasComponent(component.m_Destination))
			{
				return false;
			}
			DeliveryTruckFlags deliveryTruckFlags = (DeliveryTruckFlags)0u;
			Resource resource = Resource.NoResource;
			Resource resource2 = Resource.NoResource;
			int amount = mailTransferRequest.m_Amount;
			int returnAmount = 0;
			if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.UnsortedMail) != 0)
			{
				resource = Resource.UnsortedMail;
			}
			if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.LocalMail) != 0)
			{
				resource = Resource.LocalMail;
			}
			if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.OutgoingMail) != 0)
			{
				resource = Resource.OutgoingMail;
			}
			if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.RequireTransport) != 0)
			{
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Deliver) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Loaded;
				}
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Receive) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Buying;
				}
			}
			else
			{
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Deliver) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Buying;
				}
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.Receive) != 0)
				{
					deliveryTruckFlags |= DeliveryTruckFlags.Loaded;
				}
			}
			int max;
			if ((deliveryTruckFlags & DeliveryTruckFlags.Loaded) != 0)
			{
				amount = math.min(amount, EconomyUtils.GetResources(resource, resources));
				if (amount <= 0)
				{
					return false;
				}
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnUnsortedMail) != 0)
				{
					resource2 = Resource.UnsortedMail;
				}
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnLocalMail) != 0)
				{
					resource2 = Resource.LocalMail;
				}
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnOutgoingMail) != 0)
				{
					resource2 = Resource.OutgoingMail;
				}
				if (resource2 != Resource.NoResource)
				{
					m_DeliveryTruckSelectData.GetCapacityRange(resource | resource2, out var min, out max);
					returnAmount = math.min(amount + freeSpace, math.max(amount, min));
					if (returnAmount <= 0)
					{
						resource2 = Resource.NoResource;
						returnAmount = 0;
					}
				}
			}
			else
			{
				resource2 = resource;
				returnAmount = amount;
				resource = Resource.NoResource;
				amount = 0;
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnUnsortedMail) != 0)
				{
					resource = Resource.UnsortedMail;
				}
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnLocalMail) != 0)
				{
					resource = Resource.LocalMail;
				}
				if ((mailTransferRequest.m_Flags & MailTransferRequestFlags.ReturnOutgoingMail) != 0)
				{
					resource = Resource.OutgoingMail;
				}
				if (resource != Resource.NoResource)
				{
					m_DeliveryTruckSelectData.GetCapacityRange(resource | resource2, out var min2, out max);
					amount = math.min(EconomyUtils.GetResources(resource, resources), math.max(returnAmount, min2));
					if (amount <= 0)
					{
						resource = Resource.NoResource;
						amount = 0;
					}
				}
				returnAmount = math.min(returnAmount, amount + freeSpace);
				if (returnAmount <= 0)
				{
					resource2 = Resource.NoResource;
					returnAmount = 0;
					if (amount == 0)
					{
						return false;
					}
				}
				deliveryTruckFlags = (DeliveryTruckFlags)((uint)deliveryTruckFlags & 0xFFFFFFEFu);
				deliveryTruckFlags |= DeliveryTruckFlags.Loaded;
			}
			if (amount > 0)
			{
				deliveryTruckFlags |= DeliveryTruckFlags.UpdateOwnerQuantity;
			}
			Entity entity2 = m_DeliveryTruckSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, ref m_PrefabDeliveryTruckData, ref m_PrefabObjectData, resource, resource2, ref amount, ref returnAmount, m_TransformData[entity], entity, deliveryTruckFlags);
			if (entity2 != Entity.Null)
			{
				if (amount > 0)
				{
					EconomyUtils.AddResources(resource, -amount, resources);
				}
				availableDeliveryTrucks--;
				freeSpace += amount - returnAmount;
				m_CommandBuffer.SetComponent(jobIndex, entity2, new Target(component.m_Destination));
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(entity));
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity2, completed: true));
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> sourceElements = m_PathElements[request];
					if (sourceElements.Length != 0)
					{
						DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity2);
						PathUtils.CopyPath(sourceElements, default(PathOwner), 0, targetElements);
						m_CommandBuffer.SetComponent(jobIndex, entity2, new PathOwner(PathFlags.Updated));
						m_CommandBuffer.SetComponent(jobIndex, entity2, component);
					}
				}
				return true;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PostFacilityActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.PostVan> m_PostVanData;

		public NativeQueue<PostFacilityAction> m_ActionQueue;

		public void Execute()
		{
			PostFacilityAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				if (m_PostVanData.TryGetComponent(item.m_Entity, out var componentData))
				{
					if (item.m_Disabled)
					{
						componentData.m_State |= PostVanFlags.Disabled;
					}
					else
					{
						componentData.m_State &= ~PostVanFlags.Disabled;
					}
					m_PostVanData[item.m_Entity] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.PostFacility> __Game_Buildings_PostFacility_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Routes.MailBox> __Game_Routes_MailBox_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle;

		public BufferTypeHandle<GuestVehicle> __Game_Vehicles_GuestVehicle_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostVanRequest> __Game_Simulation_PostVanRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailTransferRequest> __Game_Simulation_MailTransferRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PostVan> __Game_Vehicles_PostVan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ReturnLoad> __Game_Vehicles_ReturnLoad_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostFacilityData> __Game_Prefabs_PostFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MailBoxData> __Game_Prefabs_MailBoxData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostVanData> __Game_Prefabs_PostVanData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.PostVan> __Game_Vehicles_PostVan_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_PostFacility_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.PostFacility>();
			__Game_Routes_MailBox_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Routes.MailBox>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>();
			__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>();
			__Game_Vehicles_GuestVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<GuestVehicle>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Simulation_PostVanRequest_RO_ComponentLookup = state.GetComponentLookup<PostVanRequest>(isReadOnly: true);
			__Game_Simulation_MailTransferRequest_RO_ComponentLookup = state.GetComponentLookup<MailTransferRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Vehicles_PostVan_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PostVan>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_ReturnLoad_RO_ComponentLookup = state.GetComponentLookup<ReturnLoad>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PostFacilityData_RO_ComponentLookup = state.GetComponentLookup<PostFacilityData>(isReadOnly: true);
			__Game_Prefabs_MailBoxData_RO_ComponentLookup = state.GetComponentLookup<MailBoxData>(isReadOnly: true);
			__Game_Prefabs_PostVanData_RO_ComponentLookup = state.GetComponentLookup<PostVanData>(isReadOnly: true);
			__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruckData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_PostVan_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PostVan>();
		}
	}

	public static readonly int kUpdatesPerDay = 1024;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_PostVanPrefabQuery;

	private EntityQuery m_PostConfigurationQuery;

	private EntityArchetype m_MailTransferRequestArchetype;

	private EntityArchetype m_PostVanRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private PostVanSelectData m_PostVanSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 176;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PostVanSelectData = new PostVanSelectData(this);
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.PostFacility>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_PostVanPrefabQuery = GetEntityQuery(PostVanSelectData.GetEntityQueryDesc());
		m_PostConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PostConfigurationData>());
		m_MailTransferRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<MailTransferRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_PostVanRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PostVanRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_ParkedToMovingRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<Stopped>());
		m_ParkedToMovingCarAddTypes = new ComponentTypeSet(new ComponentType[14]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		RequireForUpdate(m_BuildingQuery);
		RequireForUpdate(m_PostConfigurationQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_PostVanSelectData.PreUpdate(this, m_CityConfigurationSystem, m_PostVanPrefabQuery, Allocator.TempJob, out var jobHandle);
		NativeQueue<PostFacilityAction> actionQueue = new NativeQueue<PostFacilityAction>(Allocator.TempJob);
		PostFacilityTickJob jobData = new PostFacilityTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PostFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PostFacility_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MailBoxType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_MailBox_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_GuestVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PostVanRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PostVanRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MailTransferRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MailTransferRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PostVanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PostVan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ReturnLoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ReturnLoad_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPostFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PostFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMailBoxData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MailBoxData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPostVanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PostVanData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDeliveryTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_PostVanSelectData = m_PostVanSelectData,
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_PostConfigurationData = m_PostConfigurationQuery.GetSingleton<PostConfigurationData>(),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_PostVanRequestArchetype = m_PostVanRequestArchetype,
			m_MailTransferRequestArchetype = m_MailTransferRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		PostFacilityActionJob jobData2 = new PostFacilityActionJob
		{
			m_PostVanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PostVan_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_PostVanSelectData.PostUpdate(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle3;
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
	public PostFacilityAISystem()
	{
	}
}
