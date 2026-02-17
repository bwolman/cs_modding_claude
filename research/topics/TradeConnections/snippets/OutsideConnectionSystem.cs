using System;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class OutsideConnectionSystem : GameSystemBase
{
	private enum ConnectionType
	{
		Pedestrian,
		Road,
		Track,
		Parking
	}

	private struct NodeData : IComparable<NodeData>
	{
		public float m_Order;

		public float m_Remoteness;

		public float3 m_Position1;

		public float3 m_Position2;

		public PathNode m_Node1;

		public PathNode m_Node2;

		public PathNode m_Node3;

		public ConnectionType m_ConnectionType;

		public TrackTypes m_TrackType;

		public RoadTypes m_RoadType;

		public Entity m_Owner;

		public int CompareTo(NodeData other)
		{
			return math.select(math.select(math.select(math.select(0, math.select(-1, 1, m_Order > other.m_Order), m_Order != other.m_Order), m_TrackType - other.m_TrackType, m_TrackType != other.m_TrackType), m_RoadType - other.m_RoadType, m_RoadType != other.m_RoadType), m_ConnectionType - other.m_ConnectionType, m_ConnectionType != other.m_ConnectionType);
		}
	}

	private struct LaneData : IEquatable<LaneData>
	{
		public PathNode m_Start;

		public PathNode m_End;

		public LaneData(Lane lane)
		{
			m_Start = lane.m_StartNode;
			m_End = lane.m_EndNode;
		}

		public LaneData(NodeData nodeData)
		{
			m_Start = nodeData.m_Node1;
			m_End = nodeData.m_Node3;
		}

		public LaneData(NodeData nodeData1, NodeData nodeData2)
		{
			m_Start = nodeData1.m_Node3;
			m_End = nodeData2.m_Node3;
		}

		public bool Equals(LaneData other)
		{
			if (m_Start.Equals(other.m_Start))
			{
				return m_End.Equals(other.m_End);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (17 * 31 + m_Start.GetHashCode()) * 31 + m_End.GetHashCode();
		}
	}

	[BurstCompile]
	private struct UpdateOutsideConnectionsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_ConnectionChunks;

		[ReadOnly]
		public NativeList<Entity> m_PrefabEntities;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<SubLane> m_SubLaneType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedRoute> m_ConnectedRouteType;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<SecondaryLane> m_SecondaryLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabRouteConnectionData;

		[ReadOnly]
		public ComponentLookup<NetLaneArchetypeData> m_PrefabLaneArchetypeData;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_PrefabOutsideConnectionData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<NetCompositionLane> m_PrefabCompositionLanes;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeList<NodeData> nativeList = new NativeList<NodeData>(100, Allocator.Temp);
			NativeParallelHashMap<LaneData, Entity> laneMap = new NativeParallelHashMap<LaneData, Entity>(100, Allocator.Temp);
			for (int i = 0; i < m_ConnectionChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_ConnectionChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				if (archetypeChunk.Has(ref m_OutsideConnectionType))
				{
					NativeArray<Owner> nativeArray2 = archetypeChunk.GetNativeArray(ref m_OwnerType);
					NativeArray<Transform> nativeArray3 = archetypeChunk.GetNativeArray(ref m_TransformType);
					NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
					bool flag = archetypeChunk.Has(ref m_SubLaneType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						PrefabRef prefabRef = nativeArray4[j];
						OutsideConnectionData outsideConnectionData = m_PrefabOutsideConnectionData[prefabRef.m_Prefab];
						if (nativeArray2.Length != 0)
						{
							Owner owner = nativeArray2[j];
							if (m_ConnectedEdges.HasBuffer(owner.m_Owner))
							{
								DynamicBuffer<ConnectedEdge> connectedEdges = m_ConnectedEdges[owner.m_Owner];
								FillNodeData(owner.m_Owner, connectedEdges, outsideConnectionData, nativeList);
								continue;
							}
						}
						if (flag)
						{
							Entity owner2 = nativeArray[j];
							Transform transform = nativeArray3[j];
							if (m_PrefabRouteConnectionData.HasComponent(prefabRef.m_Prefab))
							{
								RouteConnectionData routeConnectionData = m_PrefabRouteConnectionData[prefabRef.m_Prefab];
								FillNodeData(owner2, transform, outsideConnectionData, routeConnectionData, nativeList);
							}
						}
					}
				}
				else
				{
					NativeArray<Lane> nativeArray5 = archetypeChunk.GetNativeArray(ref m_LaneType);
					for (int k = 0; k < nativeArray5.Length; k++)
					{
						Entity item = nativeArray[k];
						Lane lane = nativeArray5[k];
						laneMap.TryAdd(new LaneData(lane), item);
					}
				}
			}
			nativeList.Sort();
			for (int l = 0; l < nativeList.Length; l++)
			{
				TryCreateLane(nativeList[l], laneMap);
			}
			if (nativeList.Length >= 2)
			{
				int num = 0;
				while (num < nativeList.Length)
				{
					int num2 = num;
					NodeData nodeData = nativeList[num];
					while (++num2 < nativeList.Length)
					{
						NodeData nodeData2 = nativeList[num2];
						if (nodeData2.m_ConnectionType != nodeData.m_ConnectionType || nodeData2.m_TrackType != nodeData.m_TrackType || nodeData2.m_RoadType != nodeData.m_RoadType)
						{
							break;
						}
					}
					if (nodeData.m_ConnectionType != ConnectionType.Parking)
					{
						int num3 = num2 - num;
						int num4 = num3 - 2;
						for (int m = num; m < num2; m++)
						{
							int num5 = m - 1;
							int index = m;
							if (m == num)
							{
								if (num3 <= 2)
								{
									continue;
								}
								num5 += num3;
							}
							NodeData nodeData3 = nativeList[num5];
							NodeData nodeData4 = nativeList[index];
							TryCreateLane(nodeData3, nodeData4, laneMap);
							float num6 = nodeData4.m_Remoteness - nodeData3.m_Remoteness;
							float2 falseValue = math.select(test: new bool2(num6 <= 0f, num6 >= 0f), falseValue: new float2(float.MinValue, float.MaxValue), trueValue: num6);
							for (int n = 1; n < num4; n++)
							{
								if (num6 == 0f)
								{
									break;
								}
								index = m + n;
								index -= math.select(0, num3, index >= num2);
								nodeData4 = nativeList[index];
								num6 = nodeData4.m_Remoteness - nodeData3.m_Remoteness;
								bool2 @bool = new bool2(num6 <= 0f, num6 >= 0f) & new bool2(num6 > falseValue.x, num6 < falseValue.y);
								if (math.any(@bool))
								{
									TryCreateLane(nodeData3, nodeData4, laneMap);
									falseValue = math.select(falseValue, num6, @bool);
								}
							}
						}
					}
					num = num2;
				}
			}
			if (laneMap.Count() != 0)
			{
				NativeArray<Entity> valueArray = laneMap.GetValueArray(Allocator.Temp);
				for (int num7 = 0; num7 < valueArray.Length; num7++)
				{
					m_CommandBuffer.AddComponent(valueArray[num7], default(Deleted));
				}
				valueArray.Dispose();
				for (int num8 = 0; num8 < m_ConnectionChunks.Length; num8++)
				{
					ArchetypeChunk archetypeChunk2 = m_ConnectionChunks[num8];
					if (!archetypeChunk2.Has(ref m_OutsideConnectionType))
					{
						continue;
					}
					BufferAccessor<ConnectedRoute> bufferAccessor = archetypeChunk2.GetBufferAccessor(ref m_ConnectedRouteType);
					for (int num9 = 0; num9 < bufferAccessor.Length; num9++)
					{
						DynamicBuffer<ConnectedRoute> dynamicBuffer = bufferAccessor[num9];
						for (int num10 = 0; num10 < dynamicBuffer.Length; num10++)
						{
							m_CommandBuffer.AddComponent(dynamicBuffer[num10].m_Waypoint, default(Updated));
						}
					}
				}
			}
			laneMap.Dispose();
			nativeList.Dispose();
		}

		private void FillNodeData(Entity owner, Transform transform, OutsideConnectionData outsideConnectionData, RouteConnectionData routeConnectionData, NativeList<NodeData> buffer)
		{
			int num = 0;
			if (routeConnectionData.m_AccessConnectionType == RouteConnectionType.Road)
			{
				NodeData value = default(NodeData);
				value.m_Position1 = transform.m_Position;
				value.m_Position2 = CalculateEndPos(value.m_Position1);
				value.m_Node1 = new PathNode(owner, (ushort)num++);
				value.m_Node2 = new PathNode(owner, (ushort)num++);
				value.m_Node3 = new PathNode(owner, (ushort)num++);
				value.m_Order = math.atan2(value.m_Position1.z, value.m_Position1.x);
				value.m_Remoteness = outsideConnectionData.m_Remoteness;
				value.m_Owner = owner;
				value.m_ConnectionType = ConnectionType.Road;
				value.m_TrackType = TrackTypes.None;
				value.m_RoadType = routeConnectionData.m_AccessRoadType;
				buffer.Add(in value);
			}
			if (routeConnectionData.m_AccessConnectionType == RouteConnectionType.Road)
			{
				NodeData value2 = default(NodeData);
				value2.m_Position1 = transform.m_Position;
				value2.m_Position1.y += 2f;
				value2.m_Position2 = CalculateEndPos(value2.m_Position1);
				value2.m_Node1 = new PathNode(owner, (ushort)num++);
				value2.m_Node2 = new PathNode(owner, (ushort)num++);
				value2.m_Node3 = new PathNode(owner, (ushort)num++);
				value2.m_Order = math.atan2(value2.m_Position1.z, value2.m_Position1.x);
				value2.m_Remoteness = outsideConnectionData.m_Remoteness;
				value2.m_Owner = owner;
				value2.m_ConnectionType = ConnectionType.Pedestrian;
				value2.m_TrackType = TrackTypes.None;
				value2.m_RoadType = RoadTypes.None;
				buffer.Add(in value2);
			}
		}

		private void FillNodeData(Entity node, DynamicBuffer<ConnectedEdge> connectedEdges, OutsideConnectionData outsideConnectionData, NativeList<NodeData> buffer)
		{
			int length = buffer.Length;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			float3 position = default(float3);
			NodeData value = default(NodeData);
			NodeData value2 = default(NodeData);
			for (int i = 0; i < connectedEdges.Length; i++)
			{
				ConnectedEdge connectedEdge = connectedEdges[i];
				bool flag = m_EdgeData[connectedEdge.m_Edge].m_End == node;
				if (!m_UpdatedData.HasComponent(connectedEdge.m_Edge) && m_SubLanes.HasBuffer(connectedEdge.m_Edge))
				{
					DynamicBuffer<SubLane> dynamicBuffer = m_SubLanes[connectedEdge.m_Edge];
					float num5 = math.select(0f, 1f, flag);
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subLane = dynamicBuffer[j].m_SubLane;
						if (!m_EdgeLaneData.HasComponent(subLane) || m_SecondaryLaneData.HasComponent(subLane) || m_SlaveLaneData.HasComponent(subLane))
						{
							continue;
						}
						bool2 x = m_EdgeLaneData[subLane].m_EdgeDelta == num5;
						if (!math.any(x))
						{
							continue;
						}
						bool y = x.y;
						Curve curve = m_CurveData[subLane];
						if (y)
						{
							curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
						}
						PrefabRef prefabRef = m_PrefabRefData[subLane];
						NetLaneData netLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
						if ((!m_MasterLaneData.HasComponent(subLane) && ((uint)netLaneData.m_Flags & (uint)(y ? 512 : 256)) != 0) || (netLaneData.m_Flags & (LaneFlags.Parking | LaneFlags.Utility | LaneFlags.FindAnchor)) != 0)
						{
							continue;
						}
						Lane lane = m_LaneData[subLane];
						byte laneIndex = ((!y) ? ((byte)(lane.m_StartNode.GetLaneIndex() & 0xFF)) : ((byte)(lane.m_EndNode.GetLaneIndex() & 0xFF)));
						value.m_Position1 = curve.m_Bezier.a;
						value.m_Position2 = CalculateEndPos(value.m_Position1);
						value.m_Node1 = new PathNode(connectedEdge.m_Edge, laneIndex, (byte)math.select(0, 4, flag));
						value.m_Node2 = new PathNode(node, (ushort)num++);
						value.m_Node3 = new PathNode(node, (ushort)num++);
						value.m_Order = math.atan2(value.m_Position1.z, value.m_Position1.x);
						value.m_Remoteness = outsideConnectionData.m_Remoteness;
						value.m_Owner = node;
						if ((netLaneData.m_Flags & LaneFlags.Track) != 0)
						{
							TrackLaneData trackLaneData = m_PrefabTrackLaneData[prefabRef.m_Prefab];
							value.m_ConnectionType = ConnectionType.Track;
							value.m_TrackType = trackLaneData.m_TrackTypes;
							value.m_RoadType = RoadTypes.None;
							num2++;
							position += value.m_Position1;
						}
						else if ((netLaneData.m_Flags & LaneFlags.Road) != 0)
						{
							CarLaneData carLaneData = m_PrefabCarLaneData[prefabRef.m_Prefab];
							value.m_ConnectionType = ConnectionType.Road;
							value.m_TrackType = TrackTypes.None;
							value.m_RoadType = carLaneData.m_RoadTypes;
							num3++;
							position += value.m_Position1;
						}
						else
						{
							if ((netLaneData.m_Flags & LaneFlags.Pedestrian) == 0)
							{
								continue;
							}
							value.m_ConnectionType = ConnectionType.Pedestrian;
							value.m_TrackType = TrackTypes.None;
							value.m_RoadType = RoadTypes.None;
							num4++;
							position += value.m_Position1;
						}
						buffer.Add(in value);
					}
					continue;
				}
				Composition composition = m_CompositionData[connectedEdge.m_Edge];
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[connectedEdge.m_Edge];
				NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
				DynamicBuffer<NetCompositionLane> dynamicBuffer2 = m_PrefabCompositionLanes[composition.m_Edge];
				if (flag)
				{
					edgeGeometry.m_Start.m_Left = MathUtils.Invert(edgeGeometry.m_End.m_Right);
					edgeGeometry.m_Start.m_Right = MathUtils.Invert(edgeGeometry.m_End.m_Left);
				}
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					NetCompositionLane netCompositionLane = dynamicBuffer2[k];
					bool flag2 = flag == ((netCompositionLane.m_Flags & LaneFlags.Invert) == 0);
					if ((netCompositionLane.m_Flags & (LaneFlags.Slave | LaneFlags.Parking | LaneFlags.Utility)) != 0 || ((uint)netCompositionLane.m_Flags & (uint)(flag2 ? 512 : 256)) != 0)
					{
						continue;
					}
					float num6 = netCompositionLane.m_Position.x / math.max(1f, netCompositionData.m_Width) + 0.5f;
					if (!flag)
					{
						num6 = 1f - num6;
					}
					value2.m_Position1 = MathUtils.Lerp(edgeGeometry.m_Start.m_Right, edgeGeometry.m_Start.m_Left, num6).a;
					value2.m_Position1.y += netCompositionLane.m_Position.y;
					value2.m_Position2 = CalculateEndPos(value2.m_Position1);
					value2.m_Node1 = new PathNode(connectedEdge.m_Edge, netCompositionLane.m_Index, (byte)math.select(0, 4, flag));
					value2.m_Node2 = new PathNode(node, (ushort)num++);
					value2.m_Node3 = new PathNode(node, (ushort)num++);
					value2.m_Order = math.atan2(value2.m_Position1.z, value2.m_Position1.x);
					value2.m_Remoteness = outsideConnectionData.m_Remoteness;
					value2.m_Owner = node;
					if ((netCompositionLane.m_Flags & LaneFlags.Track) != 0)
					{
						TrackLaneData trackLaneData2 = m_PrefabTrackLaneData[netCompositionLane.m_Lane];
						value2.m_ConnectionType = ConnectionType.Track;
						value2.m_TrackType = trackLaneData2.m_TrackTypes;
						value2.m_RoadType = RoadTypes.None;
						num2++;
						position += value2.m_Position1;
					}
					else if ((netCompositionLane.m_Flags & LaneFlags.Road) != 0)
					{
						CarLaneData carLaneData2 = m_PrefabCarLaneData[netCompositionLane.m_Lane];
						value2.m_ConnectionType = ConnectionType.Road;
						value2.m_TrackType = TrackTypes.None;
						value2.m_RoadType = carLaneData2.m_RoadTypes;
						num3++;
						position += value2.m_Position1;
					}
					else
					{
						if ((netCompositionLane.m_Flags & LaneFlags.Pedestrian) == 0)
						{
							continue;
						}
						value2.m_ConnectionType = ConnectionType.Pedestrian;
						value2.m_TrackType = TrackTypes.None;
						value2.m_RoadType = RoadTypes.None;
						num4++;
						position += value2.m_Position1;
					}
					buffer.Add(in value2);
				}
			}
			if (num4 == 0 && (num2 != 0 || num3 != 0))
			{
				position /= (float)(num2 + num3);
				NodeData value3 = default(NodeData);
				value3.m_Position1 = position;
				value3.m_Position2 = CalculateEndPos(value3.m_Position1);
				value3.m_Node1 = new PathNode(node, (ushort)num++);
				value3.m_Node2 = new PathNode(node, (ushort)num++);
				value3.m_Node3 = new PathNode(node, (ushort)num++);
				value3.m_Order = math.atan2(value3.m_Position1.z, value3.m_Position1.x);
				value3.m_Remoteness = outsideConnectionData.m_Remoteness;
				value3.m_Owner = node;
				value3.m_ConnectionType = ConnectionType.Pedestrian;
				value3.m_TrackType = TrackTypes.None;
				value3.m_RoadType = RoadTypes.None;
				buffer.Add(in value3);
			}
			if (num3 == 0)
			{
				return;
			}
			int length2 = buffer.Length;
			NodeData value4 = default(NodeData);
			for (int l = length; l < length2; l++)
			{
				NodeData nodeData = buffer[l];
				if (nodeData.m_ConnectionType != ConnectionType.Road || nodeData.m_RoadType != RoadTypes.Car)
				{
					continue;
				}
				float num7 = float.MaxValue;
				NodeData nodeData2 = default(NodeData);
				bool flag3 = false;
				for (int m = length; m < length2; m++)
				{
					NodeData nodeData3 = buffer[m];
					if (nodeData3.m_ConnectionType == ConnectionType.Pedestrian)
					{
						float num8 = math.distance(nodeData.m_Position1, nodeData3.m_Position1);
						if (num8 < num7)
						{
							num7 = num8;
							nodeData2 = nodeData3;
							flag3 = true;
						}
					}
				}
				if (flag3)
				{
					value4.m_Position1 = nodeData.m_Position2;
					value4.m_Position2 = nodeData2.m_Position2;
					value4.m_Node1 = new PathNode(nodeData.m_Node2, 1f);
					value4.m_Node2 = new PathNode(node, (ushort)num++);
					value4.m_Node3 = new PathNode(nodeData2.m_Node2, 1f);
					value4.m_Order = math.atan2(value4.m_Position1.z, value4.m_Position1.x);
					value4.m_Remoteness = outsideConnectionData.m_Remoteness;
					value4.m_Owner = node;
					value4.m_ConnectionType = ConnectionType.Parking;
					value4.m_TrackType = TrackTypes.None;
					value4.m_RoadType = RoadTypes.Car;
					buffer.Add(in value4);
				}
			}
		}

		private void TryCreateLane(NodeData nodeData, NativeParallelHashMap<LaneData, Entity> laneMap)
		{
			ConnectionLane connectionLane = default(ConnectionLane);
			connectionLane.m_AccessRestriction = Entity.Null;
			connectionLane.m_Flags = ConnectionLaneFlags.Start | ConnectionLaneFlags.Outside;
			connectionLane.m_TrackTypes = nodeData.m_TrackType;
			connectionLane.m_RoadTypes = nodeData.m_RoadType;
			switch (nodeData.m_ConnectionType)
			{
			case ConnectionType.Road:
				if (nodeData.m_RoadType == RoadTypes.Car)
				{
					connectionLane.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd | ConnectionLaneFlags.Road | ConnectionLaneFlags.AllowMiddle;
				}
				else
				{
					connectionLane.m_Flags |= ConnectionLaneFlags.Road | ConnectionLaneFlags.AllowMiddle;
				}
				break;
			case ConnectionType.Track:
				connectionLane.m_Flags |= ConnectionLaneFlags.Track | ConnectionLaneFlags.AllowMiddle;
				break;
			case ConnectionType.Pedestrian:
				connectionLane.m_Flags |= ConnectionLaneFlags.Pedestrian | ConnectionLaneFlags.AllowMiddle | ConnectionLaneFlags.AllowCargo;
				break;
			case ConnectionType.Parking:
				connectionLane.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.Parking;
				break;
			}
			if (laneMap.TryGetValue(new LaneData(nodeData), out var item) && m_ConnectionLaneData[item].Equals(connectionLane))
			{
				Curve component = CalculateCurve(nodeData);
				Curve curve = m_CurveData[item];
				if (!component.m_Bezier.Equals(curve.m_Bezier))
				{
					m_CommandBuffer.SetComponent(item, component);
					m_CommandBuffer.AddComponent(item, default(Updated));
				}
				laneMap.Remove(new LaneData(nodeData));
			}
			else if (m_PrefabEntities.Length != 0)
			{
				Entity entity = m_PrefabEntities[0];
				NetLaneArchetypeData netLaneArchetypeData = m_PrefabLaneArchetypeData[entity];
				Lane component2 = default(Lane);
				component2.m_StartNode = nodeData.m_Node1;
				component2.m_MiddleNode = nodeData.m_Node2;
				component2.m_EndNode = nodeData.m_Node3;
				item = m_CommandBuffer.CreateEntity(netLaneArchetypeData.m_LaneArchetype);
				m_CommandBuffer.SetComponent(item, new PrefabRef(entity));
				m_CommandBuffer.SetComponent(item, component2);
				m_CommandBuffer.SetComponent(item, CalculateCurve(nodeData));
				m_CommandBuffer.SetComponent(item, connectionLane);
				m_CommandBuffer.AddComponent(item, default(OutsideConnection));
				m_CommandBuffer.AddComponent(item, new Owner(nodeData.m_Owner));
			}
		}

		private void TryCreateLane(NodeData nodeData1, NodeData nodeData2, NativeParallelHashMap<LaneData, Entity> laneMap)
		{
			ConnectionLane connectionLane = default(ConnectionLane);
			connectionLane.m_AccessRestriction = Entity.Null;
			connectionLane.m_Flags = ConnectionLaneFlags.Distance | ConnectionLaneFlags.Outside;
			connectionLane.m_TrackTypes = nodeData1.m_TrackType;
			connectionLane.m_RoadTypes = nodeData1.m_RoadType;
			switch (nodeData1.m_ConnectionType)
			{
			case ConnectionType.Road:
				if (nodeData1.m_RoadType == RoadTypes.Car)
				{
					connectionLane.m_Flags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd | ConnectionLaneFlags.Road;
				}
				else
				{
					connectionLane.m_Flags |= ConnectionLaneFlags.Road;
				}
				break;
			case ConnectionType.Track:
				connectionLane.m_Flags |= ConnectionLaneFlags.Track;
				break;
			case ConnectionType.Pedestrian:
				connectionLane.m_Flags |= ConnectionLaneFlags.Pedestrian;
				break;
			}
			if (laneMap.TryGetValue(new LaneData(nodeData1, nodeData2), out var item) && m_ConnectionLaneData[item].Equals(connectionLane))
			{
				Curve component = CalculateCurve(nodeData1, nodeData2);
				Curve curve = m_CurveData[item];
				if (!component.m_Bezier.Equals(curve.m_Bezier))
				{
					m_CommandBuffer.SetComponent(item, component);
					m_CommandBuffer.AddComponent(item, default(Updated));
				}
				laneMap.Remove(new LaneData(nodeData1, nodeData2));
			}
			else if (m_PrefabEntities.Length != 0)
			{
				Entity entity = m_PrefabEntities[0];
				NetLaneArchetypeData netLaneArchetypeData = m_PrefabLaneArchetypeData[entity];
				Lane component2 = default(Lane);
				component2.m_StartNode = nodeData1.m_Node3;
				component2.m_MiddleNode = default(PathNode);
				component2.m_EndNode = nodeData2.m_Node3;
				item = m_CommandBuffer.CreateEntity(netLaneArchetypeData.m_LaneArchetype);
				m_CommandBuffer.SetComponent(item, new PrefabRef(entity));
				m_CommandBuffer.SetComponent(item, component2);
				m_CommandBuffer.SetComponent(item, CalculateCurve(nodeData1, nodeData2));
				m_CommandBuffer.SetComponent(item, connectionLane);
				m_CommandBuffer.AddComponent(item, default(OutsideConnection));
			}
		}

		private float3 CalculateEndPos(float3 startPos)
		{
			float3 result = startPos;
			float2 value = startPos.xz;
			if (MathUtils.TryNormalize(ref value, 10f))
			{
				result.xz += value;
			}
			return result;
		}

		private Curve CalculateCurve(NodeData nodeData)
		{
			Curve result = default(Curve);
			result.m_Bezier = NetUtils.StraightCurve(nodeData.m_Position1, nodeData.m_Position2);
			result.m_Length = 10f;
			return result;
		}

		private Curve CalculateCurve(NodeData nodeData1, NodeData nodeData2)
		{
			float3 @float = nodeData1.m_Position2;
			float3 float2 = nodeData2.m_Position2;
			float3 b = math.lerp(@float, float2, 1f / 3f);
			float3 c = math.lerp(@float, float2, 2f / 3f);
			float2 value = b.xz;
			float2 value2 = c.xz;
			float num = math.cmax(math.abs(@float.xz - float2.xz));
			float2 float3 = new float2(math.length(@float.xz), math.length(float2.xz));
			float3 = math.lerp(float3.x, float3.y, new float2(1f / 3f, 2f / 3f)) + num;
			if (!MathUtils.TryNormalize(ref value, float3.x))
			{
				value = new float2(0f, float3.x);
			}
			if (!MathUtils.TryNormalize(ref value2, float3.y))
			{
				value2 = new float2(0f, float3.y);
			}
			float num2 = 50f + math.abs(nodeData2.m_Remoteness - nodeData1.m_Remoteness) * 0.5f;
			float2 value3 = float2.xz - @float.xz;
			value += value * (num2 / math.max(1f, float3.x));
			value2 += value2 * (num2 / math.max(1f, float3.y));
			if (MathUtils.TryNormalize(ref value3, num2))
			{
				value -= value3;
				value2 += value3;
			}
			b.xz = value;
			c.xz = value2;
			Curve result = default(Curve);
			result.m_Bezier = new Bezier4x3(@float, b, c, float2);
			result.m_Length = MathUtils.Length(result.m_Bezier);
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneArchetypeData> __Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionLane> __Game_Prefabs_NetCompositionLane_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedRoute>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<SecondaryLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<ConnectionLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup = state.GetComponentLookup<NetLaneArchetypeData>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
			__Game_Prefabs_NetCompositionLane_RO_BufferLookup = state.GetBufferLookup<NetCompositionLane>(isReadOnly: true);
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_ConnectionQuery;

	private EntityQuery m_PrefabQuery;

	private bool m_Regenerate;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.OutsideConnection>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Game.Objects.ElectricityOutsideConnection>(),
				ComponentType.ReadOnly<Game.Objects.WaterPipeOutsideConnection>()
			}
		});
		m_ConnectionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
				ComponentType.ReadOnly<Transform>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Game.Objects.ElectricityOutsideConnection>(),
				ComponentType.ReadOnly<Game.Objects.WaterPipeOutsideConnection>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<OutsideConnection>(),
				ComponentType.ReadOnly<Lane>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ConnectionLaneData>(), ComponentType.ReadOnly<PrefabData>());
	}

	protected override void OnGameLoaded(Context context)
	{
		base.OnGameLoaded(context);
		if (context.version < Version.outsideConnectionRemoteness)
		{
			m_Regenerate = true;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_Regenerate || !m_UpdatedQuery.IsEmptyIgnoreFilter)
		{
			m_Regenerate = false;
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> connectionChunks = m_ConnectionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			JobHandle outJobHandle2;
			NativeList<Entity> prefabEntities = m_PrefabQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle2);
			JobHandle jobHandle = IJobExtensions.Schedule(new UpdateOutsideConnectionsJob
			{
				m_ConnectionChunks = connectionChunks,
				m_PrefabEntities = prefabEntities,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ConnectedRouteType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLaneArchetypeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabOutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabCompositionLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
			}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2));
			connectionChunks.Dispose(jobHandle);
			prefabEntities.Dispose(jobHandle);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle;
		}
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
	public OutsideConnectionSystem()
	{
	}
}
