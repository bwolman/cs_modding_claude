using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Vehicles;

[CompilerGenerated]
public class FixParkingLocationSystem : GameSystemBase
{
	[BurstCompile]
	private struct CollectParkedCarsJob : IJobChunk
	{
		private struct AddVehiclesIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_Lane;

			public Bounds3 m_Bounds;

			public NativeQueue<Entity>.ParallelWriter m_VehicleQueue;

			public ComponentLookup<ParkedCar> m_ParkedCarData;

			public ComponentLookup<Controller> m_ControllerData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
				{
					if (m_ControllerData.TryGetComponent(entity, out var componentData))
					{
						entity = componentData.m_Controller;
					}
					if (m_ParkedCarData.TryGetComponent(entity, out var componentData2) && componentData2.m_Lane == m_Lane)
					{
						m_VehicleQueue.Enqueue(entity);
					}
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<FixParkingLocation> m_FixParkingLocationType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> m_SpawnLocationType;

		[ReadOnly]
		public ComponentTypeHandle<MovedLocation> m_MovedLocationType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public BufferTypeHandle<LaneObject> m_LaneObjectType;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocations;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

		public NativeQueue<Entity>.ParallelWriter m_VehicleQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_FixParkingLocationType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					m_VehicleQueue.Enqueue(nativeArray[i]);
				}
				return;
			}
			BufferAccessor<LaneObject> bufferAccessor = chunk.GetBufferAccessor(ref m_LaneObjectType);
			if (bufferAccessor.Length != 0)
			{
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					DynamicBuffer<LaneObject> dynamicBuffer = bufferAccessor[j];
					int num = 0;
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						LaneObject value = dynamicBuffer[k];
						if (m_ParkedCarData.HasComponent(value.m_LaneObject))
						{
							Entity value2 = value.m_LaneObject;
							if (m_ControllerData.TryGetComponent(value.m_LaneObject, out var componentData))
							{
								value2 = componentData.m_Controller;
							}
							m_VehicleQueue.Enqueue(value2);
						}
						else
						{
							dynamicBuffer[num++] = value;
						}
					}
					if (num != 0)
					{
						if (num < dynamicBuffer.Length)
						{
							dynamicBuffer.RemoveRange(num, dynamicBuffer.Length - num);
						}
					}
					else
					{
						dynamicBuffer.Clear();
					}
				}
				return;
			}
			NativeArray<Game.Net.ConnectionLane> nativeArray2 = chunk.GetNativeArray(ref m_ConnectionLaneType);
			if (nativeArray2.Length != 0)
			{
				NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Curve> nativeArray4 = chunk.GetNativeArray(ref m_CurveType);
				NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
				for (int l = 0; l < nativeArray2.Length; l++)
				{
					Game.Net.ConnectionLane connectionLane = nativeArray2[l];
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
					{
						Entity entity = nativeArray3[l];
						Curve curve = nativeArray4[l];
						Owner owner = nativeArray5[l];
						AddVehicles(entity, owner, curve, connectionLane);
					}
				}
				return;
			}
			NativeArray<Transform> nativeArray6 = chunk.GetNativeArray(ref m_TransformType);
			if (nativeArray6.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Objects.SpawnLocation> nativeArray8 = chunk.GetNativeArray(ref m_SpawnLocationType);
			NativeArray<MovedLocation> nativeArray9 = chunk.GetNativeArray(ref m_MovedLocationType);
			NativeArray<PrefabRef> nativeArray10 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int m = 0; m < nativeArray6.Length; m++)
			{
				PrefabRef prefabRef = nativeArray10[m];
				if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && (((componentData2.m_RoadTypes & RoadTypes.Helicopter) != RoadTypes.None && componentData2.m_ConnectionType == RouteConnectionType.Air) || componentData2.m_ConnectionType == RouteConnectionType.Track))
				{
					Entity entity2 = nativeArray7[m];
					Transform transform = nativeArray6[m];
					Game.Objects.SpawnLocation spawnLocation = nativeArray8[m];
					if (CollectionUtils.TryGet(nativeArray9, m, out var value3))
					{
						transform.m_Position = value3.m_OldPosition;
					}
					AddVehicles(entity2, transform, spawnLocation, componentData2);
				}
			}
		}

		private void AddVehicles(Entity entity, Owner owner, Curve curve, Game.Net.ConnectionLane connectionLane)
		{
			AddVehiclesIterator iterator = new AddVehiclesIterator
			{
				m_Lane = entity,
				m_Bounds = VehicleUtils.GetConnectionParkingBounds(connectionLane, curve.m_Bezier),
				m_VehicleQueue = m_VehicleQueue,
				m_ParkedCarData = m_ParkedCarData,
				m_ControllerData = m_ControllerData
			};
			Owner owner2 = owner;
			while (m_OwnerData.HasComponent(owner2.m_Owner))
			{
				owner2 = m_OwnerData[owner2.m_Owner];
			}
			if (m_BuildingData.HasComponent(owner2.m_Owner))
			{
				PrefabRef prefabRef = m_PrefabRefData[owner2.m_Owner];
				if (m_ActivityLocations.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
				{
					Transform transform = m_TransformData[owner2.m_Owner];
					ActivityMask activityMask = new ActivityMask(ActivityType.GarageSpot);
					for (int i = 0; i < bufferData.Length; i++)
					{
						ActivityLocationElement activityLocationElement = bufferData[i];
						if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask.m_Mask) != 0)
						{
							float3 @float = ObjectUtils.LocalToWorld(transform, activityLocationElement.m_Position);
							iterator.m_Bounds.min = math.min(iterator.m_Bounds.min, @float - 1f);
							iterator.m_Bounds.max = math.max(iterator.m_Bounds.max, @float + 1f);
						}
					}
				}
			}
			m_MovingObjectSearchTree.Iterate(ref iterator);
		}

		private void AddVehicles(Entity entity, Transform transform, Game.Objects.SpawnLocation spawnLocation, SpawnLocationData spawnLocationData)
		{
			switch (spawnLocationData.m_ConnectionType)
			{
			case RouteConnectionType.Air:
			{
				AddVehiclesIterator iterator = new AddVehiclesIterator
				{
					m_Lane = entity,
					m_Bounds = new Bounds3(transform.m_Position - 1f, transform.m_Position + 1f),
					m_VehicleQueue = m_VehicleQueue,
					m_ParkedCarData = m_ParkedCarData,
					m_ControllerData = m_ControllerData
				};
				m_MovingObjectSearchTree.Iterate(ref iterator);
				break;
			}
			case RouteConnectionType.Track:
			{
				if (!m_LaneObjects.TryGetBuffer(spawnLocation.m_ConnectedLane1, out var bufferData))
				{
					break;
				}
				for (int i = 0; i < bufferData.Length; i++)
				{
					LaneObject laneObject = bufferData[i];
					if (m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData))
					{
						laneObject.m_LaneObject = componentData.m_Controller;
					}
					if (m_ParkedTrainData.TryGetComponent(laneObject.m_LaneObject, out var componentData2) && componentData2.m_ParkingLocation == entity)
					{
						m_VehicleQueue.Enqueue(laneObject.m_LaneObject);
					}
				}
				break;
			}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixParkingLocationJob : IJob
	{
		private struct SpotData : IComparable<SpotData>
		{
			public float3 m_Position;

			public float m_Order;

			public int m_Index;

			public bool m_Occupied;

			public int CompareTo(SpotData other)
			{
				return math.select(0, math.select(-1, 1, m_Order > other.m_Order), m_Order != other.m_Order);
			}
		}

		private struct OccupySpotsIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_Lane;

			public Entity m_Ignore;

			public Bounds3 m_Bounds;

			public int m_Order;

			public NativeList<SpotData> m_Spots;

			public ComponentLookup<ParkedCar> m_ParkedCarData;

			public ComponentLookup<Transform> m_TransformData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_ParkedCarData.TryGetComponent(entity, out var componentData) || !(entity != m_Ignore) || !(componentData.m_Lane == m_Lane))
				{
					return;
				}
				Transform transform = m_TransformData[entity];
				float num = transform.m_Position[m_Order] - 1f;
				float num2 = transform.m_Position[m_Order] + 1f;
				int num3 = 0;
				int num4 = m_Spots.Length;
				while (num4 > num3)
				{
					int num5 = num3 + num4 >> 1;
					if (m_Spots[num5].m_Order < num)
					{
						num3 = num5 + 1;
					}
					else
					{
						num4 = num5;
					}
				}
				num4 = m_Spots.Length;
				while (num3 < num4)
				{
					ref SpotData reference = ref m_Spots.ElementAt(num3++);
					if (!(reference.m_Order > num2))
					{
						if (math.distancesq(transform.m_Position, reference.m_Position) < 1f)
						{
							reference.m_Occupied = true;
						}
						continue;
					}
					break;
				}
			}
		}

		private struct SpawnLocationIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_Lane;

			public Entity m_Ignore;

			public Bounds3 m_Bounds;

			public ComponentLookup<ParkedCar> m_ParkedCarData;

			public ComponentLookup<Controller> m_ControllerData;

			public bool m_Occupied;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && !(entity == m_Ignore) && (!m_ControllerData.TryGetComponent(entity, out var componentData) || !(componentData.m_Controller == m_Ignore)) && m_ParkedCarData.TryGetComponent(entity, out var componentData2) && componentData2.m_Lane == m_Lane)
				{
					m_Occupied = true;
				}
			}
		}

		private struct LaneIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_VehicleEntity;

			public Bounds3 m_Bounds;

			public float3 m_Position;

			public float2 m_ParkingSize;

			public float m_MaxDistance;

			public float m_ParkingOffset;

			public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

			public NativeHashMap<Entity, bool> m_IsUnspawnedMap;

			public ComponentLookup<ParkedCar> m_ParkedCarData;

			public ComponentLookup<ParkedTrain> m_ParkedTrainData;

			public ComponentLookup<Controller> m_ControllerData;

			public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Unspawned> m_UnspawnedData;

			public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

			public BufferLookup<LaneObject> m_LaneObjects;

			public BufferLookup<LaneOverlap> m_LaneOverlaps;

			public Entity m_SelectedLane;

			public float m_SelectedCurvePos;

			public bool m_KeepUnspawned;

			public bool m_SpecialVehicle;

			public TrackTypes m_TrackType;

			public RoadTypes m_RoadType;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
				{
					return;
				}
				if (m_ParkingLaneData.HasComponent(entity))
				{
					Curve curve = m_CurveData[entity];
					if (MathUtils.Distance(curve.m_Bezier, m_Position, out var t) < m_MaxDistance && (m_KeepUnspawned || TryFindParkingSpace(entity, curve, ignoreDisabled: true, ref t)))
					{
						float3 y = MathUtils.Position(curve.m_Bezier, t);
						float num = math.distance(m_Position, y);
						if (num < m_MaxDistance)
						{
							m_MaxDistance = num;
							m_SelectedLane = entity;
							m_SelectedCurvePos = t;
							m_Bounds = new Bounds3(m_Position - m_MaxDistance, m_Position + m_MaxDistance);
						}
					}
				}
				else
				{
					if (!m_SpawnLocationData.HasComponent(entity))
					{
						return;
					}
					Transform transform = m_TransformData[entity];
					float num2 = math.distance(transform.m_Position, m_Position);
					if (num2 < m_MaxDistance)
					{
						PrefabRef prefabRef = m_PrefabRefData[entity];
						if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && ((m_TrackType == TrackTypes.None && (componentData.m_RoadTypes & RoadTypes.Helicopter) != RoadTypes.None && componentData.m_ConnectionType == RouteConnectionType.Air) || (componentData.m_TrackTypes & m_TrackType) != TrackTypes.None) && TryFindParkingSpace(entity, m_VehicleEntity, transform))
						{
							m_MaxDistance = num2;
							m_SelectedLane = entity;
							m_SelectedCurvePos = 0f;
							m_Bounds = new Bounds3(m_Position - m_MaxDistance, m_Position + m_MaxDistance);
						}
					}
				}
			}

			public bool TryFindParkingSpace(Entity lane, Entity vehicle, Transform transform)
			{
				PrefabRef prefabRef = m_PrefabRefData[lane];
				switch (m_PrefabSpawnLocationData[prefabRef.m_Prefab].m_ConnectionType)
				{
				case RouteConnectionType.Air:
				{
					SpawnLocationIterator iterator = new SpawnLocationIterator
					{
						m_Lane = lane,
						m_Bounds = new Bounds3(transform.m_Position - 1f, transform.m_Position + 1f),
						m_Ignore = vehicle,
						m_ParkedCarData = m_ParkedCarData,
						m_ControllerData = m_ControllerData
					};
					m_MovingObjectSearchTree.Iterate(ref iterator);
					return !iterator.m_Occupied;
				}
				case RouteConnectionType.Track:
				{
					Game.Objects.SpawnLocation spawnLocation = m_SpawnLocationData[lane];
					if (m_LaneObjects.TryGetBuffer(spawnLocation.m_ConnectedLane1, out var bufferData))
					{
						for (int i = 0; i < bufferData.Length; i++)
						{
							LaneObject laneObject = bufferData[i];
							if (!(laneObject.m_LaneObject == vehicle) && (!m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData) || !(componentData.m_Controller == vehicle)) && m_ParkedTrainData.TryGetComponent(laneObject.m_LaneObject, out var componentData2) && componentData2.m_ParkingLocation == lane)
							{
								return false;
							}
						}
					}
					return true;
				}
				default:
					return false;
				}
			}

			public bool TryFindParkingSpace(Entity lane, Curve curve, bool ignoreDisabled, ref float curvePos)
			{
				Game.Net.ParkingLane parkingLane = m_ParkingLaneData[lane];
				if ((parkingLane.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
				{
					return false;
				}
				if (ignoreDisabled && (parkingLane.m_Flags & ParkingLaneFlags.ParkingDisabled) != 0)
				{
					return false;
				}
				if (m_SpecialVehicle != ((parkingLane.m_Flags & ParkingLaneFlags.SpecialVehicles) != 0))
				{
					return false;
				}
				PrefabRef prefabRef = m_PrefabRefData[lane];
				DynamicBuffer<LaneObject> dynamicBuffer = m_LaneObjects[lane];
				DynamicBuffer<LaneOverlap> dynamicBuffer2 = m_LaneOverlaps[lane];
				ParkingLaneData parkingLaneData = m_PrefabParkingLaneData[prefabRef.m_Prefab];
				if (math.any(m_ParkingSize > VehicleUtils.GetParkingSize(parkingLaneData)))
				{
					return false;
				}
				if ((m_RoadType & parkingLaneData.m_RoadTypes) == 0)
				{
					return false;
				}
				if (parkingLaneData.m_SlotInterval != 0f)
				{
					int parkingSlotCount = NetUtils.GetParkingSlotCount(curve, parkingLane, parkingLaneData);
					float parkingSlotInterval = NetUtils.GetParkingSlotInterval(curve, parkingLane, parkingLaneData, parkingSlotCount);
					float3 x = curve.m_Bezier.a;
					float2 @float = 0f;
					float num = 0f;
					float num2 = math.max((parkingLane.m_Flags & (ParkingLaneFlags.StartingLane | ParkingLaneFlags.EndingLane)) switch
					{
						ParkingLaneFlags.StartingLane => curve.m_Length - (float)parkingSlotCount * parkingSlotInterval, 
						ParkingLaneFlags.EndingLane => 0f, 
						_ => (curve.m_Length - (float)parkingSlotCount * parkingSlotInterval) * 0.5f, 
					}, 0f);
					int i = -1;
					float num3 = 1f;
					float num4 = curvePos;
					float num5 = 2f;
					int num6 = 0;
					while (num6 < dynamicBuffer.Length)
					{
						LaneObject laneObject = dynamicBuffer[num6++];
						if (m_ParkedCarData.HasComponent(laneObject.m_LaneObject) && !IsUnspawned(laneObject.m_LaneObject))
						{
							num5 = laneObject.m_CurvePosition.x;
							break;
						}
					}
					float2 float2 = 2f;
					int num7 = 0;
					if (num7 < dynamicBuffer2.Length)
					{
						LaneOverlap laneOverlap = dynamicBuffer2[num7++];
						float2 = new float2((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd) * 0.003921569f;
					}
					for (int j = 1; j <= 16; j++)
					{
						float num8 = (float)j * 0.0625f;
						float3 float3 = MathUtils.Position(curve.m_Bezier, num8);
						for (num += math.distance(x, float3); num >= num2 || (j == 16 && i < parkingSlotCount); i++)
						{
							@float.y = math.select(num8, math.lerp(@float.x, num8, num2 / num), num2 < num);
							bool flag = false;
							if (num5 <= @float.y)
							{
								num5 = 2f;
								flag = true;
								while (num6 < dynamicBuffer.Length)
								{
									LaneObject laneObject2 = dynamicBuffer[num6++];
									if (m_ParkedCarData.HasComponent(laneObject2.m_LaneObject) && !IsUnspawned(laneObject2.m_LaneObject) && laneObject2.m_CurvePosition.x > @float.y)
									{
										num5 = laneObject2.m_CurvePosition.x;
										break;
									}
								}
							}
							if (float2.x < @float.y)
							{
								flag = true;
								if (float2.y <= @float.y)
								{
									float2 = 2f;
									while (num7 < dynamicBuffer2.Length)
									{
										LaneOverlap laneOverlap2 = dynamicBuffer2[num7++];
										float2 float4 = new float2((int)laneOverlap2.m_ThisStart, (int)laneOverlap2.m_ThisEnd) * 0.003921569f;
										if (float4.y > @float.y)
										{
											float2 = float4;
											break;
										}
									}
								}
							}
							if (!flag && i >= 0 && i < parkingSlotCount)
							{
								float num9 = math.max(@float.x - curvePos, curvePos - @float.y);
								if (num9 < num3)
								{
									num4 = math.lerp(@float.x, @float.y, 0.5f);
									num3 = num9;
								}
							}
							num -= num2;
							@float.x = @float.y;
							num2 = parkingSlotInterval;
						}
						x = float3;
					}
					if (num4 != curvePos && parkingLaneData.m_SlotAngle <= 0.25f)
					{
						if (m_ParkingOffset > 0f)
						{
							Bounds1 t = new Bounds1(num4, 1f);
							MathUtils.ClampLength(curve.m_Bezier, ref t, m_ParkingOffset);
							num4 = t.max;
						}
						else if (m_ParkingOffset < 0f)
						{
							Bounds1 t2 = new Bounds1(0f, num4);
							MathUtils.ClampLengthInverse(curve.m_Bezier, ref t2, 0f - m_ParkingOffset);
							num4 = t2.min;
						}
					}
					curvePos = num4;
					return num3 != 1f;
				}
				float2 float5 = default(float2);
				float2 float6 = default(float2);
				float num10 = 1f;
				float3 float7 = default(float3);
				float2 float8 = math.select(0f, 0.5f, (parkingLane.m_Flags & ParkingLaneFlags.StartingLane) == 0);
				float3 x2 = curve.m_Bezier.a;
				float num11 = 2f;
				float2 float9 = 0f;
				int num12 = 0;
				while (num12 < dynamicBuffer.Length)
				{
					LaneObject laneObject3 = dynamicBuffer[num12++];
					if (m_ParkedCarData.HasComponent(laneObject3.m_LaneObject) && !IsUnspawned(laneObject3.m_LaneObject))
					{
						num11 = laneObject3.m_CurvePosition.x;
						float9 = VehicleUtils.GetParkingOffsets(laneObject3.m_LaneObject, ref m_PrefabRefData, ref m_PrefabObjectGeometryData) + 1f;
						break;
					}
				}
				float2 yz = 2f;
				int num13 = 0;
				if (num13 < dynamicBuffer2.Length)
				{
					LaneOverlap laneOverlap3 = dynamicBuffer2[num13++];
					yz = new float2((int)laneOverlap3.m_ThisStart, (int)laneOverlap3.m_ThisEnd) * 0.003921569f;
				}
				while (num11 != 2f || yz.x != 2f)
				{
					float x3;
					if (num11 <= yz.x)
					{
						float7.yz = num11;
						float8.y = float9.x;
						x3 = float9.y;
						num11 = 2f;
						while (num12 < dynamicBuffer.Length)
						{
							LaneObject laneObject4 = dynamicBuffer[num12++];
							if (m_ParkedCarData.HasComponent(laneObject4.m_LaneObject) && !IsUnspawned(laneObject4.m_LaneObject))
							{
								num11 = laneObject4.m_CurvePosition.x;
								float9 = VehicleUtils.GetParkingOffsets(laneObject4.m_LaneObject, ref m_PrefabRefData, ref m_PrefabObjectGeometryData) + 1f;
								break;
							}
						}
					}
					else
					{
						float7.yz = yz;
						float8.y = 0.5f;
						x3 = 0.5f;
						yz = 2f;
						while (num13 < dynamicBuffer2.Length)
						{
							LaneOverlap laneOverlap4 = dynamicBuffer2[num13++];
							float2 float10 = new float2((int)laneOverlap4.m_ThisStart, (int)laneOverlap4.m_ThisEnd) * 0.003921569f;
							if (float10.x <= float7.z)
							{
								float7.z = math.max(float7.z, float10.y);
								continue;
							}
							yz = float10;
							break;
						}
					}
					float3 y = MathUtils.Position(curve.m_Bezier, float7.y);
					if (math.distance(x2, y) - math.csum(float8) >= m_ParkingSize.y)
					{
						float num14 = math.max(float7.x - curvePos, curvePos - float7.y);
						if (num14 < num10)
						{
							float5 = float7.xy;
							float6 = float8;
							num10 = num14;
						}
					}
					float7.x = float7.z;
					float8.x = x3;
					x2 = MathUtils.Position(curve.m_Bezier, float7.z);
				}
				float7.y = 1f;
				float8.y = math.select(0f, 0.5f, (parkingLane.m_Flags & ParkingLaneFlags.EndingLane) == 0);
				if (math.distance(x2, curve.m_Bezier.d) - math.csum(float8) >= m_ParkingSize.y)
				{
					float num15 = math.max(float7.x - curvePos, curvePos - float7.y);
					if (num15 < num10)
					{
						float5 = float7.xy;
						float6 = float8;
						num10 = num15;
					}
				}
				if (num10 != 1f)
				{
					float6 += m_ParkingSize.y * 0.5f;
					float6.x += m_ParkingOffset;
					float6.y -= m_ParkingOffset;
					Bounds1 t3 = new Bounds1(float5.x, float5.y);
					Bounds1 t4 = new Bounds1(float5.x, float5.y);
					MathUtils.ClampLength(curve.m_Bezier, ref t3, float6.x);
					MathUtils.ClampLengthInverse(curve.m_Bezier, ref t4, float6.y);
					if (curvePos < t3.max || curvePos > t4.min)
					{
						if (t3.max < t4.min)
						{
							curvePos = math.select(t3.max, t4.min, curvePos > t4.min);
						}
						else
						{
							curvePos = math.lerp(t3.max, t4.min, 0.5f);
						}
					}
					return true;
				}
				return false;
			}

			private bool IsUnspawned(Entity vehicle)
			{
				if (m_IsUnspawnedMap.TryGetValue(vehicle, out var item))
				{
					return item;
				}
				return m_UnspawnedData.HasComponent(vehicle);
			}
		}

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<FixParkingLocation> m_FixParkingLocationData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<Helicopter> m_HelicopterData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocations;

		public ComponentLookup<Transform> m_TransformData;

		public ComponentLookup<ParkedCar> m_ParkedCarData;

		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		public ComponentLookup<PersonalCar> m_PersonalCarData;

		public ComponentLookup<CarKeeper> m_CarKeeperData;

		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		public NativeQueue<Entity> m_VehicleQueue;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (m_VehicleQueue.Count == 0)
			{
				return;
			}
			NativeHashMap<Entity, bool> isUnspawnedMap = new NativeHashMap<Entity, bool>(m_VehicleQueue.Count * 2, Allocator.Temp);
			NativeList<PathElement> laneBuffer = default(NativeList<PathElement>);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			LaneIterator iterator = new LaneIterator
			{
				m_MovingObjectSearchTree = m_MovingObjectSearchTree,
				m_IsUnspawnedMap = isUnspawnedMap,
				m_ParkedCarData = m_ParkedCarData,
				m_ParkedTrainData = m_ParkedTrainData,
				m_ControllerData = m_ControllerData,
				m_ParkingLaneData = m_ParkingLaneData,
				m_CurveData = m_CurveData,
				m_UnspawnedData = m_UnspawnedData,
				m_TransformData = m_TransformData,
				m_SpawnLocationData = m_SpawnLocationData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabParkingLaneData = m_PrefabParkingLaneData,
				m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
				m_PrefabSpawnLocationData = m_PrefabSpawnLocationData,
				m_LaneObjects = m_LaneObjects,
				m_LaneOverlaps = m_LaneOverlaps
			};
			Entity item;
			while (m_VehicleQueue.TryDequeue(out item))
			{
				bool flag = m_UnspawnedData.HasComponent(item);
				if (!isUnspawnedMap.TryAdd(item, flag))
				{
					continue;
				}
				ParkedCar componentData;
				bool flag2 = m_ParkedCarData.TryGetComponent(item, out componentData);
				ParkedTrain componentData2;
				bool flag3 = m_ParkedTrainData.TryGetComponent(item, out componentData2);
				if (!flag2 && !flag3)
				{
					FixParkingLocation fixParkingLocation = m_FixParkingLocationData[item];
					m_CommandBuffer.RemoveComponent<FixParkingLocation>(item);
					if (m_LaneObjects.TryGetBuffer(fixParkingLocation.m_ChangeLane, out var bufferData))
					{
						NetUtils.RemoveLaneObject(bufferData, item);
					}
					continue;
				}
				Transform transform = m_TransformData[item];
				PrefabRef prefabRef = m_PrefabRefData[item];
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				m_LayoutElements.TryGetBuffer(item, out var bufferData2);
				Transform transform2 = transform;
				bool flag4 = false;
				bool flag5 = flag && flag2 && m_LaneObjects.HasBuffer(componentData.m_Lane);
				if (m_FixParkingLocationData.TryGetComponent(item, out var componentData3))
				{
					m_CommandBuffer.RemoveComponent<FixParkingLocation>(item);
					if (m_LaneObjects.TryGetBuffer(componentData3.m_ChangeLane, out var bufferData3))
					{
						NetUtils.RemoveLaneObject(bufferData3, item);
					}
					if (componentData3.m_ResetLocation != item)
					{
						bool setHomeTarget = false;
						if (m_TransformData.TryGetComponent(componentData3.m_ResetLocation, out var componentData4))
						{
							transform = componentData4;
							setHomeTarget = true;
						}
						else
						{
							flag4 = true;
						}
						RemoveCarKeeper(item, setHomeTarget, unsetHomeTarget: false);
					}
					if (flag2 && m_LaneObjects.TryGetBuffer(componentData.m_Lane, out bufferData3))
					{
						NetUtils.RemoveLaneObject(bufferData3, item);
						if (m_ParkingLaneData.HasComponent(componentData.m_Lane) && isUnspawnedMap.TryAdd(componentData.m_Lane, item: false) && !m_UpdatedData.HasComponent(componentData.m_Lane))
						{
							m_CommandBuffer.AddComponent(componentData.m_Lane, default(PathfindUpdated));
						}
					}
					else
					{
						Entity entity = (flag2 ? componentData.m_Lane : componentData2.m_ParkingLocation);
						if (!m_DeletedData.HasComponent(entity) && !flag4)
						{
							if (flag2 && m_ConnectionLaneData.TryGetComponent(componentData.m_Lane, out var componentData5))
							{
								if ((componentData5.m_Flags & ConnectionLaneFlags.Parking) != 0)
								{
									if (FindGarageSpot(ref random, item, componentData.m_Lane, ref transform))
									{
										m_TransformData[item] = transform;
										if (flag)
										{
											m_CommandBuffer.RemoveComponent<Unspawned>(item);
											m_CommandBuffer.AddComponent(item, default(BatchesUpdated));
											isUnspawnedMap[item] = false;
										}
									}
									AddToSearchTree(item, transform, objectGeometryData);
									continue;
								}
							}
							else if (m_SpawnLocationData.HasComponent(entity))
							{
								Transform transform3 = m_TransformData[entity];
								PrefabRef prefabRef2 = m_PrefabRefData[entity];
								if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef2.m_Prefab, out var componentData6) && ((flag2 && (componentData6.m_RoadTypes & RoadTypes.Helicopter) != RoadTypes.None && componentData6.m_ConnectionType == RouteConnectionType.Air) || (flag3 && componentData6.m_TrackTypes != TrackTypes.None && ValidateParkedTrain(bufferData2))) && iterator.TryFindParkingSpace(entity, item, transform3))
								{
									if (flag3)
									{
										UpdateTrainLocation(item, componentData2.m_ParkingLocation, bufferData2, ref laneBuffer);
										continue;
									}
									transform.m_Position = transform3.m_Position;
									m_TransformData[item] = transform;
									AddToSearchTree(item, transform, objectGeometryData);
									continue;
								}
							}
						}
						if (bufferData2.IsCreated && bufferData2.Length != 0)
						{
							for (int i = 0; i < bufferData2.Length; i++)
							{
								m_MovingObjectSearchTree.TryRemove(bufferData2[i].m_Vehicle);
							}
						}
						else
						{
							m_MovingObjectSearchTree.TryRemove(item);
						}
					}
					componentData.m_Lane = Entity.Null;
					componentData2.m_ParkingLocation = Entity.Null;
				}
				iterator.m_VehicleEntity = item;
				iterator.m_Position = transform.m_Position;
				iterator.m_MaxDistance = 100f;
				iterator.m_ParkingSize = VehicleUtils.GetParkingSize(item, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, out iterator.m_ParkingOffset);
				iterator.m_Bounds = new Bounds3(iterator.m_Position - iterator.m_MaxDistance, iterator.m_Position + iterator.m_MaxDistance);
				iterator.m_SelectedLane = Entity.Null;
				iterator.m_KeepUnspawned = flag5;
				iterator.m_SpecialVehicle = !m_PersonalCarData.HasComponent(item);
				iterator.m_TrackType = TrackTypes.None;
				iterator.m_RoadType = RoadTypes.None;
				if (flag3 && m_PrefabTrainData.TryGetComponent(prefabRef.m_Prefab, out var componentData7))
				{
					iterator.m_TrackType = componentData7.m_TrackType;
				}
				if (flag2)
				{
					if (m_HelicopterData.HasComponent(item))
					{
						iterator.m_RoadType = RoadTypes.Helicopter;
					}
					else if (m_BicycleData.HasComponent(item))
					{
						iterator.m_RoadType = RoadTypes.Bicycle;
					}
					else
					{
						iterator.m_RoadType = RoadTypes.Car;
					}
				}
				Game.Net.ParkingLane componentData9;
				if (flag2 && m_ConnectionLaneData.TryGetComponent(componentData.m_Lane, out var componentData8))
				{
					flag4 |= (componentData8.m_Flags & ConnectionLaneFlags.Outside) != 0;
				}
				else if (flag2 && m_ParkingLaneData.TryGetComponent(componentData.m_Lane, out componentData9) && !m_DeletedData.HasComponent(componentData.m_Lane))
				{
					Curve curve = m_CurveData[componentData.m_Lane];
					if (flag5 || iterator.TryFindParkingSpace(componentData.m_Lane, curve, ignoreDisabled: false, ref componentData.m_CurvePosition))
					{
						PrefabRef prefabRef3 = m_PrefabRefData[componentData.m_Lane];
						ParkingLaneData parkingLaneData = m_PrefabParkingLaneData[prefabRef3.m_Prefab];
						Transform ownerTransform = default(Transform);
						if (m_OwnerData.TryGetComponent(componentData.m_Lane, out var componentData10) && m_TransformData.HasComponent(componentData10.m_Owner))
						{
							ownerTransform = m_TransformData[componentData10.m_Owner];
						}
						transform = VehicleUtils.CalculateParkingSpaceTarget(componentData9, parkingLaneData, objectGeometryData, curve, ownerTransform, componentData.m_CurvePosition);
						NetUtils.AddLaneObject(m_LaneObjects[componentData.m_Lane], item, componentData.m_CurvePosition);
						UpdateParkedCar(item, transform2, transform, componentData);
						continue;
					}
				}
				if (!flag4)
				{
					if (flag2)
					{
						m_LaneSearchTree.Iterate(ref iterator);
					}
					if ((flag2 && iterator.m_RoadType == RoadTypes.Helicopter) || flag3)
					{
						m_StaticObjectSearchTree.Iterate(ref iterator);
					}
				}
				if (iterator.m_SelectedLane != Entity.Null)
				{
					if (m_ParkingLaneData.TryGetComponent(iterator.m_SelectedLane, out var componentData11))
					{
						Curve curve2 = m_CurveData[iterator.m_SelectedLane];
						PrefabRef prefabRef4 = m_PrefabRefData[iterator.m_SelectedLane];
						ParkingLaneData parkingLaneData2 = m_PrefabParkingLaneData[prefabRef4.m_Prefab];
						Transform ownerTransform2 = default(Transform);
						if (m_OwnerData.TryGetComponent(iterator.m_SelectedLane, out var componentData12) && m_TransformData.HasComponent(componentData12.m_Owner))
						{
							ownerTransform2 = m_TransformData[componentData12.m_Owner];
						}
						if (flag5)
						{
							iterator.m_SelectedCurvePos = math.clamp(iterator.m_SelectedCurvePos, 0.05f, 0.95f);
							iterator.m_SelectedCurvePos = random.NextFloat(math.max(0.05f, iterator.m_SelectedCurvePos - 0.2f), math.min(0.95f, iterator.m_SelectedCurvePos + 0.2f));
						}
						transform = VehicleUtils.CalculateParkingSpaceTarget(componentData11, parkingLaneData2, objectGeometryData, curve2, ownerTransform2, iterator.m_SelectedCurvePos);
						NetUtils.AddLaneObject(m_LaneObjects[iterator.m_SelectedLane], item, iterator.m_SelectedCurvePos);
						m_MovingObjectSearchTree.TryRemove(item);
					}
					else
					{
						transform = m_TransformData[iterator.m_SelectedLane];
						if (!flag3)
						{
							AddToSearchTree(item, transform, objectGeometryData);
						}
					}
					componentData.m_Lane = iterator.m_SelectedLane;
					componentData.m_CurvePosition = iterator.m_SelectedCurvePos;
					componentData2.m_ParkingLocation = iterator.m_SelectedLane;
					if (isUnspawnedMap.TryAdd(iterator.m_SelectedLane, item: false) && !m_UpdatedData.HasComponent(iterator.m_SelectedLane))
					{
						m_CommandBuffer.AddComponent(iterator.m_SelectedLane, default(PathfindUpdated));
					}
					if (flag && !flag5)
					{
						if (bufferData2.IsCreated && bufferData2.Length != 0)
						{
							for (int j = 0; j < bufferData2.Length; j++)
							{
								Entity vehicle = bufferData2[j].m_Vehicle;
								m_CommandBuffer.RemoveComponent<Unspawned>(vehicle);
								m_CommandBuffer.AddComponent(vehicle, default(BatchesUpdated));
								isUnspawnedMap[item] = false;
							}
						}
						else
						{
							m_CommandBuffer.RemoveComponent<Unspawned>(item);
							m_CommandBuffer.AddComponent(item, default(BatchesUpdated));
							isUnspawnedMap[item] = false;
						}
					}
					if (flag2)
					{
						UpdateParkedCar(item, transform2, transform, componentData);
					}
					else
					{
						if (!flag3)
						{
							continue;
						}
						UpdateTrainLocation(item, componentData2.m_ParkingLocation, bufferData2, ref laneBuffer);
						bool flag6 = m_TransformData[item].Equals(transform2);
						for (int k = 0; k < bufferData2.Length; k++)
						{
							Entity vehicle2 = bufferData2[k].m_Vehicle;
							ParkedTrain value = m_ParkedTrainData[vehicle2];
							value.m_ParkingLocation = componentData2.m_ParkingLocation;
							m_ParkedTrainData[vehicle2] = value;
							if (flag6)
							{
								m_CommandBuffer.AddComponent(vehicle2, default(Updated));
							}
						}
					}
					continue;
				}
				componentData.m_Lane = Entity.Null;
				componentData2.m_ParkingLocation = Entity.Null;
				RemoveCarKeeper(item, setHomeTarget: false, unsetHomeTarget: true);
				if (bufferData2.IsCreated && bufferData2.Length != 0)
				{
					for (int l = 0; l < bufferData2.Length; l++)
					{
						Entity vehicle3 = bufferData2[l].m_Vehicle;
						PrefabRef prefabRef5 = m_PrefabRefData[vehicle3];
						ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef5.m_Prefab];
						if (flag2)
						{
							AddToSearchTree(vehicle3, transform, objectGeometryData2);
						}
						if (!flag)
						{
							isUnspawnedMap[item] = true;
						}
						m_CommandBuffer.AddComponent(vehicle3, default(Unspawned));
						m_CommandBuffer.AddComponent(vehicle3, default(Updated));
					}
				}
				else
				{
					if (flag2)
					{
						AddToSearchTree(item, transform, objectGeometryData);
					}
					if (!flag)
					{
						isUnspawnedMap[item] = true;
					}
					m_CommandBuffer.AddComponent(item, default(Unspawned));
					m_CommandBuffer.AddComponent(item, default(Updated));
				}
				if (flag2)
				{
					UpdateParkedCar(item, transform2, transform, componentData);
				}
				else if (flag3)
				{
					RemoveTrainFromLanes(bufferData2);
					for (int m = 0; m < bufferData2.Length; m++)
					{
						Entity vehicle4 = bufferData2[m].m_Vehicle;
						ParkedTrain parkedTrain = m_ParkedTrainData[vehicle4];
						parkedTrain.m_ParkingLocation = componentData2.m_ParkingLocation;
						parkedTrain.m_FrontLane = Entity.Null;
						parkedTrain.m_RearLane = Entity.Null;
						m_ParkedTrainData[vehicle4] = parkedTrain;
						UpdateParkedTrain(vehicle4, m_TransformData[vehicle4], transform, parkedTrain);
					}
				}
			}
			isUnspawnedMap.Dispose();
			if (laneBuffer.IsCreated)
			{
				laneBuffer.Dispose();
			}
		}

		private void UpdateTrainLocation(Entity entity, Entity parkingLocation, DynamicBuffer<LayoutElement> layout, ref NativeList<PathElement> laneBuffer)
		{
			RemoveTrainFromLanes(layout);
			PathOwner pathOwner = default(PathOwner);
			ComponentLookup<TrainCurrentLane> currentLaneData = default(ComponentLookup<TrainCurrentLane>);
			ComponentLookup<TrainNavigation> navigationData = default(ComponentLookup<TrainNavigation>);
			if (laneBuffer.IsCreated)
			{
				laneBuffer.Clear();
			}
			else
			{
				laneBuffer = new NativeList<PathElement>(10, Allocator.Temp);
			}
			float length = VehicleUtils.CalculateLength(entity, layout, ref m_PrefabRefData, ref m_PrefabTrainData);
			PathUtils.InitializeSpawnPath(default(DynamicBuffer<PathElement>), laneBuffer, parkingLocation, ref pathOwner, length, ref m_CurveData, ref m_LaneData, ref m_EdgeLaneData, ref m_OwnerData, ref m_EdgeData, ref m_SpawnLocationData, ref m_ConnectedEdges, ref m_SubLanes);
			VehicleUtils.UpdateCarriageLocations(layout, laneBuffer, ref m_TrainData, ref m_ParkedTrainData, ref currentLaneData, ref navigationData, ref m_TransformData, ref m_CurveData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabTrainData);
			AddTrainToLanes(layout);
		}

		private void RemoveTrainFromLanes(DynamicBuffer<LayoutElement> layout)
		{
			for (int i = 0; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				ParkedTrain parkedTrain = m_ParkedTrainData[vehicle];
				if (m_LaneObjects.TryGetBuffer(parkedTrain.m_FrontLane, out var bufferData))
				{
					NetUtils.RemoveLaneObject(bufferData, vehicle);
				}
				if (parkedTrain.m_RearLane != parkedTrain.m_FrontLane && m_LaneObjects.TryGetBuffer(parkedTrain.m_RearLane, out bufferData))
				{
					NetUtils.RemoveLaneObject(bufferData, vehicle);
				}
			}
		}

		private void AddTrainToLanes(DynamicBuffer<LayoutElement> layout)
		{
			for (int i = 0; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				ParkedTrain parkedTrain = m_ParkedTrainData[vehicle];
				TrainNavigationHelpers.GetCurvePositions(ref parkedTrain, out var pos, out var pos2);
				if (m_LaneObjects.TryGetBuffer(parkedTrain.m_FrontLane, out var bufferData))
				{
					NetUtils.AddLaneObject(bufferData, vehicle, pos);
				}
				if (parkedTrain.m_RearLane != parkedTrain.m_FrontLane && m_LaneObjects.TryGetBuffer(parkedTrain.m_RearLane, out bufferData))
				{
					NetUtils.AddLaneObject(bufferData, vehicle, pos2);
				}
			}
		}

		private bool ValidateParkedTrain(DynamicBuffer<LayoutElement> layout)
		{
			for (int i = 0; i < layout.Length; i++)
			{
				Entity vehicle = layout[i].m_Vehicle;
				if (!m_ParkedTrainData.TryGetComponent(vehicle, out var componentData))
				{
					return false;
				}
				if (!m_EntityLookup.Exists(componentData.m_FrontLane))
				{
					return false;
				}
				if (!m_EntityLookup.Exists(componentData.m_RearLane))
				{
					return false;
				}
			}
			return true;
		}

		private void AddToSearchTree(Entity entity, Transform transform, ObjectGeometryData objectGeometryData)
		{
			Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, objectGeometryData);
			m_MovingObjectSearchTree.AddOrUpdate(entity, new QuadTreeBoundsXZ(bounds));
		}

		private void UpdateParkedCar(Entity entity, Transform oldTransform, Transform transform, ParkedCar parkedCar)
		{
			if (!transform.Equals(oldTransform))
			{
				m_TransformData[entity] = transform;
				m_CommandBuffer.AddComponent(entity, default(Updated));
				UpdateSubObjects(entity, transform);
			}
			m_ParkedCarData[entity] = parkedCar;
		}

		private void UpdateSubObjects(Entity entity, Transform transform)
		{
			if (!m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Objects.SubObject subObject = bufferData[i];
				if (m_RelativeData.TryGetComponent(subObject.m_SubObject, out var componentData))
				{
					Transform transform2 = ObjectUtils.LocalToWorld(transform, new Transform(componentData.m_Position, componentData.m_Rotation));
					m_TransformData[subObject.m_SubObject] = transform2;
					m_CommandBuffer.AddComponent(subObject.m_SubObject, default(Updated));
					UpdateSubObjects(subObject.m_SubObject, transform2);
				}
			}
		}

		private void UpdateParkedTrain(Entity entity, Transform oldTransform, Transform transform, ParkedTrain parkedTrain)
		{
			if (!transform.Equals(oldTransform))
			{
				m_TransformData[entity] = transform;
				m_CommandBuffer.AddComponent(entity, default(Updated));
				UpdateSubObjects(entity, transform);
			}
			m_ParkedTrainData[entity] = parkedTrain;
		}

		private void RemoveCarKeeper(Entity entity, bool setHomeTarget, bool unsetHomeTarget)
		{
			if (m_PersonalCarData.TryGetComponent(entity, out var componentData))
			{
				if (m_CarKeeperData.TryGetEnabledComponent(componentData.m_Keeper, out var component) && component.m_Car == entity)
				{
					component.m_Car = Entity.Null;
					m_CarKeeperData[componentData.m_Keeper] = component;
				}
				componentData.m_Keeper = Entity.Null;
				if (setHomeTarget)
				{
					componentData.m_State |= PersonalCarFlags.HomeTarget;
				}
				if (unsetHomeTarget)
				{
					componentData.m_State &= ~PersonalCarFlags.HomeTarget;
				}
				m_PersonalCarData[entity] = componentData;
			}
		}

		private bool FindGarageSpot(ref Unity.Mathematics.Random random, Entity vehicle, Entity lane, ref Transform transform)
		{
			Entity entity = lane;
			while (m_OwnerData.HasComponent(entity))
			{
				entity = m_OwnerData[entity].m_Owner;
			}
			if (m_BuildingData.HasComponent(entity))
			{
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_ActivityLocations.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
				{
					Transform transform2 = m_TransformData[entity];
					ActivityMask activityMask = new ActivityMask(ActivityType.GarageSpot);
					OccupySpotsIterator iterator = new OccupySpotsIterator
					{
						m_Lane = lane,
						m_Ignore = vehicle,
						m_Bounds = new Bounds3(float.MaxValue, float.MinValue),
						m_Spots = new NativeList<SpotData>(bufferData.Length, Allocator.Temp),
						m_ParkedCarData = m_ParkedCarData,
						m_TransformData = m_TransformData
					};
					int num = -1;
					for (int i = 0; i < bufferData.Length; i++)
					{
						ActivityLocationElement activityLocationElement = bufferData[i];
						if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask.m_Mask) != 0)
						{
							float3 @float = ObjectUtils.LocalToWorld(transform2, activityLocationElement.m_Position);
							iterator.m_Bounds.min = math.min(iterator.m_Bounds.min, @float - 1f);
							iterator.m_Bounds.max = math.max(iterator.m_Bounds.max, @float + 1f);
							ref NativeList<SpotData> reference = ref iterator.m_Spots;
							SpotData value = new SpotData
							{
								m_Position = @float,
								m_Index = i
							};
							reference.Add(in value);
							if (math.distancesq(transform.m_Position, @float) < 1f)
							{
								num = i;
							}
						}
					}
					bool result = false;
					if (iterator.m_Spots.Length > 0)
					{
						if (iterator.m_Spots.Length >= 2)
						{
							float3 float2 = MathUtils.Size(iterator.m_Bounds);
							iterator.m_Order = math.select(0, 1, float2.y > float2.x);
							iterator.m_Order = math.select(iterator.m_Order, 2, math.all(float2.z > float2.xy));
						}
						for (int j = 0; j < iterator.m_Spots.Length; j++)
						{
							ref SpotData reference2 = ref iterator.m_Spots.ElementAt(j);
							reference2.m_Order = reference2.m_Position[iterator.m_Order];
						}
						if (iterator.m_Spots.Length >= 2)
						{
							iterator.m_Spots.Sort();
						}
						m_MovingObjectSearchTree.Iterate(ref iterator);
						int num2 = 0;
						bool flag = false;
						for (int k = 0; k < iterator.m_Spots.Length; k++)
						{
							ref SpotData reference3 = ref iterator.m_Spots.ElementAt(k);
							num2 += math.select(1, 0, reference3.m_Occupied);
							flag |= reference3.m_Index == num && !reference3.m_Occupied;
						}
						if (num2 != 0 && !flag)
						{
							num2 = random.NextInt(num2);
							for (int l = 0; l < iterator.m_Spots.Length; l++)
							{
								ref SpotData reference4 = ref iterator.m_Spots.ElementAt(l);
								if (!reference4.m_Occupied && num2-- == 0)
								{
									num = reference4.m_Index;
									flag = true;
									break;
								}
							}
						}
						if (flag)
						{
							ActivityLocationElement activityLocationElement2 = bufferData[num];
							transform = ObjectUtils.LocalToWorld(transform2, activityLocationElement2.m_Position, activityLocationElement2.m_Rotation);
							result = true;
						}
					}
					iterator.m_Spots.Dispose();
					return result;
				}
			}
			return false;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FixParkingLocation> __Game_Vehicles_FixParkingLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MovedLocation> __Game_Objects_MovedLocation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public BufferTypeHandle<LaneObject> __Game_Net_LaneObject_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FixParkingLocation> __Game_Vehicles_FixParkingLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RW_ComponentLookup;

		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RW_ComponentLookup;

		public ComponentLookup<PersonalCar> __Game_Vehicles_PersonalCar_RW_ComponentLookup;

		public ComponentLookup<CarKeeper> __Game_Citizens_CarKeeper_RW_ComponentLookup;

		public BufferLookup<LaneObject> __Game_Net_LaneObject_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_FixParkingLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FixParkingLocation>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_MovedLocation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MovedLocation>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_LaneObject_RW_BufferTypeHandle = state.GetBufferTypeHandle<LaneObject>();
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Vehicles_FixParkingLocation_RO_ComponentLookup = state.GetComponentLookup<FixParkingLocation>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentLookup = state.GetComponentLookup<Helicopter>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
			__Game_Vehicles_ParkedCar_RW_ComponentLookup = state.GetComponentLookup<ParkedCar>();
			__Game_Vehicles_ParkedTrain_RW_ComponentLookup = state.GetComponentLookup<ParkedTrain>();
			__Game_Vehicles_PersonalCar_RW_ComponentLookup = state.GetComponentLookup<PersonalCar>();
			__Game_Citizens_CarKeeper_RW_ComponentLookup = state.GetComponentLookup<CarKeeper>();
			__Game_Net_LaneObject_RW_BufferLookup = state.GetBufferLookup<LaneObject>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EntityQuery m_FixQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_FixQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>(),
				ComponentType.ReadOnly<Game.Net.ConnectionLane>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.ParkingLane>(),
				ComponentType.ReadOnly<Game.Objects.SpawnLocation>(),
				ComponentType.ReadOnly<FixParkingLocation>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		RequireForUpdate(m_FixQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<Entity> vehicleQueue = new NativeQueue<Entity>(Allocator.TempJob);
		JobHandle dependencies;
		CollectParkedCarsJob jobData = new CollectParkedCarsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_FixParkingLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_FixParkingLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectionLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovedLocationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_MovedLocation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LaneObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies),
			m_VehicleQueue = vehicleQueue.AsParallelWriter()
		};
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		FixParkingLocationJob jobData2 = new FixParkingLocationJob
		{
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FixParkingLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_FixParkingLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PersonalCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PersonalCar_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CarKeeperData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CarKeeper_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_LaneSearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out dependencies2),
			m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
			m_VehicleQueue = vehicleQueue,
			m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: false, out dependencies4),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		};
		JobHandle job = JobChunkExtensions.ScheduleParallel(jobData, m_FixQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle = IJobExtensions.Schedule(jobData2, JobUtils.CombineDependencies(job, dependencies2, dependencies3, dependencies4));
		vehicleQueue.Dispose(jobHandle);
		m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddMovingSearchTreeWriter(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public FixParkingLocationSystem()
	{
	}
}
