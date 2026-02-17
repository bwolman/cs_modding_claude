using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.PSI;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ObjectToolSystem : ObjectToolBaseSystem
{
	public enum Mode
	{
		Create,
		Upgrade,
		Move,
		Brush,
		Stamp,
		Line,
		Curve
	}

	public enum State
	{
		Default,
		Rotating,
		Adding,
		Removing
	}

	private struct Rotation
	{
		public quaternion m_Rotation;

		public quaternion m_ParentRotation;

		public bool m_IsAligned;

		public bool m_IsSnapped;
	}

	[BurstCompile]
	private struct SnapJob : IJob
	{
		private struct LoweredParentIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public ControlPoint m_Result;

			public float3 m_Position;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Game.Net.Node> m_NodeData;

			public ComponentLookup<Orphan> m_OrphanData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Position.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Position.xz))
				{
					if (m_EdgeGeometryData.HasComponent(entity))
					{
						CheckEdge(entity);
					}
					else if (m_OrphanData.HasComponent(entity))
					{
						CheckNode(entity);
					}
				}
			}

			private void CheckNode(Entity entity)
			{
				Game.Net.Node node = m_NodeData[entity];
				Orphan orphan = m_OrphanData[entity];
				NetCompositionData netCompositionData = m_PrefabCompositionData[orphan.m_Composition];
				if ((netCompositionData.m_State & CompositionState.Marker) == 0 && ((netCompositionData.m_Flags.m_Left | netCompositionData.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
				{
					float3 position = node.m_Position;
					position.y += netCompositionData.m_SurfaceHeight.max;
					if (math.distance(m_Position.xz, position.xz) <= netCompositionData.m_Width * 0.5f)
					{
						m_Result.m_OriginalEntity = entity;
						m_Result.m_Position = node.m_Position;
						m_Result.m_HitPosition = m_Position;
						m_Result.m_HitPosition.y = position.y;
						m_Result.m_HitDirection = default(float3);
					}
				}
			}

			private void CheckEdge(Entity entity)
			{
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[entity];
				EdgeNodeGeometry geometry = m_StartNodeGeometryData[entity].m_Geometry;
				EdgeNodeGeometry geometry2 = m_EndNodeGeometryData[entity].m_Geometry;
				bool3 x = default(bool3);
				x.x = MathUtils.Intersect(edgeGeometry.m_Bounds.xz, m_Position.xz);
				x.y = MathUtils.Intersect(geometry.m_Bounds.xz, m_Position.xz);
				x.z = MathUtils.Intersect(geometry2.m_Bounds.xz, m_Position.xz);
				if (!math.any(x))
				{
					return;
				}
				Composition composition = m_CompositionData[entity];
				Edge edge = m_EdgeData[entity];
				Curve curve = m_CurveData[entity];
				if (x.x)
				{
					NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
					if ((prefabCompositionData.m_State & CompositionState.Marker) == 0 && ((prefabCompositionData.m_Flags.m_Left | prefabCompositionData.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
					{
						CheckSegment(entity, edgeGeometry.m_Start, curve.m_Bezier, prefabCompositionData);
						CheckSegment(entity, edgeGeometry.m_End, curve.m_Bezier, prefabCompositionData);
					}
				}
				if (x.y)
				{
					NetCompositionData prefabCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
					if ((prefabCompositionData2.m_State & CompositionState.Marker) == 0 && ((prefabCompositionData2.m_Flags.m_Left | prefabCompositionData2.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
					{
						if (geometry.m_MiddleRadius > 0f)
						{
							CheckSegment(edge.m_Start, geometry.m_Left, curve.m_Bezier, prefabCompositionData2);
							Segment right = geometry.m_Right;
							Segment right2 = geometry.m_Right;
							right.m_Right = MathUtils.Lerp(geometry.m_Right.m_Left, geometry.m_Right.m_Right, 0.5f);
							right2.m_Left = MathUtils.Lerp(geometry.m_Right.m_Left, geometry.m_Right.m_Right, 0.5f);
							right.m_Right.d = geometry.m_Middle.d;
							right2.m_Left.d = geometry.m_Middle.d;
							CheckSegment(edge.m_Start, right, curve.m_Bezier, prefabCompositionData2);
							CheckSegment(edge.m_Start, right2, curve.m_Bezier, prefabCompositionData2);
						}
						else
						{
							Segment left = geometry.m_Left;
							Segment right3 = geometry.m_Right;
							CheckSegment(edge.m_Start, left, curve.m_Bezier, prefabCompositionData2);
							CheckSegment(edge.m_Start, right3, curve.m_Bezier, prefabCompositionData2);
							left.m_Right = geometry.m_Middle;
							right3.m_Left = geometry.m_Middle;
							CheckSegment(edge.m_Start, left, curve.m_Bezier, prefabCompositionData2);
							CheckSegment(edge.m_Start, right3, curve.m_Bezier, prefabCompositionData2);
						}
					}
				}
				if (!x.z)
				{
					return;
				}
				NetCompositionData prefabCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
				if ((prefabCompositionData3.m_State & CompositionState.Marker) == 0 && ((prefabCompositionData3.m_Flags.m_Left | prefabCompositionData3.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
				{
					if (geometry2.m_MiddleRadius > 0f)
					{
						CheckSegment(edge.m_End, geometry2.m_Left, curve.m_Bezier, prefabCompositionData3);
						Segment right4 = geometry2.m_Right;
						Segment right5 = geometry2.m_Right;
						right4.m_Right = MathUtils.Lerp(geometry2.m_Right.m_Left, geometry2.m_Right.m_Right, 0.5f);
						right4.m_Right.d = geometry2.m_Middle.d;
						right5.m_Left = right4.m_Right;
						CheckSegment(edge.m_End, right4, curve.m_Bezier, prefabCompositionData3);
						CheckSegment(edge.m_End, right5, curve.m_Bezier, prefabCompositionData3);
					}
					else
					{
						Segment left2 = geometry2.m_Left;
						Segment right6 = geometry2.m_Right;
						CheckSegment(edge.m_End, left2, curve.m_Bezier, prefabCompositionData3);
						CheckSegment(edge.m_End, right6, curve.m_Bezier, prefabCompositionData3);
						left2.m_Right = geometry2.m_Middle;
						right6.m_Left = geometry2.m_Middle;
						CheckSegment(edge.m_End, left2, curve.m_Bezier, prefabCompositionData3);
						CheckSegment(edge.m_End, right6, curve.m_Bezier, prefabCompositionData3);
					}
				}
			}

			private void CheckSegment(Entity entity, Segment segment, Bezier4x3 curve, NetCompositionData prefabCompositionData)
			{
				float3 a = segment.m_Left.a;
				float3 @float = segment.m_Right.a;
				for (int i = 1; i <= 8; i++)
				{
					float t = (float)i / 8f;
					float3 float2 = MathUtils.Position(segment.m_Left, t);
					float3 float3 = MathUtils.Position(segment.m_Right, t);
					Triangle3 triangle = new Triangle3(a, @float, float2);
					Triangle3 triangle2 = new Triangle3(float3, float2, @float);
					if (MathUtils.Intersect(triangle.xz, m_Position.xz, out var t2))
					{
						float3 hitPosition = m_Position;
						hitPosition.y = MathUtils.Position(triangle.y, t2) + prefabCompositionData.m_SurfaceHeight.max;
						MathUtils.Distance(curve.xz, hitPosition.xz, out var t3);
						m_Result.m_OriginalEntity = entity;
						m_Result.m_Position = MathUtils.Position(curve, t3);
						m_Result.m_HitPosition = hitPosition;
						m_Result.m_HitDirection = default(float3);
						m_Result.m_CurvePosition = t3;
					}
					else if (MathUtils.Intersect(triangle2.xz, m_Position.xz, out t2))
					{
						float3 hitPosition2 = m_Position;
						hitPosition2.y = MathUtils.Position(triangle2.y, t2) + prefabCompositionData.m_SurfaceHeight.max;
						MathUtils.Distance(curve.xz, hitPosition2.xz, out var t4);
						m_Result.m_OriginalEntity = entity;
						m_Result.m_Position = MathUtils.Position(curve, t4);
						m_Result.m_HitPosition = hitPosition2;
						m_Result.m_HitDirection = default(float3);
						m_Result.m_CurvePosition = t4;
					}
					a = float2;
					@float = float3;
				}
			}
		}

		private struct OriginalObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_Parent;

			public Entity m_Result;

			public Bounds3 m_Bounds;

			public float m_BestDistance;

			public bool m_EditorMode;

			public TransportStopData m_TransportStopData1;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Attached> m_AttachedData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetObjectData> m_NetObjectData;

			public ComponentLookup<TransportStopData> m_TransportStopData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz) || !m_AttachedData.HasComponent(item) || (!m_EditorMode && m_OwnerData.HasComponent(item)) || m_AttachedData[item].m_Parent != m_Parent)
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[item];
				if (!m_NetObjectData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				if (!m_TransportStopData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					componentData.m_TransportType = TransportType.None;
				}
				if (m_TransportStopData1.m_TransportType == componentData.m_TransportType)
				{
					float num = math.distance(MathUtils.Center(m_Bounds), MathUtils.Center(bounds.m_Bounds));
					if (num < m_BestDistance)
					{
						m_Result = item;
						m_BestDistance = num;
					}
				}
			}
		}

		private struct ParentObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public Bounds3 m_Bounds;

			public float m_BestOverlap;

			public bool m_IsBuilding;

			public ObjectGeometryData m_PrefabObjectGeometryData1;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<BuildingData> m_BuildingData;

			public ComponentLookup<AssetStampData> m_AssetStampData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[item];
				bool flag = m_BuildingData.HasComponent(prefabRef.m_Prefab);
				bool flag2 = m_AssetStampData.HasComponent(prefabRef.m_Prefab);
				if (m_IsBuilding && !flag2)
				{
					return;
				}
				float num = m_BestOverlap;
				if (flag || flag2)
				{
					Game.Objects.Transform transform = m_TransformData[item];
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					float3 @float = MathUtils.Center(bounds.m_Bounds);
					if ((m_PrefabObjectGeometryData1.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
					{
						Circle2 circle = new Circle2(m_PrefabObjectGeometryData1.m_Size.x * 0.5f - 0.01f, (m_ControlPoint.m_Position - @float).xz);
						Bounds2 intersection;
						if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
						{
							Circle2 circle2 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - @float).xz);
							if (MathUtils.Intersect(circle, circle2))
							{
								num = math.distance(new float3
								{
									xz = @float.xz + MathUtils.Center(MathUtils.Bounds(circle) & MathUtils.Bounds(circle2)),
									y = MathUtils.Center(bounds.m_Bounds.y & m_Bounds.y)
								}, m_ControlPoint.m_Position);
							}
						}
						else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transform.m_Position - @float, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz, circle, out intersection))
						{
							num = math.distance(new float3
							{
								xz = @float.xz + MathUtils.Center(intersection),
								y = MathUtils.Center(bounds.m_Bounds.y & m_Bounds.y)
							}, m_ControlPoint.m_Position);
						}
					}
					else
					{
						Quad2 xz = ObjectUtils.CalculateBaseCorners(m_ControlPoint.m_Position - @float, m_ControlPoint.m_Rotation, MathUtils.Expand(m_PrefabObjectGeometryData1.m_Bounds, -0.01f)).xz;
						if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
						{
							Circle2 circle3 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - @float).xz);
							if (MathUtils.Intersect(xz, circle3, out var intersection2))
							{
								num = math.distance(new float3
								{
									xz = @float.xz + MathUtils.Center(intersection2),
									y = MathUtils.Center(bounds.m_Bounds.y & m_Bounds.y)
								}, m_ControlPoint.m_Position);
							}
						}
						else
						{
							Quad2 xz2 = ObjectUtils.CalculateBaseCorners(transform.m_Position - @float, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz;
							if (MathUtils.Intersect(xz, xz2, out var intersection3))
							{
								num = math.distance(new float3
								{
									xz = @float.xz + MathUtils.Center(intersection3),
									y = MathUtils.Center(bounds.m_Bounds.y & m_Bounds.y)
								}, m_ControlPoint.m_Position);
							}
						}
					}
				}
				else
				{
					if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
					{
						return;
					}
					Game.Objects.Transform transform2 = m_TransformData[item];
					ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Physical) == 0 && m_OwnerData.HasComponent(item))
					{
						return;
					}
					float3 float2 = MathUtils.Center(bounds.m_Bounds);
					quaternion q = math.inverse(m_ControlPoint.m_Rotation);
					quaternion q2 = math.inverse(transform2.m_Rotation);
					float3 float3 = math.mul(q, m_ControlPoint.m_Position - float2);
					float3 float4 = math.mul(q2, transform2.m_Position - float2);
					if ((m_PrefabObjectGeometryData1.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
					{
						Cylinder3 cylinder = new Cylinder3
						{
							circle = new Circle2(m_PrefabObjectGeometryData1.m_Size.x * 0.5f - 0.01f, float3.xz),
							height = new Bounds1(0.01f, m_PrefabObjectGeometryData1.m_Size.y - 0.01f) + float3.y,
							rotation = m_ControlPoint.m_Rotation
						};
						if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
						{
							Cylinder3 cylinder2 = new Cylinder3
							{
								circle = new Circle2(objectGeometryData2.m_Size.x * 0.5f - 0.01f, float4.xz),
								height = new Bounds1(0.01f, objectGeometryData2.m_Size.y - 0.01f) + float4.y,
								rotation = transform2.m_Rotation
							};
							float3 pos = default(float3);
							if (Game.Objects.ValidationHelpers.Intersect(cylinder, cylinder2, ref pos))
							{
								num = math.distance(pos, m_ControlPoint.m_Position);
							}
						}
						else
						{
							Box3 box = default(Box3);
							box.bounds = objectGeometryData2.m_Bounds + float4;
							box.bounds = MathUtils.Expand(box.bounds, -0.01f);
							box.rotation = transform2.m_Rotation;
							if (MathUtils.Intersect(cylinder, box, out var cylinderIntersection, out var boxIntersection))
							{
								float3 start = math.mul(cylinder.rotation, MathUtils.Center(cylinderIntersection));
								float3 end = math.mul(box.rotation, MathUtils.Center(boxIntersection));
								num = math.distance(float2 + math.lerp(start, end, 0.5f), m_ControlPoint.m_Position);
							}
						}
					}
					else
					{
						Box3 box2 = default(Box3);
						box2.bounds = m_PrefabObjectGeometryData1.m_Bounds + float3;
						box2.bounds = MathUtils.Expand(box2.bounds, -0.01f);
						box2.rotation = m_ControlPoint.m_Rotation;
						if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
						{
							Cylinder3 cylinder3 = new Cylinder3
							{
								circle = new Circle2(objectGeometryData2.m_Size.x * 0.5f - 0.01f, float4.xz),
								height = new Bounds1(0.01f, objectGeometryData2.m_Size.y - 0.01f) + float4.y,
								rotation = transform2.m_Rotation
							};
							if (MathUtils.Intersect(cylinder3, box2, out var cylinderIntersection2, out var boxIntersection2))
							{
								float3 start2 = math.mul(box2.rotation, MathUtils.Center(boxIntersection2));
								float3 end2 = math.mul(cylinder3.rotation, MathUtils.Center(cylinderIntersection2));
								num = math.distance(float2 + math.lerp(start2, end2, 0.5f), m_ControlPoint.m_Position);
							}
						}
						else
						{
							Box3 box3 = default(Box3);
							box3.bounds = objectGeometryData2.m_Bounds + float4;
							box3.bounds = MathUtils.Expand(box3.bounds, -0.01f);
							box3.rotation = transform2.m_Rotation;
							if (MathUtils.Intersect(box2, box3, out var intersection4, out var intersection5))
							{
								float3 start3 = math.mul(box2.rotation, MathUtils.Center(intersection4));
								float3 end3 = math.mul(box3.rotation, MathUtils.Center(intersection5));
								num = math.distance(float2 + math.lerp(start3, end3, 0.5f), m_ControlPoint.m_Position);
							}
						}
					}
				}
				if (num < m_BestOverlap)
				{
					m_BestSnapPosition = m_ControlPoint;
					m_BestSnapPosition.m_OriginalEntity = item;
					m_BestSnapPosition.m_ElementIndex = new int2(-1, -1);
					m_BestOverlap = num;
				}
			}
		}

		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public quaternion m_Rotation;

			public Bounds2 m_Bounds;

			public float3 m_LocalOffset;

			public float2 m_LocalTangent;

			public Entity m_IgnoreOwner;

			public float m_SnapFactor;

			public NetData m_NetData;

			public NetGeometryData m_NetGeometryData;

			public RoadData m_RoadData;

			public NativeList<SubSnapPoint> m_SubSnapPoints;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Net.Node> m_NodeData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetData> m_PrefabNetData;

			public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity netEntity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || !m_NodeData.TryGetComponent(netEntity, out var componentData))
				{
					return;
				}
				if (m_IgnoreOwner != Entity.Null)
				{
					Entity entity = netEntity;
					Owner componentData2;
					while (m_OwnerData.TryGetComponent(entity, out componentData2))
					{
						if (componentData2.m_Owner == m_IgnoreOwner)
						{
							return;
						}
						entity = componentData2.m_Owner;
					}
				}
				bool flag = true;
				float num = float.MaxValue;
				float num2 = float.MaxValue;
				float3 @float = default(float3);
				float2 float2 = default(float2);
				float3 float3 = math.mul(m_Rotation, m_LocalOffset);
				float2 xz = math.mul(m_Rotation, new float3(m_LocalTangent.x, 0f, m_LocalTangent.y)).xz;
				ControlPoint snapPosition = m_ControlPoint;
				snapPosition.m_OriginalEntity = Entity.Null;
				snapPosition.m_Direction = math.normalizesafe(math.forward(m_Rotation).xz);
				snapPosition.m_Rotation = m_Rotation;
				if (m_ConnectedEdges.TryGetBuffer(netEntity, out var bufferData))
				{
					bool flag2 = (m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) == 0 && (m_RoadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0;
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity edge = bufferData[i].m_Edge;
						Edge edge2 = m_EdgeData[edge];
						Curve curve = m_CurveData[edge];
						float3 float4;
						float2 float5;
						if (edge2.m_Start == netEntity)
						{
							float4 = curve.m_Bezier.a;
							float5 = math.normalizesafe(MathUtils.StartTangent(curve.m_Bezier).xz);
						}
						else
						{
							if (!(edge2.m_End == netEntity))
							{
								continue;
							}
							float4 = curve.m_Bezier.d;
							float5 = math.normalizesafe(-MathUtils.EndTangent(curve.m_Bezier).xz);
						}
						flag = false;
						PrefabRef prefabRef = m_PrefabRefData[edge];
						NetData netData = m_PrefabNetData[prefabRef.m_Prefab];
						if ((m_NetData.m_RequiredLayers & netData.m_RequiredLayers) == 0)
						{
							continue;
						}
						float defaultWidth = m_NetGeometryData.m_DefaultWidth;
						if ((m_NetGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) == 0 && m_PrefabNetGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
						{
							defaultWidth = componentData3.m_DefaultWidth;
						}
						int num3;
						float num4;
						float num5;
						if (flag2)
						{
							int cellWidth = ZoneUtils.GetCellWidth(m_NetGeometryData.m_DefaultWidth);
							int cellWidth2 = ZoneUtils.GetCellWidth(defaultWidth);
							num3 = 1 + math.abs(cellWidth2 - cellWidth);
							num4 = (float)(num3 - 1) * -4f;
							num5 = 8f;
						}
						else
						{
							float num6 = math.abs(defaultWidth - m_NetGeometryData.m_DefaultWidth);
							if (num6 > 1.6f)
							{
								num3 = 3;
								num4 = num6 * -0.5f;
								num5 = num6 * 0.5f;
							}
							else
							{
								num3 = 1;
								num4 = 0f;
								num5 = 0f;
							}
						}
						for (int j = 0; j < num3; j++)
						{
							float3 float6 = float4;
							if (math.abs(num4) >= 0.08f)
							{
								float6.xz += MathUtils.Left(float5) * num4;
							}
							float num7 = math.distancesq(float6 - float3, m_ControlPoint.m_HitPosition);
							if (num7 < num)
							{
								num = num7;
								@float = float6;
							}
							num4 += num5;
						}
						float num8 = math.dot(xz, float5);
						if (num8 < num2)
						{
							num2 = num8;
							float2 = float5;
						}
					}
				}
				if (flag)
				{
					PrefabRef prefabRef2 = m_PrefabRefData[netEntity];
					NetData netData2 = m_PrefabNetData[prefabRef2.m_Prefab];
					if ((m_NetData.m_RequiredLayers & netData2.m_RequiredLayers) != Layer.None && math.distancesq(componentData.m_Position - float3, m_ControlPoint.m_HitPosition) < num)
					{
						@float = componentData.m_Position;
					}
				}
				if (num != float.MaxValue)
				{
					snapPosition.m_Position = @float - float3;
					snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 1f, m_ControlPoint.m_HitPosition * m_SnapFactor, snapPosition.m_Position * m_SnapFactor, snapPosition.m_Direction);
					AddSnapPosition(ref m_BestSnapPosition, snapPosition);
					if (num2 != float.MaxValue && !m_LocalTangent.Equals(default(float2)))
					{
						snapPosition.m_Rotation = quaternion.RotateY(MathUtils.RotationAngleSignedRight(m_LocalTangent, -float2));
						snapPosition.m_Direction = math.normalizesafe(math.forward(snapPosition.m_Rotation).xz);
						snapPosition.m_Position = @float - math.mul(snapPosition.m_Rotation, m_LocalOffset);
						snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 1f, m_ControlPoint.m_HitPosition * m_SnapFactor, snapPosition.m_Position * m_SnapFactor, snapPosition.m_Direction);
						AddSnapPosition(ref m_BestSnapPosition, snapPosition);
					}
					m_SubSnapPoints.Add(new SubSnapPoint
					{
						m_Position = @float,
						m_Tangent = float2
					});
				}
			}
		}

		private struct ZoneBlockIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public ControlPoint m_ControlPoint;

			public ControlPoint m_BestSnapPosition;

			public float m_BestDistance;

			public int2 m_LotSize;

			public Bounds2 m_Bounds;

			public float2 m_Direction;

			public Entity m_IgnoreOwner;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Block> m_BlockData;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (!MathUtils.Intersect(bounds, m_Bounds))
				{
					return;
				}
				if (m_IgnoreOwner != Entity.Null)
				{
					Entity entity = blockEntity;
					Owner componentData;
					while (m_OwnerData.TryGetComponent(entity, out componentData))
					{
						if (componentData.m_Owner == m_IgnoreOwner)
						{
							return;
						}
						entity = componentData.m_Owner;
					}
				}
				Block block = m_BlockData[blockEntity];
				Quad2 quad = ZoneUtils.CalculateCorners(block);
				Line2.Segment line = new Line2.Segment(quad.a, quad.b);
				Line2.Segment line2 = new Line2.Segment(m_ControlPoint.m_HitPosition.xz, m_ControlPoint.m_HitPosition.xz);
				float2 @float = m_Direction * (math.max(0f, m_LotSize.y - m_LotSize.x) * 4f);
				line2.a -= @float;
				line2.b += @float;
				float2 t;
				float num = MathUtils.Distance(line, line2, out t);
				if (num == 0f)
				{
					num -= 0.5f - math.abs(t.y - 0.5f);
				}
				if (!(num >= m_BestDistance))
				{
					m_BestDistance = num;
					float2 y = m_ControlPoint.m_HitPosition.xz - block.m_Position.xz;
					float2 float2 = MathUtils.Left(block.m_Direction);
					float num2 = (float)block.m_Size.y * 4f;
					float num3 = (float)m_LotSize.y * 4f;
					float num4 = math.dot(block.m_Direction, y);
					float num5 = math.dot(float2, y);
					float num6 = math.select(0f, 0.5f, ((block.m_Size.x ^ m_LotSize.x) & 1) != 0);
					num5 -= (math.round(num5 / 8f - num6) + num6) * 8f;
					m_BestSnapPosition = m_ControlPoint;
					m_BestSnapPosition.m_Position = m_ControlPoint.m_HitPosition;
					m_BestSnapPosition.m_Position.xz += block.m_Direction * (num2 - num3 - num4);
					m_BestSnapPosition.m_Position.xz -= float2 * num5;
					m_BestSnapPosition.m_Direction = block.m_Direction;
					m_BestSnapPosition.m_Rotation = ToolUtils.CalculateRotation(m_BestSnapPosition.m_Direction);
					m_BestSnapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, m_ControlPoint.m_HitPosition * 0.5f, m_BestSnapPosition.m_Position * 0.5f, m_BestSnapPosition.m_Direction);
					m_BestSnapPosition.m_OriginalEntity = blockEntity;
				}
			}
		}

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_RemoveUpgrade;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public float m_Distance;

		[ReadOnly]
		public float m_DistanceScale;

		[ReadOnly]
		public Snap m_Snap;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public Entity m_Selected;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Game.Common.Terrain> m_TerrainData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<District> m_DistrictData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<RoadComposition> m_RoadCompositionData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> m_MovingObjectData;

		[ReadOnly]
		public ComponentLookup<AssetStampData> m_AssetStampData;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<NetObjectData> m_NetObjectData;

		[ReadOnly]
		public ComponentLookup<TransportStopData> m_TransportStopData;

		[ReadOnly]
		public ComponentLookup<StackData> m_StackData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_PrefabServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_RoadData;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<SubReplacement> m_SubReplacements;

		[ReadOnly]
		public BufferLookup<NetCompositionArea> m_PrefabCompositionAreas;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> m_PrefabAuxiliaryNets;

		[ReadOnly]
		public BufferLookup<FixedNetElement> m_PrefabFixedNetElements;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public NativeList<ControlPoint> m_ControlPoints;

		public NativeList<SubSnapPoint> m_SubSnapPoints;

		public NativeList<NetToolSystem.UpgradeState> m_UpgradeStates;

		public NativeReference<Rotation> m_Rotation;

		public NativeReference<NetToolSystem.AppliedUpgrade> m_AppliedUpgrade;

		public void Execute()
		{
			m_SubSnapPoints.Clear();
			m_UpgradeStates.Clear();
			ControlPoint controlPoint = m_ControlPoints[m_ControlPoints.Length - 1];
			if ((m_Snap & (Snap.NetArea | Snap.NetNode)) != Snap.None && m_TerrainData.HasComponent(controlPoint.m_OriginalEntity) && !m_BuildingData.HasComponent(m_Prefab))
			{
				FindLoweredParent(ref controlPoint);
			}
			ControlPoint controlPoint2 = controlPoint;
			ControlPoint bestSnapPosition = controlPoint;
			bestSnapPosition.m_OriginalEntity = Entity.Null;
			if (m_OutsideConnectionData.HasComponent(m_Prefab))
			{
				HandleWorldSize(ref bestSnapPosition, controlPoint);
			}
			float waterSurfaceHeight = float.MinValue;
			if ((m_Snap & Snap.Shoreline) != Snap.None)
			{
				float radius = 1f;
				float3 offset = 0f;
				BuildingExtensionData componentData2;
				if (m_BuildingData.TryGetComponent(m_Prefab, out var componentData))
				{
					radius = math.length(componentData.m_LotSize) * 4f;
				}
				else if (m_BuildingExtensionData.TryGetComponent(m_Prefab, out componentData2))
				{
					radius = math.length(componentData2.m_LotSize) * 4f;
				}
				if (m_PlaceableObjectData.TryGetComponent(m_Prefab, out var componentData3))
				{
					offset = componentData3.m_PlacementOffset;
				}
				SnapShoreline(controlPoint, ref bestSnapPosition, ref waterSurfaceHeight, radius, offset);
			}
			if ((m_Snap & Snap.NetSide) != Snap.None)
			{
				BuildingData buildingData = m_BuildingData[m_Prefab];
				float num = (float)buildingData.m_LotSize.y * 4f + 16f;
				float bestDistance = (float)math.cmin(buildingData.m_LotSize) * 4f + 16f;
				ZoneBlockIterator iterator = new ZoneBlockIterator
				{
					m_ControlPoint = controlPoint,
					m_BestSnapPosition = bestSnapPosition,
					m_BestDistance = bestDistance,
					m_LotSize = buildingData.m_LotSize,
					m_Bounds = new Bounds2(controlPoint.m_Position.xz - num, controlPoint.m_Position.xz + num),
					m_Direction = math.forward(m_Rotation.Value.m_Rotation).xz,
					m_IgnoreOwner = ((m_Mode == Mode.Move) ? m_Selected : Entity.Null),
					m_OwnerData = m_OwnerData,
					m_BlockData = m_BlockData
				};
				m_ZoneSearchTree.Iterate(ref iterator);
				bestSnapPosition = iterator.m_BestSnapPosition;
			}
			if ((m_Snap & Snap.ExistingGeometry) != Snap.None && m_PrefabSubNets.TryGetBuffer(m_Prefab, out var bufferData))
			{
				float num2 = 2f;
				if (m_Mode == Mode.Stamp)
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Game.Prefabs.SubNet subNet = bufferData[i];
						if (subNet.m_Snapping.x)
						{
							num2 = math.clamp(math.length(subNet.m_Curve.a.xz) * 0.02f, num2, 4f);
						}
						if (subNet.m_Snapping.y)
						{
							num2 = math.clamp(math.length(subNet.m_Curve.d.xz) * 0.02f, num2, 4f);
						}
					}
				}
				NetIterator iterator2 = new NetIterator
				{
					m_ControlPoint = controlPoint,
					m_BestSnapPosition = bestSnapPosition,
					m_Rotation = m_Rotation.Value.m_Rotation,
					m_IgnoreOwner = ((m_Mode == Mode.Move) ? m_Selected : Entity.Null),
					m_SnapFactor = 1f / num2,
					m_SubSnapPoints = m_SubSnapPoints,
					m_OwnerData = m_OwnerData,
					m_NodeData = m_NodeData,
					m_EdgeData = m_EdgeData,
					m_CurveData = m_CurveData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabNetData = m_NetData,
					m_PrefabNetGeometryData = m_NetGeometryData,
					m_ConnectedEdges = m_ConnectedEdges
				};
				for (int j = 0; j < bufferData.Length; j++)
				{
					Game.Prefabs.SubNet subNet2 = bufferData[j];
					if (subNet2.m_Snapping.x)
					{
						float2 xz = ObjectUtils.LocalToWorld(controlPoint.m_HitPosition, controlPoint.m_Rotation, subNet2.m_Curve.a).xz;
						iterator2.m_Bounds = new Bounds2(xz - 8f * num2, xz + 8f * num2);
						iterator2.m_LocalOffset = subNet2.m_Curve.a;
						iterator2.m_LocalTangent = math.select(default(float2), math.normalizesafe(MathUtils.StartTangent(subNet2.m_Curve).xz), subNet2.m_NodeIndex.y != subNet2.m_NodeIndex.x);
						m_NetData.TryGetComponent(subNet2.m_Prefab, out iterator2.m_NetData);
						m_NetGeometryData.TryGetComponent(subNet2.m_Prefab, out iterator2.m_NetGeometryData);
						m_RoadData.TryGetComponent(subNet2.m_Prefab, out iterator2.m_RoadData);
						m_NetSearchTree.Iterate(ref iterator2);
					}
					if (subNet2.m_Snapping.y)
					{
						float2 xz2 = ObjectUtils.LocalToWorld(controlPoint.m_HitPosition, controlPoint.m_Rotation, subNet2.m_Curve.d).xz;
						iterator2.m_Bounds = new Bounds2(xz2 - 8f * num2, xz2 + 8f * num2);
						iterator2.m_LocalOffset = subNet2.m_Curve.d;
						iterator2.m_LocalTangent = math.normalizesafe(-MathUtils.EndTangent(subNet2.m_Curve).xz);
						m_NetData.TryGetComponent(subNet2.m_Prefab, out iterator2.m_NetData);
						m_NetGeometryData.TryGetComponent(subNet2.m_Prefab, out iterator2.m_NetGeometryData);
						m_RoadData.TryGetComponent(subNet2.m_Prefab, out iterator2.m_RoadData);
						m_NetSearchTree.Iterate(ref iterator2);
					}
				}
				bestSnapPosition = iterator2.m_BestSnapPosition;
			}
			if ((m_Snap & Snap.OwnerSide) != Snap.None)
			{
				Entity entity = Entity.Null;
				Owner componentData4;
				if (m_Mode == Mode.Upgrade)
				{
					entity = m_Selected;
				}
				else if (m_Mode == Mode.Move && m_OwnerData.TryGetComponent(m_Selected, out componentData4))
				{
					entity = componentData4.m_Owner;
				}
				if (entity != Entity.Null)
				{
					BuildingData buildingData2 = m_BuildingData[m_Prefab];
					PrefabRef prefabRef = m_PrefabRefData[entity];
					Game.Objects.Transform transform = m_TransformData[entity];
					BuildingData buildingData3 = m_BuildingData[prefabRef.m_Prefab];
					int2 lotSize = buildingData3.m_LotSize + buildingData2.m_LotSize.y;
					Quad2 xz3 = BuildingUtils.CalculateCorners(transform, lotSize).xz;
					int num3 = buildingData2.m_LotSize.x - 1;
					bool flag = false;
					if (m_PrefabServiceUpgradeData.TryGetComponent(m_Prefab, out var componentData5))
					{
						num3 = math.select(num3, componentData5.m_MaxPlacementOffset, componentData5.m_MaxPlacementOffset >= 0);
						flag |= componentData5.m_MaxPlacementDistance == 0f;
					}
					if (!flag)
					{
						float2 halfLotSize = (float2)buildingData2.m_LotSize * 4f - 0.4f;
						Quad2 xz4 = BuildingUtils.CalculateCorners(transform, buildingData3.m_LotSize).xz;
						Quad2 xz5 = BuildingUtils.CalculateCorners(controlPoint.m_HitPosition, m_Rotation.Value.m_Rotation, halfLotSize).xz;
						flag = MathUtils.Intersect(xz4, xz5) && MathUtils.Intersect(xz3, controlPoint.m_HitPosition.xz);
					}
					CheckSnapLine(buildingData2, transform, controlPoint, ref bestSnapPosition, new Line2(xz3.a, xz3.b), num3, 0f, flag);
					CheckSnapLine(buildingData2, transform, controlPoint, ref bestSnapPosition, new Line2(xz3.b, xz3.c), num3, MathF.PI / 2f, flag);
					CheckSnapLine(buildingData2, transform, controlPoint, ref bestSnapPosition, new Line2(xz3.c, xz3.d), num3, MathF.PI, flag);
					CheckSnapLine(buildingData2, transform, controlPoint, ref bestSnapPosition, new Line2(xz3.d, xz3.a), num3, 4.712389f, flag);
				}
			}
			if ((m_Snap & Snap.NetArea) != Snap.None)
			{
				m_PlaceableObjectData.TryGetComponent(m_Prefab, out var componentData6);
				BuildingData componentData7;
				bool flag2 = m_BuildingData.TryGetComponent(m_Prefab, out componentData7);
				float num4 = 0f;
				if (m_ObjectGeometryData.TryGetComponent(m_Prefab, out var componentData8))
				{
					num4 = (flag2 ? 1f : (((componentData8.m_Flags & Game.Objects.GeometryFlags.Standing) == 0) ? (componentData8.m_Size.z * 0.5f) : (componentData8.m_LegSize.z * 0.5f + componentData8.m_LegOffset.y)));
				}
				if (flag2 && (componentData7.m_Flags & Game.Prefabs.BuildingFlags.CanBeOnRoadArea) == 0)
				{
					if (m_CurveData.TryGetComponent(controlPoint.m_OriginalEntity, out var componentData9))
					{
						ControlPoint snapPosition = controlPoint;
						snapPosition.m_OriginalEntity = controlPoint.m_OriginalEntity;
						snapPosition.m_Position = MathUtils.Position(componentData9.m_Bezier, controlPoint.m_CurvePosition);
						snapPosition.m_Direction = math.normalizesafe(MathUtils.Tangent(componentData9.m_Bezier, controlPoint.m_CurvePosition).xz);
						snapPosition.m_Direction = MathUtils.Left(snapPosition.m_Direction);
						if ((componentData6.m_Flags & Game.Objects.PlacementFlags.Shoreline) != Game.Objects.PlacementFlags.None && m_CompositionData.TryGetComponent(controlPoint.m_OriginalEntity, out var componentData10) && m_PrefabCompositionData.TryGetComponent(componentData10.m_Edge, out var componentData11) && ((componentData11.m_Flags.m_Left ^ componentData11.m_Flags.m_Right) & CompositionFlags.Side.Raised) != 0)
						{
							if ((componentData11.m_Flags.m_Left & CompositionFlags.Side.Raised) != 0)
							{
								snapPosition.m_Direction = -snapPosition.m_Direction;
							}
						}
						else if (math.dot(snapPosition.m_Position.xz - controlPoint.m_HitPosition.xz, snapPosition.m_Direction) < 0f)
						{
							snapPosition.m_Direction = -snapPosition.m_Direction;
						}
						snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);
						snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
						AddSnapPosition(ref bestSnapPosition, snapPosition);
					}
				}
				else if (componentData6.m_SubReplacementType != SubReplacementType.None)
				{
					ControlPoint startPoint = ((m_ControlPoints.Length == 1) ? controlPoint : m_ControlPoints[0]);
					ControlPoint endPoint = controlPoint;
					if (m_EdgeData.HasComponent(startPoint.m_OriginalEntity) || m_NodeData.HasComponent(startPoint.m_OriginalEntity) || m_EdgeData.HasComponent(endPoint.m_OriginalEntity) || m_NodeData.HasComponent(endPoint.m_OriginalEntity))
					{
						PlaceableNetData placeableNetData = new PlaceableNetData
						{
							m_PlacementFlags = Game.Net.PlacementFlags.IsUpgrade,
							m_SetUpgradeFlags = GetCompositionFlags(componentData6.m_SubReplacementType),
							m_SnapDistance = m_DistanceScale * 0.5f
						};
						SubReplacement subReplacement = new SubReplacement
						{
							m_Type = componentData6.m_SubReplacementType,
							m_Prefab = m_Prefab
						};
						NativeList<NetToolSystem.PathEdge> path = new NativeList<NetToolSystem.PathEdge>(Allocator.Temp);
						NetToolSystem.CreatePath(startPoint, endPoint, path, default(NetData), placeableNetData, ref m_EdgeData, ref m_NodeData, ref m_CurveData, ref m_PrefabRefData, ref m_NetData, ref m_ConnectedEdges);
						if (path.Length != 0)
						{
							m_ControlPoints.Clear();
							NetToolSystem.AddControlPoints(m_ControlPoints, m_UpgradeStates, m_AppliedUpgrade, startPoint, endPoint, path, m_Snap, m_RemoveUpgrade, m_LeftHandTraffic, m_EditorMode, default(NetGeometryData), default(RoadData), placeableNetData, subReplacement, ref m_OwnerData, ref m_BorderDistrictData, ref m_DistrictData, ref m_EdgeData, ref m_NodeData, ref m_CurveData, ref m_CompositionData, ref m_UpgradedData, ref m_EdgeGeometryData, ref m_PrefabRefData, ref m_NetData, ref m_NetGeometryData, ref m_PrefabCompositionData, ref m_RoadCompositionData, ref m_ConnectedEdges, ref m_SubReplacements, ref m_PrefabAuxiliaryNets, ref m_PrefabFixedNetElements);
							return;
						}
						bestSnapPosition.m_Position = bestSnapPosition.m_HitPosition;
					}
				}
				else if (m_EdgeGeometryData.HasComponent(controlPoint.m_OriginalEntity))
				{
					EdgeGeometry edgeGeometry = m_EdgeGeometryData[controlPoint.m_OriginalEntity];
					Composition composition = m_CompositionData[controlPoint.m_OriginalEntity];
					NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
					DynamicBuffer<NetCompositionArea> areas = m_PrefabCompositionAreas[composition.m_Edge];
					float num5 = num4;
					bool snapToEdge = flag2 && (componentData6.m_Flags & Game.Objects.PlacementFlags.Shoreline) != 0;
					if ((componentData8.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None && !flag2 && componentData8.m_LegSize.y <= prefabCompositionData.m_HeightRange.max)
					{
						num5 = math.max(num5, componentData8.m_Size.z * 0.5f);
					}
					SnapSegmentAreas(controlPoint, ref bestSnapPosition, num5, snapToEdge, controlPoint.m_OriginalEntity, edgeGeometry.m_Start, prefabCompositionData, areas);
					SnapSegmentAreas(controlPoint, ref bestSnapPosition, num5, snapToEdge, controlPoint.m_OriginalEntity, edgeGeometry.m_End, prefabCompositionData, areas);
				}
				else if (m_ConnectedEdges.HasBuffer(controlPoint.m_OriginalEntity))
				{
					DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[controlPoint.m_OriginalEntity];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Entity edge = dynamicBuffer[k].m_Edge;
						Edge edge2 = m_EdgeData[edge];
						if ((!(edge2.m_Start != controlPoint.m_OriginalEntity) || !(edge2.m_End != controlPoint.m_OriginalEntity)) && m_EdgeGeometryData.HasComponent(edge))
						{
							EdgeGeometry edgeGeometry2 = m_EdgeGeometryData[edge];
							Composition composition2 = m_CompositionData[edge];
							NetCompositionData prefabCompositionData2 = m_PrefabCompositionData[composition2.m_Edge];
							DynamicBuffer<NetCompositionArea> areas2 = m_PrefabCompositionAreas[composition2.m_Edge];
							float num6 = num4;
							bool snapToEdge2 = flag2 && (componentData6.m_Flags & Game.Objects.PlacementFlags.Shoreline) != 0;
							if ((componentData8.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None && !flag2 && componentData8.m_LegSize.y <= prefabCompositionData2.m_HeightRange.max)
							{
								num6 = math.max(num6, componentData8.m_Size.z * 0.5f);
							}
							SnapSegmentAreas(controlPoint, ref bestSnapPosition, num6, snapToEdge2, edge, edgeGeometry2.m_Start, prefabCompositionData2, areas2);
							SnapSegmentAreas(controlPoint, ref bestSnapPosition, num6, snapToEdge2, edge, edgeGeometry2.m_End, prefabCompositionData2, areas2);
						}
					}
				}
			}
			if ((m_Snap & Snap.NetNode) != Snap.None)
			{
				if (m_NodeData.HasComponent(controlPoint.m_OriginalEntity))
				{
					Game.Net.Node node = m_NodeData[controlPoint.m_OriginalEntity];
					SnapNode(controlPoint, ref bestSnapPosition, controlPoint.m_OriginalEntity, node);
				}
				else if (m_EdgeData.HasComponent(controlPoint.m_OriginalEntity))
				{
					Edge edge3 = m_EdgeData[controlPoint.m_OriginalEntity];
					SnapNode(controlPoint, ref bestSnapPosition, edge3.m_Start, m_NodeData[edge3.m_Start]);
					SnapNode(controlPoint, ref bestSnapPosition, edge3.m_End, m_NodeData[edge3.m_End]);
				}
			}
			if ((m_Snap & Snap.ObjectSurface) != Snap.None && m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
			{
				int parentMesh = controlPoint.m_ElementIndex.x;
				Entity entity2 = controlPoint.m_OriginalEntity;
				while (m_OwnerData.HasComponent(entity2))
				{
					if (m_LocalTransformCacheData.HasComponent(entity2) && !m_ServiceUpgradeData.HasComponent(entity2))
					{
						parentMesh = m_LocalTransformCacheData[entity2].m_ParentMesh;
						parentMesh += math.select(1000, -1000, parentMesh < 0);
					}
					entity2 = m_OwnerData[entity2].m_Owner;
				}
				if (m_TransformData.HasComponent(entity2) && m_SubObjects.HasBuffer(entity2))
				{
					SnapSurface(controlPoint, ref bestSnapPosition, entity2, parentMesh);
				}
			}
			if ((m_Snap & (Snap.StraightDirection | Snap.Distance)) != Snap.None && m_ControlPoints.Length >= 2)
			{
				HandleControlPoints(ref bestSnapPosition, controlPoint);
			}
			bool isSnapped = !controlPoint2.m_Position.Equals(bestSnapPosition.m_Position) || !m_Rotation.Value.m_Rotation.Equals(bestSnapPosition.m_Rotation);
			CalculateHeight(ref bestSnapPosition, waterSurfaceHeight);
			if (m_EditorMode)
			{
				if ((m_Snap & Snap.AutoParent) == 0)
				{
					if ((m_Snap & (Snap.NetArea | Snap.NetNode)) == 0 || m_TransformData.HasComponent(bestSnapPosition.m_OriginalEntity) || m_BuildingData.HasComponent(m_Prefab))
					{
						bestSnapPosition.m_OriginalEntity = Entity.Null;
					}
				}
				else if (bestSnapPosition.m_OriginalEntity == Entity.Null)
				{
					ObjectGeometryData objectGeometryData = default(ObjectGeometryData);
					if (m_ObjectGeometryData.HasComponent(m_Prefab))
					{
						objectGeometryData = m_ObjectGeometryData[m_Prefab];
					}
					ParentObjectIterator iterator3 = new ParentObjectIterator
					{
						m_ControlPoint = bestSnapPosition,
						m_BestSnapPosition = bestSnapPosition,
						m_Bounds = ObjectUtils.CalculateBounds(bestSnapPosition.m_Position, bestSnapPosition.m_Rotation, objectGeometryData),
						m_BestOverlap = float.MaxValue,
						m_IsBuilding = m_BuildingData.HasComponent(m_Prefab),
						m_PrefabObjectGeometryData1 = objectGeometryData,
						m_OwnerData = m_OwnerData,
						m_TransformData = m_TransformData,
						m_BuildingData = m_BuildingData,
						m_AssetStampData = m_AssetStampData,
						m_PrefabRefData = m_PrefabRefData,
						m_PrefabObjectGeometryData = m_ObjectGeometryData
					};
					m_ObjectSearchTree.Iterate(ref iterator3);
					bestSnapPosition = iterator3.m_BestSnapPosition;
				}
			}
			if (m_Mode == Mode.Create && m_NetObjectData.HasComponent(m_Prefab) && (m_NodeData.HasComponent(bestSnapPosition.m_OriginalEntity) || m_EdgeData.HasComponent(bestSnapPosition.m_OriginalEntity)))
			{
				FindOriginalObject(ref bestSnapPosition, controlPoint);
			}
			Rotation value = m_Rotation.Value;
			value.m_IsSnapped = isSnapped;
			value.m_IsAligned &= value.m_Rotation.Equals(bestSnapPosition.m_Rotation);
			AlignObject(ref bestSnapPosition, ref value.m_ParentRotation, value.m_IsAligned);
			value.m_Rotation = bestSnapPosition.m_Rotation;
			m_Rotation.Value = value;
			if ((bestSnapPosition.m_OriginalEntity == Entity.Null || bestSnapPosition.m_ElementIndex.x == -1 || bestSnapPosition.m_HitDirection.y > 0.99f) && m_ObjectGeometryData.TryGetComponent(m_Prefab, out var componentData12) && componentData12.m_Bounds.min.y <= -0.01f && ((m_PlaceableObjectData.TryGetComponent(m_Prefab, out var componentData13) && (componentData13.m_Flags & (Game.Objects.PlacementFlags.Wall | Game.Objects.PlacementFlags.Hanging)) != Game.Objects.PlacementFlags.None && (m_Snap & Snap.Upright) != Snap.None) || (m_EditorMode && m_MovingObjectData.HasComponent(m_Prefab))))
			{
				bestSnapPosition.m_Elevation -= componentData12.m_Bounds.min.y;
				bestSnapPosition.m_Position.y -= componentData12.m_Bounds.min.y;
			}
			if (m_StackData.TryGetComponent(m_Prefab, out var componentData14) && componentData14.m_Direction == StackDirection.Up)
			{
				float num7 = componentData14.m_FirstBounds.max + MathUtils.Size(componentData14.m_MiddleBounds) * 2f - componentData14.m_LastBounds.min;
				bestSnapPosition.m_Elevation += num7;
				bestSnapPosition.m_Position.y += num7;
			}
			m_ControlPoints[m_ControlPoints.Length - 1] = bestSnapPosition;
		}

		private CompositionFlags GetCompositionFlags(SubReplacementType subReplacementType)
		{
			if (subReplacementType == SubReplacementType.Tree)
			{
				return new CompositionFlags(CompositionFlags.General.SecondaryMiddleBeautification, (CompositionFlags.Side)0u, CompositionFlags.Side.SecondaryBeautification);
			}
			return default(CompositionFlags);
		}

		private void HandleControlPoints(ref ControlPoint bestSnapPosition, ControlPoint controlPoint)
		{
			ControlPoint snapPosition = controlPoint;
			snapPosition.m_OriginalEntity = Entity.Null;
			snapPosition.m_Position = controlPoint.m_HitPosition;
			ControlPoint prev = m_ControlPoints[m_ControlPoints.Length - 2];
			if (prev.m_Direction.Equals(default(float2)) && m_ControlPoints.Length >= 3)
			{
				prev.m_Direction = math.normalizesafe(prev.m_Position.xz - m_ControlPoints[m_ControlPoints.Length - 3].m_Position.xz);
			}
			float3 value = controlPoint.m_HitPosition - prev.m_Position;
			value = MathUtils.Normalize(value, value.xz);
			value.y = math.clamp(value.y, -1f, 1f);
			float num = float.MaxValue;
			bool flag = false;
			if ((m_Snap & Snap.StraightDirection) != Snap.None)
			{
				float bestDirectionDistance = float.MaxValue;
				if (prev.m_OriginalEntity != Entity.Null)
				{
					HandleStartDirection(prev.m_OriginalEntity, prev, controlPoint, ref bestDirectionDistance, ref snapPosition.m_Position, ref value);
				}
				if (!prev.m_Direction.Equals(default(float2)) && bestDirectionDistance == float.MaxValue)
				{
					ToolUtils.DirectionSnap(ref bestDirectionDistance, ref snapPosition.m_Position, ref value, controlPoint.m_HitPosition, prev.m_Position, new float3(prev.m_Direction.x, 0f, prev.m_Direction.y), m_DistanceScale);
				}
				num = math.min(num, 8f / m_DistanceScale);
				flag = bestDirectionDistance < m_DistanceScale;
			}
			if ((m_Snap & Snap.Distance) != Snap.None)
			{
				float value2 = math.distance(prev.m_Position, snapPosition.m_Position);
				snapPosition.m_Position = prev.m_Position + value * MathUtils.Snap(value2, m_Distance * m_DistanceScale);
				num = math.min(num, 8f / (m_Distance * m_DistanceScale));
				flag = true;
			}
			if (flag)
			{
				snapPosition.m_Direction = value.xz;
				snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition * num, snapPosition.m_Position * num, snapPosition.m_Direction);
				ToolUtils.AddSnapPosition(ref bestSnapPosition, snapPosition);
			}
		}

		private void HandleStartDirection(Entity startEntity, ControlPoint prev, ControlPoint controlPoint, ref float bestDirectionDistance, ref float3 snapPosition, ref float3 snapDirection)
		{
			if (m_TransformData.TryGetComponent(startEntity, out var componentData))
			{
				float3 value = math.forward(componentData.m_Rotation);
				value = MathUtils.Normalize(value, value.xz);
				value.y = math.clamp(value.y, -1f, 1f);
				ToolUtils.DirectionSnap(ref bestDirectionDistance, ref snapPosition, ref snapDirection, controlPoint.m_HitPosition, prev.m_Position, value, m_DistanceScale);
			}
		}

		private void FindLoweredParent(ref ControlPoint controlPoint)
		{
			LoweredParentIterator iterator = new LoweredParentIterator
			{
				m_Result = controlPoint,
				m_Position = controlPoint.m_HitPosition,
				m_EdgeData = m_EdgeData,
				m_NodeData = m_NodeData,
				m_OrphanData = m_OrphanData,
				m_CurveData = m_CurveData,
				m_CompositionData = m_CompositionData,
				m_EdgeGeometryData = m_EdgeGeometryData,
				m_StartNodeGeometryData = m_StartNodeGeometryData,
				m_EndNodeGeometryData = m_EndNodeGeometryData,
				m_PrefabCompositionData = m_PrefabCompositionData
			};
			m_NetSearchTree.Iterate(ref iterator);
			controlPoint = iterator.m_Result;
		}

		private void FindOriginalObject(ref ControlPoint bestSnapPosition, ControlPoint controlPoint)
		{
			OriginalObjectIterator iterator = new OriginalObjectIterator
			{
				m_Parent = bestSnapPosition.m_OriginalEntity,
				m_BestDistance = float.MaxValue,
				m_EditorMode = m_EditorMode,
				m_OwnerData = m_OwnerData,
				m_AttachedData = m_AttachedData,
				m_PrefabRefData = m_PrefabRefData,
				m_NetObjectData = m_NetObjectData,
				m_TransportStopData = m_TransportStopData
			};
			if (m_ObjectGeometryData.TryGetComponent(m_Prefab, out var componentData))
			{
				iterator.m_Bounds = ObjectUtils.CalculateBounds(bestSnapPosition.m_Position, bestSnapPosition.m_Rotation, componentData);
			}
			else
			{
				iterator.m_Bounds = new Bounds3(bestSnapPosition.m_Position - 1f, bestSnapPosition.m_Position + 1f);
			}
			if (m_TransportStopData.TryGetComponent(m_Prefab, out var componentData2))
			{
				iterator.m_TransportStopData1 = componentData2;
			}
			else
			{
				iterator.m_TransportStopData1.m_TransportType = TransportType.None;
			}
			m_ObjectSearchTree.Iterate(ref iterator);
			if (iterator.m_Result != Entity.Null)
			{
				bestSnapPosition.m_OriginalEntity = iterator.m_Result;
			}
		}

		private void HandleWorldSize(ref ControlPoint bestSnapPosition, ControlPoint controlPoint)
		{
			Bounds3 bounds = TerrainUtils.GetBounds(ref m_TerrainHeightData);
			bool2 @bool = false;
			float2 @float = 0f;
			Bounds3 bounds2 = new Bounds3(controlPoint.m_HitPosition, controlPoint.m_HitPosition);
			if (m_ObjectGeometryData.TryGetComponent(m_Prefab, out var componentData))
			{
				bounds2 = ObjectUtils.CalculateBounds(controlPoint.m_HitPosition, controlPoint.m_Rotation, componentData);
			}
			if (bounds2.min.x < bounds.min.x)
			{
				@bool.x = true;
				@float.x = bounds.min.x;
			}
			else if (bounds2.max.x > bounds.max.x)
			{
				@bool.x = true;
				@float.x = bounds.max.x;
			}
			if (bounds2.min.z < bounds.min.z)
			{
				@bool.y = true;
				@float.y = bounds.min.z;
			}
			else if (bounds2.max.z > bounds.max.z)
			{
				@bool.y = true;
				@float.y = bounds.max.z;
			}
			if (math.any(@bool))
			{
				ControlPoint snapPosition = controlPoint;
				snapPosition.m_OriginalEntity = Entity.Null;
				snapPosition.m_Direction = new float2(0f, 1f);
				snapPosition.m_Position.xz = math.select(controlPoint.m_HitPosition.xz, @float, @bool);
				snapPosition.m_Position.y = controlPoint.m_HitPosition.y;
				snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(2f, 1f, 0f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
				snapPosition.m_Rotation = quaternion.LookRotationSafe(new float3
				{
					xz = math.sign(@float)
				}, math.up());
				AddSnapPosition(ref bestSnapPosition, snapPosition);
			}
		}

		public static void AlignRotation(ref quaternion rotation, quaternion parentRotation, bool zAxis)
		{
			if (zAxis)
			{
				float3 forward = math.rotate(rotation, new float3(0f, 0f, 1f));
				float3 up = math.rotate(parentRotation, new float3(0f, 1f, 0f));
				quaternion a = quaternion.LookRotationSafe(forward, up);
				quaternion q = rotation;
				float num = float.MaxValue;
				for (int i = 0; i < 8; i++)
				{
					quaternion quaternion = math.mul(a, quaternion.RotateZ((float)i * (MathF.PI / 4f)));
					float num2 = MathUtils.RotationAngle(rotation, quaternion);
					if (num2 < num)
					{
						q = quaternion;
						num = num2;
					}
				}
				rotation = math.normalizesafe(q, quaternion.identity);
				return;
			}
			float3 forward2 = math.rotate(rotation, new float3(0f, 1f, 0f));
			float3 up2 = math.rotate(parentRotation, new float3(1f, 0f, 0f));
			quaternion a2 = math.mul(quaternion.LookRotationSafe(forward2, up2), quaternion.RotateX(MathF.PI / 2f));
			quaternion q2 = rotation;
			float num3 = float.MaxValue;
			for (int j = 0; j < 8; j++)
			{
				quaternion quaternion2 = math.mul(a2, quaternion.RotateY((float)j * (MathF.PI / 4f)));
				float num4 = MathUtils.RotationAngle(rotation, quaternion2);
				if (num4 < num3)
				{
					q2 = quaternion2;
					num3 = num4;
				}
			}
			rotation = math.normalizesafe(q2, quaternion.identity);
		}

		private void AlignObject(ref ControlPoint controlPoint, ref quaternion parentRotation, bool alignRotation)
		{
			PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
			if (m_PlaceableObjectData.HasComponent(m_Prefab))
			{
				placeableObjectData = m_PlaceableObjectData[m_Prefab];
			}
			if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hanging) != Game.Objects.PlacementFlags.None)
			{
				ObjectGeometryData objectGeometryData = m_ObjectGeometryData[m_Prefab];
				controlPoint.m_Position.y -= objectGeometryData.m_Bounds.max.y;
			}
			parentRotation = quaternion.identity;
			if (m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
			{
				Entity entity = controlPoint.m_OriginalEntity;
				PrefabRef prefabRef = m_PrefabRefData[entity];
				parentRotation = m_TransformData[entity].m_Rotation;
				while (m_OwnerData.HasComponent(entity) && !m_BuildingData.HasComponent(prefabRef.m_Prefab))
				{
					entity = m_OwnerData[entity].m_Owner;
					prefabRef = m_PrefabRefData[entity];
					if (m_TransformData.HasComponent(entity))
					{
						parentRotation = m_TransformData[entity].m_Rotation;
					}
				}
			}
			if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
			{
				float3 @float = math.forward(controlPoint.m_Rotation);
				float3 value = controlPoint.m_HitDirection;
				value.y = math.select(value.y, 0f, (m_Snap & Snap.Upright) != 0);
				if (!MathUtils.TryNormalize(ref value))
				{
					value = @float;
					value.y = math.select(value.y, 0f, (m_Snap & Snap.Upright) != 0);
					if (!MathUtils.TryNormalize(ref value))
					{
						value = new float3(0f, 0f, 1f);
					}
				}
				float3 value2 = math.cross(@float, value);
				if (MathUtils.TryNormalize(ref value2))
				{
					float angle = math.acos(math.clamp(math.dot(@float, value), -1f, 1f));
					controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(value2, angle), controlPoint.m_Rotation), quaternion.identity);
					if (alignRotation)
					{
						AlignRotation(ref controlPoint.m_Rotation, parentRotation, zAxis: true);
					}
				}
				controlPoint.m_Position += math.forward(controlPoint.m_Rotation) * placeableObjectData.m_PlacementOffset.z;
				return;
			}
			float3 float2 = math.rotate(controlPoint.m_Rotation, new float3(0f, 1f, 0f));
			float3 hitDirection = controlPoint.m_HitDirection;
			hitDirection = math.select(hitDirection, new float3(0f, 1f, 0f), (m_Snap & Snap.Upright) != 0);
			if (!MathUtils.TryNormalize(ref hitDirection))
			{
				hitDirection = float2;
			}
			float3 value3 = math.cross(float2, hitDirection);
			if (MathUtils.TryNormalize(ref value3))
			{
				float angle2 = math.acos(math.clamp(math.dot(float2, hitDirection), -1f, 1f));
				controlPoint.m_Rotation = math.normalizesafe(math.mul(quaternion.AxisAngle(value3, angle2), controlPoint.m_Rotation), quaternion.identity);
				if (alignRotation)
				{
					AlignRotation(ref controlPoint.m_Rotation, parentRotation, zAxis: false);
				}
			}
		}

		private void CalculateHeight(ref ControlPoint controlPoint, float waterSurfaceHeight)
		{
			if (!m_PlaceableObjectData.HasComponent(m_Prefab))
			{
				return;
			}
			PlaceableObjectData placeableObjectData = m_PlaceableObjectData[m_Prefab];
			if (m_SubObjects.HasBuffer(controlPoint.m_OriginalEntity))
			{
				controlPoint.m_Position.y += placeableObjectData.m_PlacementOffset.y;
				return;
			}
			float num;
			if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None && m_BuildingData.HasComponent(m_Prefab))
			{
				BuildingData buildingData = m_BuildingData[m_Prefab];
				float3 worldPosition = BuildingUtils.CalculateFrontPosition(new Game.Objects.Transform(controlPoint.m_Position, controlPoint.m_Rotation), buildingData.m_LotSize.y);
				num = TerrainUtils.SampleHeight(ref m_TerrainHeightData, worldPosition);
			}
			else
			{
				num = TerrainUtils.SampleHeight(ref m_TerrainHeightData, controlPoint.m_Position);
			}
			if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
			{
				float num2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, controlPoint.m_Position);
				num2 += placeableObjectData.m_PlacementOffset.y;
				controlPoint.m_Elevation = math.max(0f, num2 - num);
				num = math.max(num, num2);
			}
			else if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == 0)
			{
				num += placeableObjectData.m_PlacementOffset.y;
			}
			else
			{
				float num3 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, controlPoint.m_Position, out float waterDepth);
				if (waterDepth >= 0.2f)
				{
					num3 += placeableObjectData.m_PlacementOffset.y;
					if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Floating) != Game.Objects.PlacementFlags.None)
					{
						controlPoint.m_Elevation = math.max(0f, num3 - num);
					}
					num = math.max(num, num3);
				}
			}
			if ((m_Snap & Snap.Shoreline) != Snap.None)
			{
				num = math.max(num, waterSurfaceHeight + placeableObjectData.m_PlacementOffset.y);
			}
			controlPoint.m_Position.y = num;
		}

		private void SnapSurface(ControlPoint controlPoint, ref ControlPoint bestPosition, Entity entity, int parentMesh)
		{
			Game.Objects.Transform transform = m_TransformData[entity];
			ControlPoint snapPosition = controlPoint;
			snapPosition.m_OriginalEntity = entity;
			snapPosition.m_ElementIndex.x = parentMesh;
			snapPosition.m_Position = controlPoint.m_HitPosition;
			snapPosition.m_Direction = math.forward(transform.m_Rotation).xz;
			snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
			AddSnapPosition(ref bestPosition, snapPosition);
		}

		private void SnapNode(ControlPoint controlPoint, ref ControlPoint bestPosition, Entity entity, Game.Net.Node node)
		{
			Bounds1 bounds = new Bounds1(float.MaxValue, float.MinValue);
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if (edge2.m_Start == entity)
				{
					Composition composition = m_CompositionData[edge];
					bounds |= m_PrefabCompositionData[composition.m_StartNode].m_SurfaceHeight;
				}
				else if (edge2.m_End == entity)
				{
					Composition composition2 = m_CompositionData[edge];
					bounds |= m_PrefabCompositionData[composition2.m_EndNode].m_SurfaceHeight;
				}
			}
			ControlPoint snapPosition = controlPoint;
			snapPosition.m_OriginalEntity = entity;
			snapPosition.m_Position = node.m_Position;
			if (bounds.min < float.MaxValue)
			{
				snapPosition.m_Position.y += bounds.min;
			}
			snapPosition.m_Direction = math.normalizesafe(math.forward(node.m_Rotation)).xz;
			snapPosition.m_Rotation = node.m_Rotation;
			snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
			AddSnapPosition(ref bestPosition, snapPosition);
		}

		private void SnapShoreline(ControlPoint controlPoint, ref ControlPoint bestPosition, ref float waterSurfaceHeight, float radius, float3 offset)
		{
			int2 x = (int2)math.floor(WaterUtils.ToSurfaceSpace(ref m_WaterSurfaceData, controlPoint.m_HitPosition - radius).xz);
			int2 x2 = (int2)math.ceil(WaterUtils.ToSurfaceSpace(ref m_WaterSurfaceData, controlPoint.m_HitPosition + radius).xz);
			x = math.max(x, default(int2));
			x2 = math.min(x2, m_WaterSurfaceData.resolution.xz - 1);
			float3 @float = default(float3);
			float3 float2 = default(float3);
			float2 float3 = default(float2);
			for (int i = x.y; i <= x2.y; i++)
			{
				for (int j = x.x; j <= x2.x; j++)
				{
					float3 worldPosition = WaterUtils.GetWorldPosition(ref m_WaterSurfaceData, new int2(j, i));
					if (worldPosition.y > 0.2f)
					{
						float num = TerrainUtils.SampleHeight(ref m_TerrainHeightData, worldPosition) + worldPosition.y;
						float num2 = math.max(0f, radius * radius - math.distancesq(worldPosition.xz, controlPoint.m_HitPosition.xz));
						worldPosition.y = (worldPosition.y - 0.2f) * num2;
						worldPosition.xz *= worldPosition.y;
						float2 += worldPosition;
						num *= num2;
						float3 += new float2(num, num2);
					}
					else if (worldPosition.y < 0.2f)
					{
						float num3 = math.max(0f, radius * radius - math.distancesq(worldPosition.xz, controlPoint.m_HitPosition.xz));
						worldPosition.y = (0.2f - worldPosition.y) * num3;
						worldPosition.xz *= worldPosition.y;
						@float += worldPosition;
					}
				}
			}
			if (@float.y != 0f && float2.y != 0f && float3.y != 0f)
			{
				@float /= @float.y;
				float2 /= float2.y;
				float3 value = new float3
				{
					xz = @float.xz - float2.xz
				};
				if (MathUtils.TryNormalize(ref value))
				{
					waterSurfaceHeight = float3.x / float3.y;
					bestPosition = controlPoint;
					bestPosition.m_Position.xz = math.lerp(float2.xz, @float.xz, 0.5f);
					bestPosition.m_Position.y = waterSurfaceHeight + offset.y;
					bestPosition.m_Position += value * offset.z;
					bestPosition.m_Direction = value.xz;
					bestPosition.m_Rotation = ToolUtils.CalculateRotation(bestPosition.m_Direction);
					bestPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, 0f, controlPoint.m_HitPosition, bestPosition.m_Position, bestPosition.m_Direction);
					bestPosition.m_OriginalEntity = Entity.Null;
				}
			}
		}

		private void SnapSegmentAreas(ControlPoint controlPoint, ref ControlPoint bestPosition, float radius, bool snapToEdge, Entity entity, Segment segment1, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1)
		{
			Bezier4x3 curve = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, 0.5f);
			for (int i = 0; i < areas1.Length; i++)
			{
				NetCompositionArea netCompositionArea = areas1[i];
				if ((netCompositionArea.m_Flags & NetAreaFlags.Buildable) == 0)
				{
					continue;
				}
				float num = netCompositionArea.m_Width * 0.51f;
				if (radius >= num)
				{
					continue;
				}
				Bezier4x3 curve2 = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, netCompositionArea.m_Position.x / prefabCompositionData1.m_Width + 0.5f);
				MathUtils.Distance(curve2.xz, controlPoint.m_HitPosition.xz, out var t);
				ControlPoint snapPosition = controlPoint;
				snapPosition.m_OriginalEntity = entity;
				snapPosition.m_Position = MathUtils.Position(curve2, t);
				snapPosition.m_Direction = math.normalizesafe(MathUtils.Tangent(curve2, t).xz);
				if ((netCompositionArea.m_Flags & NetAreaFlags.Median) != 0)
				{
					snapPosition.m_Direction = MathUtils.Left(snapPosition.m_Direction);
					if (((prefabCompositionData1.m_Flags.m_Left ^ prefabCompositionData1.m_Flags.m_Right) & CompositionFlags.Side.Raised) != 0)
					{
						if ((prefabCompositionData1.m_Flags.m_Left & CompositionFlags.Side.Raised) != 0)
						{
							snapPosition.m_Direction = -snapPosition.m_Direction;
						}
					}
					else if (math.dot(MathUtils.Position(curve, t).xz - controlPoint.m_HitPosition.xz, snapPosition.m_Direction) < 0f)
					{
						snapPosition.m_Direction = -snapPosition.m_Direction;
					}
				}
				else if ((netCompositionArea.m_Flags & NetAreaFlags.Invert) != 0)
				{
					snapPosition.m_Direction = MathUtils.Right(snapPosition.m_Direction);
				}
				else
				{
					snapPosition.m_Direction = MathUtils.Left(snapPosition.m_Direction);
				}
				if (snapToEdge)
				{
					float num2 = netCompositionArea.m_Position.x + math.select(0f - netCompositionArea.m_Width, netCompositionArea.m_Width, math.dot(snapPosition.m_Direction, MathUtils.Left(MathUtils.Tangent(curve2, t).xz)) >= 0f) * 0.5f;
					Bezier4x3 curve3 = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, num2 / prefabCompositionData1.m_Width + 0.5f);
					snapPosition.m_Position = MathUtils.Position(curve3, t);
				}
				else
				{
					float3 @float = MathUtils.Position(MathUtils.Lerp(segment1.m_Left, segment1.m_Right, netCompositionArea.m_SnapPosition.x / prefabCompositionData1.m_Width + 0.5f), t);
					float maxLength = math.max(0f, math.min(netCompositionArea.m_Width * 0.5f, math.abs(netCompositionArea.m_SnapPosition.x - netCompositionArea.m_Position.x) + netCompositionArea.m_SnapWidth * 0.5f) - radius);
					float maxLength2 = math.max(0f, netCompositionArea.m_SnapWidth * 0.5f - radius);
					snapPosition.m_Position.xz += MathUtils.ClampLength(@float.xz - snapPosition.m_Position.xz, maxLength);
					snapPosition.m_Position.xz += MathUtils.ClampLength(controlPoint.m_HitPosition.xz - snapPosition.m_Position.xz, maxLength2);
				}
				snapPosition.m_Position.y += netCompositionArea.m_Position.y;
				snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);
				snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, 1f, controlPoint.m_HitPosition, snapPosition.m_Position, snapPosition.m_Direction);
				AddSnapPosition(ref bestPosition, snapPosition);
			}
		}

		private static Bounds3 SetHeightRange(Bounds3 bounds, Bounds1 heightRange)
		{
			bounds.min.y += heightRange.min;
			bounds.max.y += heightRange.max;
			return bounds;
		}

		private static void CheckSnapLine(BuildingData buildingData, Game.Objects.Transform ownerTransformData, ControlPoint controlPoint, ref ControlPoint bestPosition, Line2 line, int maxOffset, float angle, bool forceSnap)
		{
			MathUtils.Distance(line, controlPoint.m_Position.xz, out var t);
			float num = math.select(0f, 4f, ((buildingData.m_LotSize.x - buildingData.m_LotSize.y) & 1) != 0);
			float num2 = (float)math.min(2 * maxOffset - buildingData.m_LotSize.y - buildingData.m_LotSize.x, buildingData.m_LotSize.y - buildingData.m_LotSize.x) * 4f;
			float num3 = math.distance(line.a, line.b);
			t *= num3;
			t = MathUtils.Snap(t + num, 8f) - num;
			t = math.clamp(t, 0f - num2, num3 + num2);
			ControlPoint snapPosition = controlPoint;
			snapPosition.m_OriginalEntity = Entity.Null;
			snapPosition.m_Position.y = ownerTransformData.m_Position.y;
			snapPosition.m_Position.xz = MathUtils.Position(line, t / num3);
			snapPosition.m_Direction = math.mul(math.mul(ownerTransformData.m_Rotation, quaternion.RotateY(angle)), new float3(0f, 0f, 1f)).xz;
			snapPosition.m_Rotation = ToolUtils.CalculateRotation(snapPosition.m_Direction);
			float level = math.select(0f, 1f, forceSnap);
			snapPosition.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, 0f, controlPoint.m_HitPosition * 0.5f, snapPosition.m_Position * 0.5f, snapPosition.m_Direction);
			AddSnapPosition(ref bestPosition, snapPosition);
		}

		private static void AddSnapPosition(ref ControlPoint bestSnapPosition, ControlPoint snapPosition)
		{
			if (ToolUtils.CompareSnapPriority(snapPosition.m_SnapPriority, bestSnapPosition.m_SnapPriority))
			{
				bestSnapPosition = snapPosition;
			}
		}
	}

	[BurstCompile]
	private struct FindAttachmentBuildingJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> m_BuildingDataType;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

		[ReadOnly]
		public BuildingData m_BuildingData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		public NativeReference<AttachmentData> m_AttachmentPrefab;

		public void Execute()
		{
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(2000000);
			int2 lotSize = m_BuildingData.m_LotSize;
			bool2 @bool = new bool2((m_BuildingData.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) != 0, (m_BuildingData.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0);
			AttachmentData value = default(AttachmentData);
			BuildingData buildingData = default(BuildingData);
			float num = 0f;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<BuildingData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_BuildingDataType);
				NativeArray<SpawnableBuildingData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_SpawnableBuildingType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if (nativeArray3[j].m_Level != 1)
					{
						continue;
					}
					BuildingData buildingData2 = nativeArray2[j];
					int2 lotSize2 = buildingData2.m_LotSize;
					bool2 bool2 = new bool2((buildingData2.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) != 0, (buildingData2.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0);
					if (math.all(lotSize2 <= lotSize))
					{
						int2 @int = math.select(lotSize - lotSize2, 0, lotSize2 == lotSize - 1);
						float num2 = (float)(lotSize2.x * lotSize2.y) * random.NextFloat(1f, 1.05f);
						num2 += (float)(@int.x * lotSize2.y) * random.NextFloat(0.95f, 1f);
						num2 += (float)(lotSize.x * @int.y) * random.NextFloat(0.55f, 0.6f);
						num2 /= (float)(lotSize.x * lotSize.y);
						num2 *= math.csum(math.select(0.01f, 0.5f, @bool == bool2));
						if (num2 > num)
						{
							value.m_Entity = nativeArray[j];
							buildingData = buildingData2;
							num = num2;
						}
					}
				}
			}
			if (value.m_Entity != Entity.Null)
			{
				float z = (float)(m_BuildingData.m_LotSize.y - buildingData.m_LotSize.y) * 4f;
				value.m_Offset = new float3(0f, 0f, z);
			}
			m_AttachmentPrefab.Value = value;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Brush> __Game_Tools_Brush_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Common.Terrain> __Game_Common_Terrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<District> __Game_Areas_District_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadComposition> __Game_Prefabs_RoadComposition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> __Game_Prefabs_MovingObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetObjectData> __Game_Prefabs_NetObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubReplacement> __Game_Net_SubReplacement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionArea> __Game_Prefabs_NetCompositionArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AuxiliaryNet> __Game_Prefabs_AuxiliaryNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<FixedNetElement> __Game_Prefabs_FixedNetElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Extension> __Game_Buildings_Extension_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Tools_Brush_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Brush>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Common_Terrain_RO_ComponentLookup = state.GetComponentLookup<Game.Common.Terrain>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentLookup = state.GetComponentLookup<District>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_RoadComposition_RO_ComponentLookup = state.GetComponentLookup<RoadComposition>(isReadOnly: true);
			__Game_Prefabs_MovingObjectData_RO_ComponentLookup = state.GetComponentLookup<MovingObjectData>(isReadOnly: true);
			__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Prefabs_NetObjectData_RO_ComponentLookup = state.GetComponentLookup<NetObjectData>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubReplacement_RO_BufferLookup = state.GetBufferLookup<SubReplacement>(isReadOnly: true);
			__Game_Prefabs_NetCompositionArea_RO_BufferLookup = state.GetBufferLookup<NetCompositionArea>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_AuxiliaryNet_RO_BufferLookup = state.GetBufferLookup<AuxiliaryNet>(isReadOnly: true);
			__Game_Prefabs_FixedNetElement_RO_BufferLookup = state.GetBufferLookup<FixedNetElement>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Extension_RO_ComponentLookup = state.GetComponentLookup<Extension>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
		}
	}

	public const string kToolID = "Object Tool";

	private const string kTree = "Tree";

	private Snap m_SelectedSnap;

	private float m_Distance;

	private AreaToolSystem m_AreaToolSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Zones.SearchSystem m_ZoneSearchSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private AudioManager m_AudioManager;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_TempQuery;

	private EntityQuery m_ContainerQuery;

	private EntityQuery m_BrushQuery;

	private EntityQuery m_LotQuery;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_VisibleQuery;

	private IProxyAction m_EraseObject;

	private IProxyAction m_MoveObject;

	private IProxyAction m_PaintObject;

	private IProxyAction m_PlaceObject;

	private IProxyAction m_PlaceUpgrade;

	private IProxyAction m_PreciseRotation;

	private IProxyAction m_RotateObject;

	private IProxyAction m_PlaceNetEdge;

	private IProxyAction m_PlaceNetControlPoint;

	private IProxyAction m_UndoNetControlPoint;

	private IProxyAction m_DowngradeNetEdge;

	private IProxyAction m_UpgradeNetEdge;

	private IProxyAction m_DiscardUpgrade;

	private IProxyAction m_DiscardDowngrade;

	private IProxyAction m_ReplaceNetEdge;

	private IProxyAction m_IncorrectApply;

	private bool m_ApplyBlocked;

	private NativeList<ControlPoint> m_ControlPoints;

	private NativeList<SubSnapPoint> m_SubSnapPoints;

	private NativeList<NetToolSystem.UpgradeState> m_UpgradeStates;

	private NativeReference<Rotation> m_Rotation;

	private NativeReference<NetToolSystem.AppliedUpgrade> m_AppliedUpgrade;

	private ControlPoint m_LastRaycastPoint;

	private ControlPoint m_StartPoint;

	private Entity m_UpgradingObject;

	private Entity m_MovingObject;

	private Entity m_MovingInitialized;

	private State m_State;

	private Mode m_LastActualMode;

	private bool m_RotationModified;

	private bool m_ForceCancel;

	private float3 m_RotationStartPosition;

	private quaternion m_StartRotation;

	private float m_StartCameraAngle;

	private EntityQuery m_SoundQuery;

	private RandomSeed m_RandomSeed;

	private ObjectPrefab m_Prefab;

	private ObjectPrefab m_SelectedPrefab;

	private TransformPrefab m_TransformPrefab;

	private CameraController m_CameraController;

	private TypeHandle __TypeHandle;

	public override string toolID => "Object Tool";

	public override int uiModeIndex => (int)actualMode;

	public Mode mode { get; set; }

	public Mode actualMode
	{
		get
		{
			Mode mode = this.mode;
			if (!allowBrush && mode == Mode.Brush)
			{
				mode = Mode.Create;
			}
			if (!allowLine && mode == Mode.Line)
			{
				mode = Mode.Create;
			}
			if (!allowCurve && mode == Mode.Curve)
			{
				mode = Mode.Create;
			}
			if (!allowStamp && mode == Mode.Stamp)
			{
				mode = Mode.Create;
			}
			if (!allowCreate && allowBrush && mode == Mode.Create)
			{
				mode = Mode.Brush;
			}
			if (!allowCreate && allowStamp && mode == Mode.Create)
			{
				mode = Mode.Stamp;
			}
			return mode;
		}
	}

	public bool isUpgradeMode
	{
		get
		{
			bool flag = m_UpgradeStates.Length >= 1;
			if (flag)
			{
				flag = actualMode switch
				{
					Mode.Create => true, 
					Mode.Line => true, 
					Mode.Curve => true, 
					_ => false, 
				};
			}
			return flag;
		}
	}

	public AgeMask ageMask { get; set; }

	public AgeMask actualAgeMask
	{
		get
		{
			if (!allowAge)
			{
				return AgeMask.Sapling;
			}
			if ((ageMask & (AgeMask.Sapling | AgeMask.Young | AgeMask.Mature | AgeMask.Elderly)) == 0)
			{
				return AgeMask.Sapling;
			}
			return ageMask;
		}
	}

	[CanBeNull]
	public ObjectPrefab prefab
	{
		get
		{
			return m_SelectedPrefab;
		}
		set
		{
			if (!(value != m_SelectedPrefab))
			{
				return;
			}
			m_SelectedPrefab = value;
			m_ForceUpdate = true;
			allowCreate = true;
			allowLine = false;
			allowCurve = false;
			allowBrush = false;
			allowStamp = false;
			allowAge = false;
			allowRotation = true;
			if (value != null)
			{
				m_TransformPrefab = null;
				if (m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(m_SelectedPrefab, out var component))
				{
					allowLine = (component.m_Flags & Game.Objects.GeometryFlags.Brushable) != 0;
					allowCurve = (component.m_Flags & Game.Objects.GeometryFlags.Brushable) != 0;
					allowBrush = (component.m_Flags & Game.Objects.GeometryFlags.Brushable) != 0;
					allowStamp = (component.m_Flags & Game.Objects.GeometryFlags.Stampable) != 0;
					allowCreate = !allowStamp || m_ToolSystem.actionMode.IsEditor();
					float x = (((component.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None) ? component.m_Size.x : math.length(component.m_Size.xz));
					distanceScale = math.pow(2f, math.clamp(math.round(math.log2(x)), 0f, 5f));
				}
				if (m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(m_SelectedPrefab, out var component2))
				{
					allowRotation = component2.m_RotationSymmetry != RotationSymmetry.Any;
				}
				allowAge = m_ToolSystem.actionMode.IsGame() && m_PrefabSystem.HasComponent<TreeData>(m_SelectedPrefab);
			}
			m_ToolSystem.EventPrefabChanged?.Invoke(value);
		}
	}

	public TransformPrefab transform
	{
		get
		{
			return m_TransformPrefab;
		}
		set
		{
			if (value != m_TransformPrefab)
			{
				m_TransformPrefab = value;
				m_ForceUpdate = true;
				if (value != null)
				{
					m_SelectedPrefab = null;
					allowCreate = true;
					allowLine = false;
					allowCurve = false;
					allowBrush = false;
					allowStamp = false;
					allowAge = false;
				}
				m_ToolSystem.EventPrefabChanged?.Invoke(value);
			}
		}
	}

	public override Snap selectedSnap
	{
		get
		{
			return m_SelectedSnap;
		}
		set
		{
			if (value != m_SelectedSnap)
			{
				m_SelectedSnap = value;
				m_ForceUpdate = true;
			}
		}
	}

	public float distance
	{
		get
		{
			return m_Distance;
		}
		set
		{
			if (value != m_Distance)
			{
				m_Distance = value;
				m_ForceUpdate = true;
			}
		}
	}

	public float distanceScale { get; private set; }

	public bool underground { get; set; }

	public bool allowCreate { get; private set; }

	public bool allowLine { get; private set; }

	public bool allowCurve { get; private set; }

	public bool allowBrush { get; private set; }

	public bool allowStamp { get; private set; }

	public bool allowAge { get; private set; }

	public bool allowRotation { get; private set; }

	public override bool brushing => actualMode == Mode.Brush;

	public State state => m_State;

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_EraseObject;
			yield return m_MoveObject;
			yield return m_PaintObject;
			yield return m_PlaceObject;
			yield return m_PlaceUpgrade;
			yield return m_PreciseRotation;
			yield return m_RotateObject;
			yield return m_PlaceNetEdge;
			yield return m_PlaceNetControlPoint;
			yield return m_UndoNetControlPoint;
			yield return m_DowngradeNetEdge;
			yield return m_UpgradeNetEdge;
			yield return m_DiscardUpgrade;
			yield return m_DiscardDowngrade;
			yield return m_ReplaceNetEdge;
			yield return m_IncorrectApply;
		}
	}

	private float cameraAngle
	{
		get
		{
			if (!(m_CameraController != null))
			{
				return 0f;
			}
			return m_CameraController.angle.x;
		}
	}

	public override void GetUIModes(List<ToolMode> modes)
	{
		Mode mode = this.mode;
		if (mode != Mode.Create && (uint)(mode - 3) > 3u)
		{
			return;
		}
		if (allowCreate)
		{
			if (prefab != null && prefab.Has<TreeObject>())
			{
				modes.Add(new ToolMode(Mode.Create.ToString() + "Tree", 0));
			}
			else
			{
				modes.Add(new ToolMode(Mode.Create.ToString(), 0));
			}
		}
		if (allowLine)
		{
			modes.Add(new ToolMode(Mode.Line.ToString(), 5));
		}
		if (allowCurve)
		{
			modes.Add(new ToolMode(Mode.Curve.ToString(), 6));
		}
		if (allowBrush)
		{
			modes.Add(new ToolMode(Mode.Brush.ToString(), 3));
		}
		if (allowStamp)
		{
			modes.Add(new ToolMode(Mode.Stamp.ToString(), 4));
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_ContainerQuery = GetContainerQuery();
		m_BrushQuery = GetBrushQuery();
		m_ControlPoints = new NativeList<ControlPoint>(1, Allocator.Persistent);
		m_SubSnapPoints = new NativeList<SubSnapPoint>(10, Allocator.Persistent);
		m_UpgradeStates = new NativeList<NetToolSystem.UpgradeState>(10, Allocator.Persistent);
		m_Rotation = new NativeReference<Rotation>(Allocator.Persistent);
		m_AppliedUpgrade = new NativeReference<NetToolSystem.AppliedUpgrade>(Allocator.Persistent);
		m_Rotation.Value = new Rotation
		{
			m_Rotation = quaternion.identity,
			m_ParentRotation = quaternion.identity,
			m_IsAligned = true
		};
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_LotQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Areas.Lot>(), ComponentType.ReadOnly<Temp>());
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<SpawnableBuildingData>(), ComponentType.ReadOnly<BuildingSpawnGroupData>(), ComponentType.ReadOnly<PrefabData>());
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>());
		m_VisibleQuery = GetEntityQuery(ComponentType.ReadOnly<Brush>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_EraseObject = InputManager.instance.toolActionCollection.GetActionState("Erase Object", "ObjectToolSystem");
		m_MoveObject = InputManager.instance.toolActionCollection.GetActionState("Move Object", "ObjectToolSystem");
		m_PaintObject = InputManager.instance.toolActionCollection.GetActionState("Paint Object", "ObjectToolSystem");
		m_PlaceObject = InputManager.instance.toolActionCollection.GetActionState("Place Object", "ObjectToolSystem");
		m_PlaceUpgrade = InputManager.instance.toolActionCollection.GetActionState("Place Upgrade", "ObjectToolSystem");
		m_PreciseRotation = InputManager.instance.toolActionCollection.GetActionState("Precise Rotation", "ObjectToolSystem");
		m_RotateObject = InputManager.instance.toolActionCollection.GetActionState("Rotate Object", "ObjectToolSystem");
		m_PlaceNetEdge = InputManager.instance.toolActionCollection.GetActionState("Place Net Edge", "ObjectToolSystem");
		m_PlaceNetControlPoint = InputManager.instance.toolActionCollection.GetActionState("Place Net Control Point", "ObjectToolSystem");
		m_UndoNetControlPoint = InputManager.instance.toolActionCollection.GetActionState("Undo Net Control Point", "ObjectToolSystem");
		m_DowngradeNetEdge = InputManager.instance.toolActionCollection.GetActionState("Downgrade Net Edge", "ObjectToolSystem");
		m_UpgradeNetEdge = InputManager.instance.toolActionCollection.GetActionState("Upgrade Net Edge", "ObjectToolSystem");
		m_DiscardUpgrade = InputManager.instance.toolActionCollection.GetActionState("Discard Upgrade", "ObjectToolSystem");
		m_DiscardDowngrade = InputManager.instance.toolActionCollection.GetActionState("Discard Downgrade", "ObjectToolSystem");
		m_ReplaceNetEdge = InputManager.instance.toolActionCollection.GetActionState("Replace Net Edge", "ObjectToolSystem");
		m_IncorrectApply = InputManager.instance.toolActionCollection.GetActionState("Incorrect Apply", "ObjectToolSystem");
		base.brushSize = 200f;
		base.brushAngle = 0f;
		base.brushStrength = 0.5f;
		distance = 3f;
		distanceScale = 1f;
		selectedSnap &= ~(Snap.AutoParent | Snap.ContourLines);
		ageMask = AgeMask.Sapling;
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		base.brushType = FindDefaultBrush(m_BrushQuery);
		base.brushSize = 200f;
		base.brushAngle = 0f;
		base.brushStrength = 0.5f;
		distance = 3f;
		distanceScale = 1f;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ControlPoints.Dispose();
		m_SubSnapPoints.Dispose();
		m_UpgradeStates.Dispose();
		m_Rotation.Dispose();
		m_AppliedUpgrade.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ControlPoints.Clear();
		m_SubSnapPoints.Clear();
		m_UpgradeStates.Clear();
		m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
		m_LastRaycastPoint = default(ControlPoint);
		m_StartPoint = default(ControlPoint);
		m_State = State.Default;
		m_MovingInitialized = Entity.Null;
		m_ForceCancel = false;
		m_ApplyBlocked = false;
		Randomize();
		base.requireZones = false;
		base.requireUnderground = false;
		base.requireNetArrows = false;
		base.requireAreas = AreaTypeMask.Lots;
		base.requireNet = Layer.None;
		if (m_ToolSystem.actionMode.IsEditor())
		{
			base.requireAreas |= AreaTypeMask.Spaces;
		}
	}

	private protected override void ResetActions()
	{
		base.ResetActions();
		m_PreciseRotation.shouldBeEnabled = false;
		m_IncorrectApply.shouldBeEnabled = false;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			if (isUpgradeMode)
			{
				if (m_State == State.Default || m_UpgradeStates.Length == 1)
				{
					bool replacementExist = false;
					base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowUpgrade(out replacementExist);
					base.applyActionOverride = (replacementExist ? m_ReplaceNetEdge : m_UpgradeNetEdge);
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowDowngrade(out var _);
					base.secondaryApplyActionOverride = m_DowngradeNetEdge;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					m_PreciseRotation.shouldBeEnabled = false;
				}
				else if (m_State == State.Adding)
				{
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = m_UpgradeNetEdge;
					base.secondaryApplyAction.shouldBeEnabled = false;
					base.secondaryApplyActionOverride = null;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled;
					base.cancelActionOverride = m_DiscardUpgrade;
					m_PreciseRotation.shouldBeEnabled = false;
				}
				else if (m_State == State.Removing)
				{
					base.applyAction.shouldBeEnabled = false;
					base.applyActionOverride = null;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
					base.secondaryApplyActionOverride = m_DowngradeNetEdge;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled;
					base.cancelActionOverride = m_DiscardDowngrade;
					m_PreciseRotation.shouldBeEnabled = false;
				}
			}
			else
			{
				switch (actualMode)
				{
				case Mode.Create:
					base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
					base.applyActionOverride = m_PlaceObject;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowRotation();
					base.secondaryApplyActionOverride = m_RotateObject;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					m_PreciseRotation.shouldBeEnabled = base.actionsEnabled && GetAllowPreciseRotation();
					break;
				case Mode.Upgrade:
					base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
					base.applyActionOverride = m_PlaceUpgrade;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowRotation();
					base.secondaryApplyActionOverride = m_RotateObject;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled;
					base.cancelActionOverride = m_MouseCancel;
					m_PreciseRotation.shouldBeEnabled = base.actionsEnabled && GetAllowPreciseRotation();
					break;
				case Mode.Move:
					base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
					base.applyActionOverride = m_MoveObject;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowRotation();
					base.secondaryApplyActionOverride = m_RotateObject;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					m_PreciseRotation.shouldBeEnabled = base.actionsEnabled && GetAllowPreciseRotation();
					break;
				case Mode.Brush:
				{
					IProxyAction proxyAction = base.applyAction;
					bool replacementExist2 = base.actionsEnabled;
					if (replacementExist2)
					{
						replacementExist2 = m_State switch
						{
							State.Default => GetAllowApply(), 
							State.Adding => true, 
							_ => false, 
						};
					}
					proxyAction.shouldBeEnabled = replacementExist2;
					base.applyActionOverride = m_PaintObject;
					proxyAction = base.secondaryApplyAction;
					replacementExist2 = base.actionsEnabled;
					if (replacementExist2)
					{
						replacementExist2 = m_State switch
						{
							State.Default => GetAllowApply(), 
							State.Removing => true, 
							_ => false, 
						};
					}
					proxyAction.shouldBeEnabled = replacementExist2;
					base.secondaryApplyActionOverride = m_EraseObject;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					m_PreciseRotation.shouldBeEnabled = false;
					break;
				}
				case Mode.Stamp:
					base.applyAction.shouldBeEnabled = base.actionsEnabled && (GetAllowApply() || m_State != State.Default);
					base.applyActionOverride = m_PlaceObject;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowRotation();
					base.secondaryApplyActionOverride = m_RotateObject;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					m_PreciseRotation.shouldBeEnabled = base.actionsEnabled && GetAllowPreciseRotation();
					break;
				case Mode.Line:
				case Mode.Curve:
					base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
					base.applyActionOverride = ((m_ControlPoints.Length < GetMaxControlPointCount(actualMode)) ? m_PlaceNetControlPoint : m_PlaceNetEdge);
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowRotation() && (InputManager.instance.isGamepadControlSchemeActive || m_ControlPoints.Length <= 1);
					base.secondaryApplyActionOverride = m_RotateObject;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled && m_ControlPoints.Length >= 2;
					base.cancelActionOverride = m_UndoNetControlPoint;
					m_PreciseRotation.shouldBeEnabled = base.actionsEnabled && GetAllowPreciseRotation();
					break;
				}
			}
			m_IncorrectApply.shouldBeEnabled = base.actionsEnabled && !base.applyAction.shouldBeEnabled;
		}
	}

	public override PrefabBase GetPrefab()
	{
		Mode mode = actualMode;
		if (mode == Mode.Create || (uint)(mode - 3) <= 3u)
		{
			if (!(prefab != null))
			{
				return transform;
			}
			return prefab;
		}
		return null;
	}

	public NativeList<ControlPoint> GetControlPoints(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_ControlPoints;
	}

	public NativeList<SubSnapPoint> GetSubSnapPoints(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_SubSnapPoints;
	}

	public NativeList<NetToolSystem.UpgradeState> GetNetUpgradeStates(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_UpgradeStates;
	}

	protected override bool GetAllowApply()
	{
		if (base.GetAllowApply())
		{
			return !m_TempQuery.IsEmptyIgnoreFilter;
		}
		return false;
	}

	private bool GetAllowRotation()
	{
		if (allowRotation || actualMode == Mode.Create || actualMode == Mode.Move)
		{
			if (m_Rotation.Value.m_IsSnapped)
			{
				return m_ToolSystem.actionMode.IsEditor();
			}
			return true;
		}
		return false;
	}

	private bool GetAllowUpgrade(out bool replacementExist)
	{
		return GetAllowUpgradeOrDowngrade(Condition, State.Adding, out replacementExist);
		bool Condition(NetToolSystem.UpgradeState upgradeState, SubReplacement replacement)
		{
			if (upgradeState.m_SubReplacementPrefab == Entity.Null)
			{
				return true;
			}
			if (replacement.m_Side == upgradeState.m_SubReplacementSide && replacement.m_AgeMask == actualAgeMask)
			{
				return replacement.m_Prefab == upgradeState.m_SubReplacementPrefab;
			}
			return false;
		}
	}

	private bool GetAllowDowngrade(out bool replacementExist)
	{
		return GetAllowUpgradeOrDowngrade(Condition, State.Removing, out replacementExist);
		static bool Condition(NetToolSystem.UpgradeState upgradeState, SubReplacement replacement)
		{
			return replacement.m_Side == upgradeState.m_SubReplacementSide;
		}
	}

	private bool GetAllowUpgradeOrDowngrade(Func<NetToolSystem.UpgradeState, SubReplacement, bool> condition, State actionMode, out bool replacementExist)
	{
		replacementExist = false;
		if (m_UpgradeStates.Length == 0 || m_ControlPoints.Length < 4)
		{
			return false;
		}
		ref NativeList<ControlPoint> reference = ref m_ControlPoints;
		Entity originalEntity = reference[reference.Length - 3].m_OriginalEntity;
		ref NativeList<ControlPoint> reference2 = ref m_ControlPoints;
		Entity originalEntity2 = reference2[reference2.Length - 2].m_OriginalEntity;
		if (!base.EntityManager.TryGetBuffer(originalEntity, isReadOnly: true, out DynamicBuffer<ConnectedEdge> buffer))
		{
			return false;
		}
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity edge = buffer[i].m_Edge;
			if (!base.EntityManager.TryGetComponent<Edge>(edge, out var component) || ((component.m_Start != originalEntity || component.m_End != originalEntity2) && (component.m_End != originalEntity || component.m_Start != originalEntity2)))
			{
				continue;
			}
			if (!base.EntityManager.TryGetBuffer(edge, isReadOnly: true, out DynamicBuffer<SubReplacement> buffer2))
			{
				return actionMode switch
				{
					State.Adding => true, 
					State.Removing => false, 
					_ => false, 
				};
			}
			for (int j = 0; j < buffer2.Length; j++)
			{
				SubReplacement arg = buffer2[j];
				if (arg.m_Side == m_UpgradeStates[0].m_SubReplacementSide)
				{
					replacementExist = true;
				}
				if (condition(m_UpgradeStates[0], arg))
				{
					return actionMode switch
					{
						State.Adding => false, 
						State.Removing => true, 
						_ => false, 
					};
				}
			}
			return actionMode switch
			{
				State.Adding => true, 
				State.Removing => false, 
				_ => false, 
			};
		}
		return false;
	}

	private bool GetAllowPreciseRotation()
	{
		if (GetAllowRotation())
		{
			if (!InputManager.instance.isGamepadControlSchemeActive)
			{
				return !InputManager.instance.mouseOverUI;
			}
			return true;
		}
		return false;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (prefab is ObjectPrefab objectPrefab)
		{
			Mode mode = this.mode;
			if (!m_ToolSystem.actionMode.IsEditor() && prefab.Has<Game.Prefabs.ServiceUpgrade>())
			{
				Entity entity = m_PrefabSystem.GetEntity(prefab);
				if (!InternalCompilerInterface.HasComponentAfterCompletingDependency(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef, entity))
				{
					return false;
				}
				mode = Mode.Upgrade;
			}
			else if (mode == Mode.Upgrade || mode == Mode.Move)
			{
				mode = Mode.Create;
			}
			this.prefab = objectPrefab;
			this.mode = mode;
			return true;
		}
		if (prefab is TransformPrefab transformPrefab)
		{
			transform = transformPrefab;
			this.mode = Mode.Create;
			return true;
		}
		return false;
	}

	public void StartMoving(Entity movingObject)
	{
		m_MovingObject = movingObject;
		if (m_ToolSystem.actionMode.IsEditor() && base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(m_MovingObject) && base.EntityManager.TryGetComponent<Owner>(m_MovingObject, out var component))
		{
			m_MovingObject = component.m_Owner;
		}
		m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_RelocateBuildingSound);
		mode = Mode.Move;
		prefab = m_PrefabSystem.GetPrefab<ObjectPrefab>(base.EntityManager.GetComponentData<PrefabRef>(m_MovingObject));
	}

	private void Randomize()
	{
		m_RandomSeed = RandomSeed.Next();
		if (!(m_SelectedPrefab != null) || !m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(m_SelectedPrefab, out var component) || component.m_RotationSymmetry == RotationSymmetry.None)
		{
			return;
		}
		Unity.Mathematics.Random random = m_RandomSeed.GetRandom(567890109);
		Rotation value = m_Rotation.Value;
		float num = MathF.PI * 2f;
		if (component.m_RotationSymmetry == RotationSymmetry.Any)
		{
			num = random.NextFloat(num);
			value.m_IsAligned = false;
		}
		else
		{
			num *= (float)random.NextInt((int)component.m_RotationSymmetry) / (float)(int)component.m_RotationSymmetry;
		}
		if ((component.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
		{
			value.m_Rotation = math.normalizesafe(math.mul(value.m_Rotation, quaternion.RotateZ(num)), quaternion.identity);
			if (value.m_IsAligned)
			{
				SnapJob.AlignRotation(ref value.m_Rotation, value.m_ParentRotation, zAxis: true);
			}
		}
		else
		{
			value.m_Rotation = math.normalizesafe(math.mul(value.m_Rotation, quaternion.RotateY(num)), quaternion.identity);
			if (value.m_IsAligned)
			{
				SnapJob.AlignRotation(ref value.m_Rotation, value.m_ParentRotation, zAxis: false);
			}
		}
		m_Rotation.Value = value;
	}

	private ObjectPrefab GetObjectPrefab()
	{
		if (m_ToolSystem.actionMode.IsEditor() && m_TransformPrefab != null && GetContainers(m_ContainerQuery, out var _, out var transformContainer))
		{
			return m_PrefabSystem.GetPrefab<ObjectPrefab>(transformContainer);
		}
		if (actualMode == Mode.Move)
		{
			Entity entity = m_MovingObject;
			if (m_ToolSystem.actionMode.IsEditor() && base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(entity) && base.EntityManager.TryGetComponent<Owner>(entity, out var component))
			{
				entity = component.m_Owner;
			}
			if (base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component2))
			{
				return m_PrefabSystem.GetPrefab<ObjectPrefab>(component2);
			}
		}
		return m_SelectedPrefab;
	}

	public override void SetUnderground(bool underground)
	{
		this.underground = underground;
	}

	public override void ElevationUp()
	{
		underground = false;
	}

	public override void ElevationDown()
	{
		underground = true;
	}

	public override void ElevationScroll()
	{
		underground = !underground;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		m_Prefab = GetObjectPrefab();
		if (m_Prefab != null)
		{
			float3 rayOffset = default(float3);
			Bounds3 bounds = default(Bounds3);
			if (m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(m_Prefab, out var component))
			{
				rayOffset.y -= component.m_Pivot.y;
				bounds = component.m_Bounds;
			}
			if (m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(m_Prefab, out var component2))
			{
				rayOffset.y -= component2.m_PlacementOffset.y;
				if ((component2.m_Flags & Game.Objects.PlacementFlags.Hanging) != Game.Objects.PlacementFlags.None)
				{
					rayOffset.y += bounds.max.y;
				}
			}
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.IgnoreSecondary;
			m_ToolRaycastSystem.rayOffset = rayOffset;
			GetAvailableSnapMask(out var onMask, out var offMask);
			Snap snap = ToolBaseSystem.GetActualSnap(selectedSnap, onMask, offMask);
			Mode mode = actualMode;
			if (component2.m_SubReplacementType != SubReplacementType.None && (snap & Snap.NetArea) != Snap.None && (mode == Mode.Line || mode == Mode.Curve) && m_UpgradeStates.Length == 0 && m_ControlPoints.Length >= 2 && m_State != State.Adding && m_State != State.Removing)
			{
				snap = (Snap)((uint)snap & 0xFFFFFFEFu);
			}
			if ((snap & (Snap.NetArea | Snap.NetNode)) != Snap.None)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Net;
				m_ToolRaycastSystem.netLayerMask |= Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.SubwayTrack | Layer.PublicTransportRoad;
				if ((snap & Snap.NetNode) != Snap.None)
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
				}
			}
			if ((snap & Snap.ObjectSurface) != Snap.None)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
				if (m_ToolSystem.actionMode.IsEditor())
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
				}
			}
			if ((snap & (Snap.NetArea | Snap.NetNode | Snap.ObjectSurface)) != Snap.None && !m_PrefabSystem.HasComponent<BuildingData>(m_Prefab))
			{
				if (underground)
				{
					m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.PartialSurface;
				}
				else
				{
					m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
					if ((component2.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
					{
						m_ToolRaycastSystem.typeMask |= TypeMask.Water;
					}
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
					m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
				}
			}
			else
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
				if ((component2.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
				{
					m_ToolRaycastSystem.typeMask |= TypeMask.Water;
				}
				m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
				m_ToolRaycastSystem.netLayerMask |= Layer.None;
			}
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Water;
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
			m_ToolRaycastSystem.netLayerMask = Layer.None;
			m_ToolRaycastSystem.rayOffset = default(float3);
		}
		if (m_ToolSystem.actionMode.IsEditor())
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
		}
	}

	private void InitializeRotation(Entity entity, PlaceableObjectData placeableObjectData)
	{
		Rotation value = new Rotation
		{
			m_Rotation = quaternion.identity,
			m_ParentRotation = quaternion.identity,
			m_IsAligned = true
		};
		if (base.EntityManager.TryGetComponent<Game.Objects.Transform>(entity, out var component))
		{
			value.m_Rotation = component.m_Rotation;
		}
		if (base.EntityManager.TryGetComponent<Owner>(entity, out var component2))
		{
			Entity owner = component2.m_Owner;
			if (base.EntityManager.TryGetComponent<Game.Objects.Transform>(owner, out var component3))
			{
				value.m_ParentRotation = component3.m_Rotation;
			}
			while (base.EntityManager.TryGetComponent<Owner>(owner, out component2) && !base.EntityManager.HasComponent<Building>(owner))
			{
				owner = component2.m_Owner;
				if (base.EntityManager.TryGetComponent<Game.Objects.Transform>(owner, out component3))
				{
					value.m_ParentRotation = component3.m_Rotation;
				}
			}
		}
		quaternion rotation = value.m_Rotation;
		if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Wall) != Game.Objects.PlacementFlags.None)
		{
			SnapJob.AlignRotation(ref rotation, value.m_ParentRotation, zAxis: true);
		}
		else
		{
			SnapJob.AlignRotation(ref rotation, value.m_ParentRotation, zAxis: false);
		}
		if (MathUtils.RotationAngle(value.m_Rotation, rotation) > 0.01f)
		{
			value.m_IsAligned = false;
		}
		m_Rotation.Value = value;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		m_UpgradingObject = Entity.Null;
		if (this.mode == Mode.Upgrade && !base.EntityManager.HasBuffer<InstalledUpgrade>(GetUpgradable(m_ToolSystem.selected)))
		{
			this.mode = Mode.Create;
		}
		Mode mode = actualMode;
		if (mode == Mode.Brush && base.brushType == null)
		{
			base.brushType = FindDefaultBrush(m_BrushQuery);
		}
		if (mode != m_LastActualMode)
		{
			if (mode != Mode.Move)
			{
				m_MovingObject = Entity.Null;
			}
			if (m_LastActualMode == Mode.Brush)
			{
				m_ControlPoints.Clear();
			}
			bool flag = mode == Mode.Create || mode == Mode.Line || mode == Mode.Curve;
			if (m_UpgradeStates.Length != 0)
			{
				if (!flag)
				{
					m_ControlPoints.Clear();
					m_UpgradeStates.Clear();
					m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
				}
			}
			else
			{
				int maxControlPointCount = GetMaxControlPointCount(mode);
				if (maxControlPointCount < m_ControlPoints.Length)
				{
					m_ControlPoints.RemoveRange(maxControlPointCount, m_ControlPoints.Length - maxControlPointCount);
				}
			}
			m_LastActualMode = mode;
		}
		bool flag2 = m_ForceCancel;
		m_ForceCancel = false;
		if (m_CameraController == null && CameraController.TryGet(out var cameraController))
		{
			m_CameraController = cameraController;
		}
		UpdateActions();
		if (m_Prefab != null)
		{
			allowUnderground = false;
			base.requireUnderground = false;
			base.requireNet = Layer.None;
			base.requireNetArrows = false;
			base.requireStops = TransportType.None;
			UpdateInfoview(m_ToolSystem.actionMode.IsEditor() ? Entity.Null : m_PrefabSystem.GetEntity(m_Prefab));
			GetAvailableSnapMask(out m_SnapOnMask, out m_SnapOffMask);
			m_PrefabSystem.TryGetComponentData<ObjectGeometryData>(m_Prefab, out var component);
			if (m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(m_Prefab, out var component2))
			{
				if ((component2.m_Flags & Game.Objects.PlacementFlags.HasUndergroundElements) != Game.Objects.PlacementFlags.None)
				{
					base.requireNet |= Layer.Road;
				}
				if ((component2.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) != Game.Objects.PlacementFlags.None)
				{
					base.requireNet |= Layer.Waterway;
				}
			}
			switch (mode)
			{
			case Mode.Upgrade:
				if (m_PrefabSystem.HasComponent<ServiceUpgradeData>(m_Prefab))
				{
					m_UpgradingObject = GetUpgradable(m_ToolSystem.selected);
				}
				break;
			case Mode.Move:
				if (!base.EntityManager.Exists(m_MovingObject))
				{
					m_MovingObject = Entity.Null;
				}
				if (m_MovingInitialized != m_MovingObject)
				{
					m_MovingInitialized = m_MovingObject;
					InitializeRotation(m_MovingObject, component2);
				}
				break;
			}
			if ((ToolBaseSystem.GetActualSnap(selectedSnap, m_SnapOnMask, m_SnapOffMask) & (Snap.NetArea | Snap.NetNode | Snap.ObjectSurface)) != Snap.None && !m_PrefabSystem.HasComponent<BuildingData>(m_Prefab) && component2.m_SubReplacementType != SubReplacementType.Tree)
			{
				allowUnderground = true;
			}
			if (m_PrefabSystem.TryGetComponentData<TransportStopData>(m_Prefab, out var component3))
			{
				base.requireNetArrows = component3.m_TransportType != TransportType.Post;
				base.requireStops = component3.m_TransportType;
			}
			base.requireUnderground = allowUnderground && underground;
			base.requireZones = !base.requireUnderground && ((component2.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None || ((component.m_Flags & Game.Objects.GeometryFlags.OccupyZone) != Game.Objects.GeometryFlags.None && base.requireStops == TransportType.None));
			if (m_State != State.Default && !base.applyAction.enabled && !base.secondaryApplyAction.enabled)
			{
				m_State = State.Default;
			}
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				if (m_IncorrectApply.WasPressedThisFrame())
				{
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
					return Update(inputDeps, fullUpdate: false);
				}
				if (isUpgradeMode)
				{
					switch (m_State)
					{
					case State.Default:
						if (m_ApplyBlocked)
						{
							if (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
							{
								m_ApplyBlocked = false;
							}
							return Update(inputDeps, fullUpdate: false);
						}
						if (base.applyAction.WasPressedThisFrame())
						{
							return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
						}
						if (base.secondaryApplyAction.WasPressedThisFrame())
						{
							return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
						}
						break;
					case State.Adding:
						if (base.cancelAction.WasPressedThisFrame())
						{
							m_ApplyBlocked = true;
							m_State = State.Default;
							return Update(inputDeps, fullUpdate: true);
						}
						if (base.applyAction.WasReleasedThisFrame())
						{
							return Apply(inputDeps);
						}
						break;
					case State.Removing:
						if (base.cancelAction.WasPressedThisFrame())
						{
							m_ApplyBlocked = true;
							m_State = State.Default;
							return Update(inputDeps, fullUpdate: true);
						}
						if (base.secondaryApplyAction.WasReleasedThisFrame())
						{
							return Cancel(inputDeps);
						}
						break;
					}
					return Update(inputDeps, fullUpdate: false);
				}
				if (base.cancelAction.WasPressedThisFrame())
				{
					if (mode == Mode.Upgrade && (m_SnapOnMask & ~m_SnapOffMask & Snap.OwnerSide) != Snap.None)
					{
						m_ToolSystem.activeTool = m_DefaultToolSystem;
					}
					return Cancel(inputDeps, base.cancelAction.WasReleasedThisFrame());
				}
				if (m_State == State.Adding || m_State == State.Removing)
				{
					if (base.applyAction.WasPressedThisFrame() || base.applyAction.WasReleasedThisFrame())
					{
						return Apply(inputDeps);
					}
					if (flag2 || base.secondaryApplyAction.WasPressedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
					{
						return Cancel(inputDeps);
					}
					return Update(inputDeps, fullUpdate: false);
				}
				if (m_State == State.Rotating && base.secondaryApplyAction.WasReleasedThisFrame())
				{
					if (m_RotationModified)
					{
						m_RotationModified = false;
					}
					else
					{
						Rotate(MathF.PI / 4f, fromStart: false, align: true);
					}
					m_State = State.Default;
					return Update(inputDeps, fullUpdate: false);
				}
				if ((mode == Mode.Curve || mode == Mode.Line) && m_State == State.Default && base.secondaryApplyAction.WasPressedThisFrame())
				{
					if (m_ControlPoints.Length <= 1)
					{
						return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
					}
					Rotate(MathF.PI / 4f, fromStart: false, align: true);
					for (int i = 0; i < m_ControlPoints.Length; i++)
					{
						ControlPoint value = m_ControlPoints[i];
						value.m_Rotation = m_Rotation.Value.m_Rotation;
						m_ControlPoints[i] = value;
					}
					return Update(inputDeps, fullUpdate: true);
				}
				if ((mode != Mode.Upgrade || (m_SnapOnMask & ~m_SnapOffMask & Snap.OwnerSide) == 0) && base.secondaryApplyAction.WasPressedThisFrame())
				{
					return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
				}
				if (base.applyAction.WasPressedThisFrame())
				{
					JobHandle result = Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
					if (base.applyMode == ApplyMode.Apply && mode == Mode.Move)
					{
						if (m_ToolSystem.activeTool == this)
						{
							m_ToolSystem.activeTool = m_DefaultToolSystem;
						}
						m_TerrainSystem.OnBuildingMoved(m_MovingObject);
						return result;
					}
					if (base.applyMode == ApplyMode.Apply && mode == Mode.Upgrade && ForbidMultipleUpgrades() && m_ToolSystem.activeTool == this)
					{
						m_ToolSystem.activeTool = m_DefaultToolSystem;
					}
					return result;
				}
				if (m_PreciseRotation.IsInProgress())
				{
					if (m_State == State.Default)
					{
						float num = m_PreciseRotation.ReadValue<float>();
						float angle = MathF.PI / 2f * num * UnityEngine.Time.deltaTime;
						Rotate(angle, fromStart: false, align: false);
						for (int j = 0; j < m_ControlPoints.Length; j++)
						{
							ControlPoint value2 = m_ControlPoints[j];
							value2.m_Rotation = m_Rotation.Value.m_Rotation;
							m_ControlPoints[j] = value2;
						}
					}
					return Update(inputDeps, fullUpdate: true);
				}
				if (m_State == State.Rotating && InputManager.instance.activeControlScheme == InputManager.ControlScheme.KeyboardAndMouse)
				{
					float3 @float = InputManager.instance.mousePosition;
					if (@float.x != m_RotationStartPosition.x)
					{
						float angle2 = (@float.x - m_RotationStartPosition.x) * (MathF.PI * 2f) * 0.002f;
						Rotate(angle2, fromStart: true, align: false);
						m_RotationModified = true;
					}
					return Update(inputDeps, fullUpdate: false);
				}
				return Update(inputDeps, fullUpdate: false);
			}
		}
		else
		{
			base.requireUnderground = false;
			base.requireZones = false;
			base.requireNetArrows = false;
			base.requireNet = Layer.None;
			UpdateInfoview(Entity.Null);
		}
		if (m_State == State.Adding && (base.applyAction.WasReleasedThisFrame() || base.cancelAction.WasPressedThisFrame()))
		{
			m_State = State.Default;
		}
		else if (m_State == State.Removing && (base.secondaryApplyAction.WasReleasedThisFrame() || base.cancelAction.WasPressedThisFrame()))
		{
			m_State = State.Default;
		}
		else if (m_State != State.Default && (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame()))
		{
			m_State = State.Default;
		}
		return Clear(inputDeps);
	}

	private static int GetMaxControlPointCount(Mode mode)
	{
		return mode switch
		{
			Mode.Brush => 0, 
			Mode.Line => 2, 
			Mode.Curve => 3, 
			_ => 1, 
		};
	}

	private bool ForbidMultipleUpgrades()
	{
		if (m_Prefab != null)
		{
			if (m_PrefabSystem.HasComponent<BuildingExtensionData>(m_Prefab))
			{
				return true;
			}
			if (m_PrefabSystem.TryGetComponentData<ServiceUpgradeData>(m_Prefab, out var component) && component.m_ForbidMultiple)
			{
				return true;
			}
		}
		return false;
	}

	public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
	{
		m_Prefab = GetObjectPrefab();
		if (m_Prefab != null)
		{
			bool flag = m_PrefabSystem.HasComponent<BuildingData>(m_Prefab);
			bool isAssetStamp = !flag && m_PrefabSystem.HasComponent<AssetStampData>(m_Prefab);
			m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(m_Prefab, out var component);
			GetAvailableSnapMask(component, m_ToolSystem.actionMode.IsEditor(), flag, isAssetStamp, actualMode, out onMask, out offMask);
		}
		else
		{
			base.GetAvailableSnapMask(out onMask, out offMask);
		}
	}

	private static void GetAvailableSnapMask(PlaceableObjectData prefabPlaceableData, bool editorMode, bool isBuilding, bool isAssetStamp, Mode mode, out Snap onMask, out Snap offMask)
	{
		onMask = Snap.Upright;
		offMask = Snap.None;
		if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadSide | Game.Objects.PlacementFlags.OwnerSide)) == Game.Objects.PlacementFlags.OwnerSide)
		{
			onMask |= Snap.OwnerSide;
		}
		else if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadSide | Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating | Game.Objects.PlacementFlags.Hovering)) != Game.Objects.PlacementFlags.None)
		{
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.OwnerSide) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.OwnerSide;
				offMask |= Snap.OwnerSide;
			}
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadSide) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.NetSide;
				offMask |= Snap.NetSide;
			}
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadEdge) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.NetArea;
				offMask |= Snap.NetArea;
			}
			if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.RoadEdge)) == Game.Objects.PlacementFlags.Shoreline)
			{
				onMask |= Snap.Shoreline;
				offMask |= Snap.Shoreline;
			}
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.ObjectSurface;
				offMask |= Snap.ObjectSurface;
			}
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.SubNetSnap) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.ExistingGeometry;
				offMask |= Snap.ExistingGeometry;
			}
		}
		else if ((prefabPlaceableData.m_Flags & (Game.Objects.PlacementFlags.RoadNode | Game.Objects.PlacementFlags.RoadEdge)) != Game.Objects.PlacementFlags.None)
		{
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadNode) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.NetNode;
			}
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.RoadEdge) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.NetArea;
			}
		}
		else
		{
			if (prefabPlaceableData.m_SubReplacementType != SubReplacementType.None && mode != Mode.Move)
			{
				onMask |= Snap.NetArea;
				offMask |= Snap.NetArea;
			}
			if (editorMode && !isBuilding)
			{
				onMask |= Snap.ObjectSurface;
				offMask |= Snap.ObjectSurface;
				offMask |= Snap.Upright;
			}
			if ((prefabPlaceableData.m_Flags & Game.Objects.PlacementFlags.SubNetSnap) != Game.Objects.PlacementFlags.None)
			{
				onMask |= Snap.ExistingGeometry;
				offMask |= Snap.ExistingGeometry;
			}
		}
		if (editorMode && (!isAssetStamp || mode == Mode.Stamp))
		{
			onMask |= Snap.AutoParent;
			offMask |= Snap.AutoParent;
		}
		if (mode == Mode.Line || mode == Mode.Curve)
		{
			onMask |= Snap.Distance;
			offMask |= Snap.Distance;
		}
		if (mode == Mode.Curve || (editorMode && mode == Mode.Line))
		{
			onMask |= Snap.StraightDirection;
			offMask |= Snap.StraightDirection;
		}
		if (mode == Mode.Brush)
		{
			onMask &= Snap.Upright;
			offMask &= Snap.Upright;
			onMask |= Snap.PrefabType;
			offMask |= Snap.PrefabType;
		}
		if (isBuilding || isAssetStamp)
		{
			onMask |= Snap.ContourLines;
			offMask |= Snap.ContourLines;
		}
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		return inputDeps;
	}

	private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		if (actualMode == Mode.Brush)
		{
			if (m_State == State.Default)
			{
				base.applyMode = ApplyMode.Clear;
				Randomize();
				m_StartPoint = m_LastRaycastPoint;
				m_State = State.Removing;
				m_ForceCancel = singleFrameOnly;
				GetRaycastResult(out m_LastRaycastPoint);
				return UpdateDefinitions(inputDeps);
			}
			if (m_State == State.Removing && GetAllowApply())
			{
				base.applyMode = ApplyMode.Apply;
				Randomize();
				m_StartPoint = default(ControlPoint);
				m_State = State.Default;
				GetRaycastResult(out m_LastRaycastPoint);
				return UpdateDefinitions(inputDeps);
			}
			base.applyMode = ApplyMode.Clear;
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_LastRaycastPoint);
			return UpdateDefinitions(inputDeps);
		}
		if (m_State != State.Removing && m_UpgradeStates.Length >= 1)
		{
			m_State = State.Removing;
			m_ForceCancel = singleFrameOnly;
			m_ForceUpdate = true;
			m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
			return Update(inputDeps, fullUpdate: true);
		}
		if (m_State == State.Removing)
		{
			m_State = State.Default;
			if (GetAllowApply())
			{
				SetAppliedUpgrade(removing: true);
				base.applyMode = ApplyMode.Apply;
				m_RandomSeed = RandomSeed.Next();
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				if (GetRaycastResult(out var controlPoint))
				{
					m_ControlPoints.Add(in controlPoint);
					inputDeps = SnapControlPoint(inputDeps);
					inputDeps = FixNetControlPoints(inputDeps);
					inputDeps = UpdateDefinitions(inputDeps);
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
				}
				else
				{
					inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
				}
			}
			else
			{
				base.applyMode = ApplyMode.Clear;
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				if (GetRaycastResult(out var controlPoint2))
				{
					m_ControlPoints.Add(in controlPoint2);
					inputDeps = SnapControlPoint(inputDeps);
					inputDeps = UpdateDefinitions(inputDeps);
				}
				else
				{
					inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
				}
			}
			return inputDeps;
		}
		if ((actualMode != Mode.Upgrade || (m_SnapOnMask & ~m_SnapOffMask & Snap.OwnerSide) == 0) && m_ControlPoints.Length <= 1)
		{
			if (singleFrameOnly)
			{
				Rotate(MathF.PI / 4f, fromStart: false, align: true);
			}
			else
			{
				m_State = State.Rotating;
				m_RotationStartPosition = InputManager.instance.mousePosition;
				m_StartRotation = m_Rotation.Value.m_Rotation;
				m_StartCameraAngle = cameraAngle;
			}
		}
		base.applyMode = ApplyMode.Clear;
		if (m_ControlPoints.Length > 0)
		{
			m_ControlPoints.RemoveAt(m_ControlPoints.Length - 1);
		}
		m_UpgradeStates.Clear();
		m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
		if (GetRaycastResult(out var controlPoint3))
		{
			controlPoint3.m_Rotation = m_Rotation.Value.m_Rotation;
			if (m_ControlPoints.Length > 0)
			{
				m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint3;
			}
			else
			{
				m_ControlPoints.Add(in controlPoint3);
			}
			inputDeps = SnapControlPoint(inputDeps);
			inputDeps = UpdateDefinitions(inputDeps);
		}
		return inputDeps;
	}

	private JobHandle FixNetControlPoints(JobHandle inputDeps)
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_TempQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new NetToolSystem.FixControlPointsJob
		{
			m_Chunks = chunks,
			m_Mode = NetToolSystem.Mode.Replace,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControlPoints = m_ControlPoints
		}, JobHandle.CombineDependencies(inputDeps, outJobHandle));
		chunks.Dispose(jobHandle);
		return jobHandle;
	}

	private void Rotate(float angle, bool fromStart, bool align)
	{
		m_PrefabSystem.TryGetComponentData<PlaceableObjectData>(m_Prefab, out var component);
		Rotation value = m_Rotation.Value;
		bool flag = (component.m_Flags & Game.Objects.PlacementFlags.Wall) != 0;
		value.m_Rotation = math.mul(fromStart ? m_StartRotation : value.m_Rotation, flag ? quaternion.RotateZ(angle) : quaternion.RotateY(angle));
		value.m_Rotation = math.normalizesafe(value.m_Rotation, quaternion.identity);
		if (align)
		{
			quaternion parentRotation = value.m_ParentRotation;
			if ((actualMode == Mode.Line || actualMode == Mode.Curve) && m_UpgradeStates.Length == 0 && m_ControlPoints.Length >= 2)
			{
				float2 value2 = m_ControlPoints[1].m_Position.xz - m_ControlPoints[0].m_Position.xz;
				if (MathUtils.TryNormalize(ref value2))
				{
					parentRotation = quaternion.LookRotation(new float3(value2.x, 0f, value2.y), math.up());
				}
			}
			SnapJob.AlignRotation(ref value.m_Rotation, parentRotation, flag);
		}
		value.m_IsAligned = align;
		m_Rotation.Value = value;
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		if (actualMode == Mode.Brush)
		{
			bool allowApply = GetAllowApply();
			if (m_State == State.Default)
			{
				base.applyMode = (allowApply ? ApplyMode.Apply : ApplyMode.Clear);
				Randomize();
				if (!singleFrameOnly)
				{
					m_StartPoint = m_LastRaycastPoint;
					m_State = State.Adding;
				}
				GetRaycastResult(out m_LastRaycastPoint);
				return UpdateDefinitions(inputDeps);
			}
			if (m_State == State.Adding && allowApply)
			{
				base.applyMode = ApplyMode.Apply;
				Randomize();
				m_StartPoint = default(ControlPoint);
				m_State = State.Default;
				GetRaycastResult(out m_LastRaycastPoint);
				return UpdateDefinitions(inputDeps);
			}
			base.applyMode = ApplyMode.Clear;
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_LastRaycastPoint);
			return UpdateDefinitions(inputDeps);
		}
		if (m_State != State.Adding && m_UpgradeStates.Length >= 1 && !singleFrameOnly)
		{
			m_State = State.Adding;
			m_ForceUpdate = true;
			m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
			return Update(inputDeps, fullUpdate: true);
		}
		if (m_State == State.Adding)
		{
			m_State = State.Default;
			if (GetAllowApply())
			{
				SetAppliedUpgrade(removing: false);
				base.applyMode = ApplyMode.Apply;
				m_RandomSeed = RandomSeed.Next();
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetBuildSound);
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				if (GetRaycastResult(out var controlPoint))
				{
					m_ControlPoints.Add(in controlPoint);
					inputDeps = SnapControlPoint(inputDeps);
					inputDeps = FixNetControlPoints(inputDeps);
					inputDeps = UpdateDefinitions(inputDeps);
				}
				else
				{
					inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
				}
			}
			else
			{
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				m_ForceUpdate = true;
				inputDeps = Update(inputDeps, fullUpdate: true);
			}
			return inputDeps;
		}
		if (m_ControlPoints.Length < GetMaxControlPointCount(actualMode))
		{
			base.applyMode = ApplyMode.Clear;
			if (GetRaycastResult(out var controlPoint2))
			{
				if (m_ControlPoints.Length <= 1)
				{
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetStartSound);
				}
				else
				{
					m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_NetNodeSound);
				}
				controlPoint2.m_Rotation = m_Rotation.Value.m_Rotation;
				m_ControlPoints.Add(in controlPoint2);
				inputDeps = SnapControlPoint(inputDeps);
				inputDeps = UpdateDefinitions(inputDeps);
			}
			else
			{
				inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
			}
		}
		else if (GetAllowApply())
		{
			base.applyMode = ApplyMode.Apply;
			Randomize();
			if (m_Prefab is BuildingPrefab || m_Prefab is AssetStampPrefab)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingSound);
			}
			else if (m_Prefab is StaticObjectPrefab || m_ToolSystem.actionMode.IsEditor())
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlacePropSound);
			}
			m_ControlPoints.Clear();
			m_UpgradeStates.Clear();
			m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
			if (m_ToolSystem.actionMode.IsGame() && !m_LotQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<Entity> nativeArray = m_LotQuery.ToEntityArray(Allocator.TempJob);
				try
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						Entity entity = nativeArray[i];
						Area componentData = base.EntityManager.GetComponentData<Area>(entity);
						Temp componentData2 = base.EntityManager.GetComponentData<Temp>(entity);
						if ((componentData.m_Flags & AreaFlags.Slave) == 0 && (componentData2.m_Flags & TempFlags.Create) != 0)
						{
							LotPrefab lotPrefab = m_PrefabSystem.GetPrefab<LotPrefab>(base.EntityManager.GetComponentData<PrefabRef>(entity));
							if (!lotPrefab.m_AllowOverlap)
							{
								m_AreaToolSystem.recreate = entity;
								m_AreaToolSystem.prefab = lotPrefab;
								m_AreaToolSystem.mode = AreaToolSystem.Mode.Edit;
								m_ToolSystem.activeTool = m_AreaToolSystem;
								return inputDeps;
							}
						}
					}
				}
				finally
				{
					nativeArray.Dispose();
				}
			}
			if (GetRaycastResult(out var controlPoint3))
			{
				if (m_ToolSystem.actionMode.IsGame())
				{
					Telemetry.PlaceBuilding(m_UpgradingObject, m_Prefab, controlPoint3.m_Position);
				}
				controlPoint3.m_Rotation = m_Rotation.Value.m_Rotation;
				m_ControlPoints.Add(in controlPoint3);
				inputDeps = SnapControlPoint(inputDeps);
				inputDeps = UpdateDefinitions(inputDeps);
			}
		}
		else
		{
			m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
			inputDeps = Update(inputDeps, fullUpdate: false);
		}
		return inputDeps;
	}

	private JobHandle Update(JobHandle inputDeps, bool fullUpdate)
	{
		if (actualMode == Mode.Brush)
		{
			if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
			{
				if (m_State != State.Default)
				{
					base.applyMode = (GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
					Randomize();
					m_StartPoint = m_LastRaycastPoint;
					m_LastRaycastPoint = controlPoint;
					return UpdateDefinitions(inputDeps);
				}
				if (m_LastRaycastPoint.Equals(controlPoint) && !forceUpdate)
				{
					if (HaveBrushSettingsChanged())
					{
						base.applyMode = ApplyMode.Clear;
						return UpdateDefinitions(inputDeps);
					}
					base.applyMode = ApplyMode.None;
					return inputDeps;
				}
				base.applyMode = ApplyMode.Clear;
				m_StartPoint = controlPoint;
				m_LastRaycastPoint = controlPoint;
				return UpdateDefinitions(inputDeps);
			}
			if (m_LastRaycastPoint.Equals(default(ControlPoint)) && !forceUpdate)
			{
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			if (m_State != State.Default)
			{
				base.applyMode = (GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
				Randomize();
				m_StartPoint = m_LastRaycastPoint;
				m_LastRaycastPoint = default(ControlPoint);
			}
			else
			{
				base.applyMode = ApplyMode.Clear;
				m_StartPoint = default(ControlPoint);
				m_LastRaycastPoint = default(ControlPoint);
			}
			return UpdateDefinitions(inputDeps);
		}
		if (GetRaycastResult(out ControlPoint controlPoint2, out bool forceUpdate2))
		{
			bool flag = false;
			if (m_Rotation.Value.m_IsSnapped && !m_ToolSystem.actionMode.IsEditor())
			{
				flag = !m_LastRaycastPoint.Equals(controlPoint2);
				m_LastRaycastPoint = controlPoint2;
				controlPoint2.m_Rotation = m_Rotation.Value.m_Rotation;
			}
			else
			{
				controlPoint2.m_Rotation = m_Rotation.Value.m_Rotation;
				flag = !m_LastRaycastPoint.Equals(controlPoint2);
				m_LastRaycastPoint = controlPoint2;
			}
			forceUpdate2 = forceUpdate2 || fullUpdate;
			base.applyMode = ApplyMode.None;
			if (flag || forceUpdate2)
			{
				ControlPoint controlPoint3 = default(ControlPoint);
				if (m_ControlPoints.Length != 0)
				{
					controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 1];
				}
				if (m_State == State.Adding || m_State == State.Removing)
				{
					if (m_ControlPoints.Length == 1)
					{
						m_ControlPoints.Add(in controlPoint2);
					}
					else
					{
						m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint2;
					}
				}
				else
				{
					if (m_UpgradeStates.Length != 0)
					{
						m_ControlPoints.Clear();
						m_UpgradeStates.Clear();
					}
					if (m_ControlPoints.Length == 0)
					{
						m_ControlPoints.Add(in controlPoint2);
					}
					else
					{
						m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint2;
					}
				}
				inputDeps = SnapControlPoint(inputDeps);
				JobHandle.ScheduleBatchedJobs();
				if (!forceUpdate2)
				{
					inputDeps.Complete();
					ControlPoint other = m_ControlPoints[m_ControlPoints.Length - 1];
					forceUpdate2 = !controlPoint3.EqualsIgnoreHit(other);
				}
				if (forceUpdate2)
				{
					base.applyMode = ApplyMode.Clear;
					inputDeps = UpdateDefinitions(inputDeps);
				}
			}
		}
		else
		{
			base.applyMode = ApplyMode.Clear;
			m_LastRaycastPoint = default(ControlPoint);
			if (m_State == State.Default)
			{
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
			}
			inputDeps = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		}
		return inputDeps;
	}

	private bool HaveBrushSettingsChanged()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_VisibleQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			ComponentTypeHandle<Brush> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Brush_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				NativeArray<Brush> nativeArray2 = nativeArray[i].GetNativeArray(ref typeHandle);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (!nativeArray2[j].m_Size.Equals(base.brushSize))
					{
						return true;
					}
				}
			}
			return false;
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void SetAppliedUpgrade(bool removing)
	{
		m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
		if (m_UpgradeStates.Length < 1 || m_ControlPoints.Length < 4)
		{
			return;
		}
		Entity originalEntity = m_ControlPoints[m_ControlPoints.Length - 3].m_OriginalEntity;
		Entity originalEntity2 = m_ControlPoints[m_ControlPoints.Length - 2].m_OriginalEntity;
		NetToolSystem.UpgradeState upgradeState = m_UpgradeStates[m_UpgradeStates.Length - 1];
		NetToolSystem.AppliedUpgrade value = new NetToolSystem.AppliedUpgrade
		{
			m_SubReplacementPrefab = upgradeState.m_SubReplacementPrefab,
			m_Flags = (removing ? upgradeState.m_RemoveFlags : upgradeState.m_AddFlags),
			m_SubReplacementType = upgradeState.m_SubReplacementType,
			m_SubReplacementSide = upgradeState.m_SubReplacementSide
		};
		if (originalEntity == originalEntity2)
		{
			value.m_Entity = originalEntity;
			m_AppliedUpgrade.Value = value;
		}
		else
		{
			if (!base.EntityManager.TryGetBuffer(originalEntity, isReadOnly: true, out DynamicBuffer<ConnectedEdge> buffer))
			{
				return;
			}
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity edge = buffer[i].m_Edge;
				if (base.EntityManager.TryGetComponent<Edge>(edge, out var component) && ((component.m_Start == originalEntity && component.m_End == originalEntity2) || (component.m_End == originalEntity && component.m_Start == originalEntity2)))
				{
					value.m_Entity = edge;
					m_AppliedUpgrade.Value = value;
				}
			}
		}
	}

	private Entity GetUpgradable(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<Attached>(entity, out var component))
		{
			return component.m_Parent;
		}
		return entity;
	}

	private JobHandle SnapControlPoint(JobHandle inputDeps)
	{
		Entity selected = ((actualMode == Mode.Move) ? m_MovingObject : GetUpgradable(m_ToolSystem.selected));
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle deps;
		JobHandle jobHandle = IJobExtensions.Schedule(new SnapJob
		{
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_RemoveUpgrade = (m_State == State.Removing),
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_Distance = math.max(1f, distance),
			m_DistanceScale = distanceScale,
			m_Snap = GetActualSnap(),
			m_Mode = actualMode,
			m_Prefab = m_PrefabSystem.GetEntity(m_Prefab),
			m_Selected = selected,
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Terrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_District_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadComposition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MovingObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AssetStampData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabCompositionAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabAuxiliaryNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AuxiliaryNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabFixedNetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_FixedNetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
			m_ZoneSearchTree = m_ZoneSearchSystem.GetSearchTree(readOnly: true, out dependencies3),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_ControlPoints = m_ControlPoints,
			m_SubSnapPoints = m_SubSnapPoints,
			m_UpgradeStates = m_UpgradeStates,
			m_Rotation = m_Rotation,
			m_AppliedUpgrade = m_AppliedUpgrade
		}, JobUtils.CombineDependencies(inputDeps, dependencies, dependencies2, dependencies3, deps));
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_ZoneSearchSystem.AddSearchTreeReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		return jobHandle;
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (m_Prefab != null)
		{
			Snap actualSnap = GetActualSnap();
			Entity entity = m_PrefabSystem.GetEntity(m_Prefab);
			if (actualMode != Mode.Brush && (actualSnap & Snap.NetArea) != Snap.None)
			{
				if (m_State == State.Adding || m_State == State.Removing)
				{
					return JobHandle.CombineDependencies(jobHandle, UpdateSubReplacementDefinitions(inputDeps));
				}
				if (base.EntityManager.TryGetComponent<PlaceableObjectData>(entity, out var component) && component.m_SubReplacementType != SubReplacementType.None)
				{
					inputDeps.Complete();
					if (m_UpgradeStates.Length != 0)
					{
						return JobHandle.CombineDependencies(jobHandle, UpdateSubReplacementDefinitions(default(JobHandle)));
					}
				}
			}
			Entity laneContainer = Entity.Null;
			Entity transformPrefab = Entity.Null;
			Entity brushPrefab = Entity.Null;
			float deltaTime = UnityEngine.Time.deltaTime;
			float num = 0f;
			if (m_ToolSystem.actionMode.IsEditor())
			{
				GetContainers(m_ContainerQuery, out laneContainer, out var _);
			}
			if (m_TransformPrefab != null)
			{
				transformPrefab = m_PrefabSystem.GetEntity(m_TransformPrefab);
			}
			if (actualMode == Mode.Brush && base.brushType != null)
			{
				brushPrefab = m_PrefabSystem.GetEntity(base.brushType);
				EnsureCachedBrushData();
				ControlPoint value = m_StartPoint;
				ControlPoint value2 = m_LastRaycastPoint;
				value.m_OriginalEntity = Entity.Null;
				value2.m_OriginalEntity = Entity.Null;
				m_ControlPoints.Clear();
				m_UpgradeStates.Clear();
				m_AppliedUpgrade.Value = default(NetToolSystem.AppliedUpgrade);
				m_ControlPoints.Add(in value);
				m_ControlPoints.Add(in value2);
				if (m_State == State.Default)
				{
					deltaTime = 0.1f;
				}
			}
			if (actualMode == Mode.Line || actualMode == Mode.Curve)
			{
				num = math.max(1f, distance) * distanceScale;
			}
			NativeReference<AttachmentData> attachmentPrefab = default(NativeReference<AttachmentData>);
			if (!m_ToolSystem.actionMode.IsEditor() && base.EntityManager.TryGetComponent<PlaceholderBuildingData>(entity, out var component2))
			{
				ZoneData componentData = base.EntityManager.GetComponentData<ZoneData>(component2.m_ZonePrefab);
				BuildingData componentData2 = base.EntityManager.GetComponentData<BuildingData>(entity);
				m_BuildingQuery.ResetFilter();
				m_BuildingQuery.SetSharedComponentFilter(new BuildingSpawnGroupData(componentData.m_ZoneType));
				attachmentPrefab = new NativeReference<AttachmentData>(Allocator.TempJob);
				JobHandle outJobHandle;
				NativeList<ArchetypeChunk> chunks = m_BuildingQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
				inputDeps = IJobExtensions.Schedule(new FindAttachmentBuildingJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_BuildingDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_SpawnableBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_BuildingData = componentData2,
					m_RandomSeed = m_RandomSeed,
					m_Chunks = chunks,
					m_AttachmentPrefab = attachmentPrefab
				}, JobHandle.CombineDependencies(inputDeps, outJobHandle));
				chunks.Dispose(inputDeps);
			}
			jobHandle = JobHandle.CombineDependencies(jobHandle, CreateDefinitions(entity, transformPrefab, brushPrefab, m_UpgradingObject, m_MovingObject, laneContainer, m_CityConfigurationSystem.defaultTheme, m_ControlPoints, attachmentPrefab, m_ToolSystem.actionMode.IsEditor(), m_CityConfigurationSystem.leftHandTraffic, m_State == State.Removing, actualMode == Mode.Stamp, base.brushSize, math.radians(base.brushAngle), base.brushStrength, num, deltaTime, m_RandomSeed, actualSnap, actualAgeMask, inputDeps));
			if (attachmentPrefab.IsCreated)
			{
				attachmentPrefab.Dispose(jobHandle);
			}
		}
		return jobHandle;
	}

	private JobHandle UpdateSubReplacementDefinitions(JobHandle inputDeps)
	{
		JobHandle deps;
		JobHandle jobHandle = IJobExtensions.Schedule(new NetToolSystem.CreateDefinitionsJob
		{
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_RemoveUpgrade = (m_State == State.Removing),
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_Mode = NetToolSystem.Mode.Replace,
			m_RandomSeed = m_RandomSeed,
			m_AgeMask = actualAgeMask,
			m_ControlPoints = m_ControlPoints,
			m_UpgradeStates = m_UpgradeStates,
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_CachedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetPrefab = m_PrefabSystem.GetEntity(m_Prefab),
			m_WaterSurfaceData = m_WaterSystem.GetVelocitiesSurfaceData(out deps),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(inputDeps, deps));
		m_WaterSystem.AddVelocitySurfaceReader(jobHandle);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		return jobHandle;
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
	public ObjectToolSystem()
	{
	}
}
