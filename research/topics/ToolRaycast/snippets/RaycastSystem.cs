#define UNITY_ASSERTIONS
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Common;

[CompilerGenerated]
public class RaycastSystem : GameSystemBase
{
	public struct EntityResult
	{
		public Entity m_Entity;

		public int m_RaycastIndex;
	}

	[BurstCompile]
	private struct FindEntitiesFromTreeJob : IJobParallelFor
	{
		private struct FindEntitiesIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Line3.Segment m_Line;

			public float3 m_MinOffset;

			public float3 m_MaxOffset;

			public float m_MinY;

			public NativeQueue<EntityResult>.ParallelWriter m_EntityQueue;

			public int m_RaycastIndex;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				bounds.m_Bounds.min += m_MinOffset;
				bounds.m_Bounds.max += m_MaxOffset;
				bounds.m_Bounds.min.y = math.select(bounds.m_Bounds.min.y, m_MinY, (m_MinY < bounds.m_Bounds.min.y) & ((bounds.m_Mask & BoundsMask.HasLot) != 0));
				float2 t;
				return MathUtils.Intersect(bounds.m_Bounds, m_Line, out t);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				bounds.m_Bounds.min += m_MinOffset;
				bounds.m_Bounds.max += m_MaxOffset;
				bounds.m_Bounds.min.y = math.select(bounds.m_Bounds.min.y, m_MinY, (m_MinY < bounds.m_Bounds.min.y) & ((bounds.m_Mask & BoundsMask.HasLot) != 0));
				if (MathUtils.Intersect(bounds.m_Bounds, m_Line, out var _))
				{
					m_EntityQueue.Enqueue(new EntityResult
					{
						m_Entity = entity,
						m_RaycastIndex = m_RaycastIndex
					});
				}
			}
		}

		[ReadOnly]
		public float m_LaneExpandFovTan;

		[ReadOnly]
		public TypeMask m_TypeMask;

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public NativeQueue<EntityResult>.ParallelWriter m_EntityQueue;

		public void Execute(int index)
		{
			RaycastInput raycastInput = m_Input[index];
			if ((raycastInput.m_TypeMask & m_TypeMask) != TypeMask.None)
			{
				float minLaneRadius = Game.Net.RaycastJobs.GetMinLaneRadius(m_LaneExpandFovTan, MathUtils.Length(raycastInput.m_Line));
				FindEntitiesIterator iterator = new FindEntitiesIterator
				{
					m_Line = raycastInput.m_Line,
					m_MinOffset = math.min(-raycastInput.m_Offset, 0f - minLaneRadius),
					m_MaxOffset = math.max(-raycastInput.m_Offset, minLaneRadius),
					m_MinY = math.select(float.MaxValue, MathUtils.Min(raycastInput.m_Line.y), (raycastInput.m_Flags & RaycastFlags.BuildingLots) != 0),
					m_EntityQueue = m_EntityQueue,
					m_RaycastIndex = index
				};
				m_SearchTree.Iterate(ref iterator);
			}
		}
	}

	[BurstCompile]
	private struct DequeEntitiesJob : IJob
	{
		public NativeQueue<EntityResult> m_EntityQueue;

		public NativeList<EntityResult> m_EntityList;

		public void Execute()
		{
			m_EntityList.ResizeUninitialized(m_EntityQueue.Count);
			for (int i = 0; i < m_EntityList.Length; i++)
			{
				m_EntityList[i] = m_EntityQueue.Dequeue();
			}
		}
	}

	[BurstCompile]
	private struct RaycastTerrainJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public TerrainHeightData m_TerrainData;

		[ReadOnly]
		public WaterSurfacesData m_WaterData;

		[ReadOnly]
		public Entity m_TerrainEntity;

		[NativeDisableContainerSafetyRestriction]
		public NativeArray<RaycastResult> m_TerrainResults;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			int num = index / m_Input.Length;
			int index2 = index - num * m_Input.Length;
			RaycastInput raycastInput = m_Input[index2];
			RaycastResult value = default(RaycastResult);
			Line3.Segment segment = raycastInput.m_Line + raycastInput.m_Offset;
			bool outside = (raycastInput.m_Flags & RaycastFlags.Outside) != 0;
			Bounds3 hitBounds;
			switch (num)
			{
			case 0:
			{
				if (((raycastInput.m_TypeMask & (TypeMask.Terrain | TypeMask.Zones | TypeMask.Areas | TypeMask.WaterSources)) != TypeMask.None || (raycastInput.m_Flags & RaycastFlags.BuildingLots) != 0) && TerrainUtils.Raycast(ref m_TerrainData, segment, outside, out var t2, out var normal, out hitBounds))
				{
					value.m_Owner = m_TerrainEntity;
					value.m_Hit.m_HitEntity = value.m_Owner;
					value.m_Hit.m_Position = MathUtils.Position(segment, t2);
					value.m_Hit.m_HitPosition = value.m_Hit.m_Position;
					value.m_Hit.m_HitDirection = normal;
					value.m_Hit.m_NormalizedDistance = t2 + 1f / math.max(1f, MathUtils.Length(segment));
					if ((raycastInput.m_TypeMask & TypeMask.Terrain) != TypeMask.None)
					{
						m_Results.Accumulate(index2, value);
					}
				}
				break;
			}
			case 1:
			{
				if ((raycastInput.m_TypeMask & (TypeMask.Areas | TypeMask.Water)) != TypeMask.None && WaterUtils.Raycast(ref m_WaterData, ref m_TerrainData, segment, outside, out var t, out hitBounds))
				{
					value.m_Owner = m_TerrainEntity;
					value.m_Hit.m_HitEntity = value.m_Owner;
					value.m_Hit.m_Position = MathUtils.Position(segment, t);
					value.m_Hit.m_HitPosition = value.m_Hit.m_Position;
					value.m_Hit.m_NormalizedDistance = t + 1f / math.max(1f, MathUtils.Length(segment));
					if ((raycastInput.m_TypeMask & TypeMask.Water) != TypeMask.None)
					{
						m_Results.Accumulate(index2, value);
					}
				}
				break;
			}
			}
			if (m_TerrainResults.IsCreated)
			{
				m_TerrainResults[index] = value;
			}
		}
	}

	[BurstCompile]
	private struct RaycastWaterSourcesJob : IJobChunk
	{
		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_WaterSourceDataType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public float3 m_PositionOffset;

		[ReadOnly]
		public NativeArray<RaycastResult> m_TerrainResults;

		[ReadOnly]
		public bool m_useLegacySource;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Simulation.WaterSourceData> nativeArray2 = chunk.GetNativeArray(ref m_WaterSourceDataType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			for (int i = 0; i < m_Input.Length; i++)
			{
				RaycastInput input = m_Input[i];
				if ((input.m_TypeMask & TypeMask.WaterSources) == 0)
				{
					continue;
				}
				Line3.Segment line = input.m_Line + input.m_Offset;
				RaycastResult raycastResult = default(RaycastResult);
				if (m_TerrainResults.Length != 0)
				{
					raycastResult = m_TerrainResults[i];
				}
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity = nativeArray[j];
					Game.Simulation.WaterSourceData waterSourceData = nativeArray2[j];
					Game.Objects.Transform transform = nativeArray3[j];
					float3 position;
					if (m_useLegacySource)
					{
						transform.m_Position.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, transform.m_Position);
						position = transform.m_Position;
						if (waterSourceData.m_ConstantDepth > 0)
						{
							position.y = m_PositionOffset.y + waterSourceData.m_Height;
						}
						else
						{
							position.y += waterSourceData.m_Height;
						}
					}
					else
					{
						position = transform.m_Position;
						position.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, transform.m_Position);
						position.y += m_PositionOffset.y + waterSourceData.m_Height;
					}
					if (MathUtils.Intersect(line.y, position.y, out var t))
					{
						CheckHit(i, input, entity, waterSourceData.m_Radius, transform.m_Position, MathUtils.Position(line, t));
					}
					if (raycastResult.m_Owner != Entity.Null && raycastResult.m_Hit.m_HitPosition.y > position.y)
					{
						CheckHit(i, input, entity, waterSourceData.m_Radius, transform.m_Position, raycastResult.m_Hit.m_HitPosition);
					}
				}
			}
		}

		private void CheckHit(int raycastIndex, RaycastInput input, Entity entity, float radius, float3 position, float3 hitPosition)
		{
			float num = math.distance(hitPosition.xz, position.xz);
			if (num < radius)
			{
				RaycastResult value = new RaycastResult
				{
					m_Owner = entity,
					m_Hit = 
					{
						m_HitEntity = entity,
						m_Position = position,
						m_HitPosition = hitPosition,
						m_HitDirection = math.up(),
						m_NormalizedDistance = (radius + num) * math.max(1f, math.distance(hitPosition, input.m_Line.a + input.m_Offset))
					}
				};
				m_Results.Accumulate(raycastIndex, value);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct RaycastResultJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeAccumulator<RaycastResult> m_Accumulator;

		[NativeDisableParallelForRestriction]
		public NativeList<RaycastResult> m_Result;

		public void Execute(int index)
		{
			m_Result[index] = m_Accumulator.GetResult(index);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> __Game_Prefabs_QuantityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SharedMeshData> __Game_Prefabs_SharedMeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Bone> __Game_Rendering_Bone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LodMesh> __Game_Prefabs_LodMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshVertex> __Game_Prefabs_MeshVertex_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshIndex> __Game_Prefabs_MeshIndex_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshNode> __Game_Prefabs_MeshNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Placeholder> __Game_Objects_Placeholder_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.NetObject> __Game_Objects_NetObject_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stack> __Game_Objects_Stack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Secondary> __Game_Objects_Secondary_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> __Game_Prefabs_GrowthScaleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Overridden> __Game_Common_Overridden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ImpostorData> __Game_Prefabs_ImpostorData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Space> __Game_Areas_Space_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Segment> __Game_Routes_Segment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HiddenRoute> __Game_Routes_HiddenRoute_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CurveElement> __Game_Routes_CurveElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> __Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Geometry> __Game_Areas_Geometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.LabelExtents> __Game_Areas_LabelExtents_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Aggregated> __Game_Net_Aggregated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.LabelExtents> __Game_Net_LabelExtents_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LabelPosition> __Game_Net_LabelPosition_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Icon> __Game_Notifications_Icon_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Static> __Game_Objects_Static_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Object> __Game_Objects_Object_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> __Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_QuantityObjectData_RO_ComponentLookup = state.GetComponentLookup<QuantityObjectData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_SharedMeshData_RO_ComponentLookup = state.GetComponentLookup<SharedMeshData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferLookup = state.GetBufferLookup<Passenger>(isReadOnly: true);
			__Game_Rendering_Skeleton_RO_BufferLookup = state.GetBufferLookup<Skeleton>(isReadOnly: true);
			__Game_Rendering_Bone_RO_BufferLookup = state.GetBufferLookup<Bone>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_LodMesh_RO_BufferLookup = state.GetBufferLookup<LodMesh>(isReadOnly: true);
			__Game_Prefabs_MeshVertex_RO_BufferLookup = state.GetBufferLookup<MeshVertex>(isReadOnly: true);
			__Game_Prefabs_MeshIndex_RO_BufferLookup = state.GetBufferLookup<MeshIndex>(isReadOnly: true);
			__Game_Prefabs_MeshNode_RO_BufferLookup = state.GetBufferLookup<MeshNode>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Placeholder_RO_ComponentLookup = state.GetComponentLookup<Placeholder>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_NetObject_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.NetObject>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentLookup = state.GetComponentLookup<Stack>(isReadOnly: true);
			__Game_Objects_Secondary_RO_ComponentLookup = state.GetComponentLookup<Secondary>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true);
			__Game_Prefabs_GrowthScaleData_RO_ComponentLookup = state.GetComponentLookup<GrowthScaleData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Overridden_RO_ComponentLookup = state.GetComponentLookup<Overridden>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_ImpostorData_RO_ComponentLookup = state.GetComponentLookup<ImpostorData>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
			__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
			__Game_Areas_Space_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Space>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Segment>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_HiddenRoute_RO_ComponentLookup = state.GetComponentLookup<HiddenRoute>(isReadOnly: true);
			__Game_Routes_CurveElement_RO_BufferLookup = state.GetBufferLookup<CurveElement>(isReadOnly: true);
			__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetLaneGeometryData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_Geometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Geometry>(isReadOnly: true);
			__Game_Areas_LabelExtents_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.LabelExtents>(isReadOnly: true);
			__Game_Net_Aggregated_RO_ComponentLookup = state.GetComponentLookup<Aggregated>(isReadOnly: true);
			__Game_Net_LabelExtents_RO_ComponentLookup = state.GetComponentLookup<Game.Net.LabelExtents>(isReadOnly: true);
			__Game_Net_LabelPosition_RO_BufferLookup = state.GetBufferLookup<LabelPosition>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Icon>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Objects_Static_RO_ComponentLookup = state.GetComponentLookup<Static>(isReadOnly: true);
			__Game_Objects_Object_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Object>(isReadOnly: true);
			__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup = state.GetComponentLookup<NotificationIconDisplayData>(isReadOnly: true);
			__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
		}
	}

	private EntityQuery m_TerrainQuery;

	private EntityQuery m_LabelQuery;

	private EntityQuery m_IconQuery;

	private EntityQuery m_WaterSourceQuery;

	private Game.Zones.SearchSystem m_ZoneSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Objects.SearchSystem m_ObjectsSearchSystem;

	private Game.Routes.SearchSystem m_RouteSearchSystem;

	private IconClusterSystem m_IconClusterSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ToolSystem m_ToolSystem;

	private PreCullingSystem m_PreCullingSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private UpdateSystem m_UpdateSystem;

	private List<object> m_InputContext;

	private List<object> m_ResultContext;

	private NativeList<RaycastInput> m_Input;

	private NativeList<RaycastResult> m_Result;

	private JobHandle m_Dependencies;

	private bool m_Updating;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ObjectsSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_RouteSearchSystem = base.World.GetOrCreateSystemManaged<Game.Routes.SearchSystem>();
		m_IconClusterSystem = base.World.GetOrCreateSystemManaged<IconClusterSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_TerrainQuery = GetEntityQuery(ComponentType.ReadOnly<Terrain>(), ComponentType.Exclude<Temp>());
		m_LabelQuery = GetEntityQuery(ComponentType.ReadOnly<District>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_IconQuery = GetEntityQuery(ComponentType.ReadOnly<Icon>(), ComponentType.ReadOnly<DisallowCluster>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_WaterSourceQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(), ComponentType.Exclude<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		base.EntityManager.CreateEntity(typeof(Terrain));
		m_InputContext = new List<object>(1);
		m_ResultContext = new List<object>(1);
		m_Input = new NativeList<RaycastInput>(1, Allocator.Persistent);
		m_Result = new NativeList<RaycastResult>(1, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Dependencies.Complete();
		m_Input.Dispose();
		m_Result.Dispose();
		base.OnDestroy();
	}

	public void AddInput(object context, RaycastInput input)
	{
		if (input.IsDisabled())
		{
			input.m_TypeMask = TypeMask.None;
		}
		CompleteRaycast();
		m_InputContext.Add(context);
		m_Input.Add(in input);
	}

	public NativeArray<RaycastResult> GetResult(object context)
	{
		CompleteRaycast();
		int num = -1;
		for (int i = 0; i < m_ResultContext.Count; i++)
		{
			if (m_ResultContext[i] == context)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			return default(NativeArray<RaycastResult>);
		}
		int num2 = 1;
		for (int j = num + 1; j < m_ResultContext.Count && m_ResultContext[j] == context; j++)
		{
			num2++;
		}
		return m_Result.AsArray().GetSubArray(num, num2);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_UpdateSystem.Update(SystemUpdatePhase.Raycast);
		CompleteRaycast();
		m_ResultContext.Clear();
		m_ResultContext.AddRange(m_InputContext);
		m_Result.ResizeUninitialized(m_Input.Length);
		NativeAccumulator<RaycastResult> accumulator = new NativeAccumulator<RaycastResult>(m_Input.Length, Allocator.TempJob);
		m_Dependencies = PerformRaycast(accumulator);
		base.Dependency = m_Dependencies;
		RaycastResultJob jobData = new RaycastResultJob
		{
			m_Accumulator = accumulator,
			m_Result = m_Result
		};
		m_Dependencies = IJobParallelForExtensions.Schedule(jobData, m_Input.Length, 1, m_Dependencies);
		accumulator.Dispose(m_Dependencies);
		m_Updating = true;
	}

	private void CompleteRaycast()
	{
		if (m_Updating)
		{
			m_Updating = false;
			m_Dependencies.Complete();
			m_InputContext.Clear();
			m_Input.Clear();
		}
	}

	private JobHandle PerformRaycast(NativeAccumulator<RaycastResult> accumulator)
	{
		Camera main = Camera.main;
		if (main == null)
		{
			return default(JobHandle);
		}
		TypeMask typeMask = TypeMask.None;
		RaycastFlags raycastFlags = (RaycastFlags)0u;
		for (int i = 0; i < m_Input.Length; i++)
		{
			RaycastInput raycastInput = m_Input[i];
			typeMask |= raycastInput.m_TypeMask;
			raycastFlags |= raycastInput.m_Flags;
		}
		if (typeMask == TypeMask.None)
		{
			return default(JobHandle);
		}
		int num = 2;
		float num2 = math.tan(math.radians(main.fieldOfView) * 0.5f);
		NativeArray<RaycastInput> input = m_Input.AsArray();
		NativeArray<RaycastResult> terrainResults = default(NativeArray<RaycastResult>);
		JobHandle jobHandle = default(JobHandle);
		JobHandle jobHandle2 = default(JobHandle);
		JobHandle jobHandle3 = default(JobHandle);
		JobHandle jobHandle4 = default(JobHandle);
		JobHandle dependencies = default(JobHandle);
		NativeList<EntityResult> nativeList = default(NativeList<EntityResult>);
		NativeList<EntityResult> nativeList2 = default(NativeList<EntityResult>);
		NativeList<PreCullingData> cullingData = default(NativeList<PreCullingData>);
		TerrainHeightData terrainHeightData = default(TerrainHeightData);
		if ((typeMask & (TypeMask.Zones | TypeMask.Areas | TypeMask.WaterSources)) != TypeMask.None || (raycastFlags & RaycastFlags.BuildingLots) != 0)
		{
			terrainResults = new NativeArray<RaycastResult>(num * m_Input.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		}
		if ((typeMask & (TypeMask.Terrain | TypeMask.Zones | TypeMask.Areas | TypeMask.Water | TypeMask.WaterSources)) != TypeMask.None || (raycastFlags & RaycastFlags.BuildingLots) != 0)
		{
			terrainHeightData = m_TerrainSystem.GetHeightData();
			JobHandle deps;
			RaycastTerrainJob jobData = new RaycastTerrainJob
			{
				m_Input = input,
				m_TerrainData = terrainHeightData,
				m_WaterData = m_WaterSystem.GetSurfacesData(out deps),
				m_TerrainEntity = m_TerrainQuery.GetSingletonEntity(),
				m_TerrainResults = terrainResults,
				m_Results = accumulator.AsParallelWriter()
			};
			int2 @int = jobData.m_TerrainData.resolution.xz / jobData.m_WaterData.depths.resolution.xz;
			Assert.AreEqual(jobData.m_TerrainData.resolution.xz, jobData.m_WaterData.depths.resolution.xz * @int);
			if (jobData.m_TerrainData.isCreated && jobData.m_WaterData.depths.isCreated)
			{
				jobHandle2 = IJobParallelForExtensions.Schedule(jobData, num * input.Length, 1, JobHandle.CombineDependencies(base.Dependency, deps));
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
				m_TerrainSystem.AddCPUHeightReader(jobHandle2);
				m_WaterSystem.AddSurfaceReader(jobHandle2);
			}
		}
		if ((typeMask & (TypeMask.MovingObjects | TypeMask.Net | TypeMask.Labels)) != TypeMask.None)
		{
			NativeQueue<EntityResult> entityQueue = new NativeQueue<EntityResult>(Allocator.TempJob);
			nativeList = new NativeList<EntityResult>(Allocator.TempJob);
			JobHandle dependencies2;
			FindEntitiesFromTreeJob jobData2 = new FindEntitiesFromTreeJob
			{
				m_TypeMask = (TypeMask.MovingObjects | TypeMask.Net | TypeMask.Labels),
				m_Input = input,
				m_SearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
				m_EntityQueue = entityQueue.AsParallelWriter()
			};
			DequeEntitiesJob jobData3 = new DequeEntitiesJob
			{
				m_EntityQueue = entityQueue,
				m_EntityList = nativeList
			};
			JobHandle jobHandle5 = IJobParallelForExtensions.Schedule(jobData2, input.Length, 1, JobHandle.CombineDependencies(base.Dependency, dependencies2));
			jobHandle3 = IJobExtensions.Schedule(jobData3, jobHandle5);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle5);
			entityQueue.Dispose(jobHandle3);
		}
		if ((typeMask & (TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Net)) != TypeMask.None)
		{
			NativeQueue<EntityResult> entityQueue2 = new NativeQueue<EntityResult>(Allocator.TempJob);
			nativeList2 = new NativeList<EntityResult>(Allocator.TempJob);
			cullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies);
			JobHandle dependencies3;
			FindEntitiesFromTreeJob jobData4 = new FindEntitiesFromTreeJob
			{
				m_TypeMask = (TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Net),
				m_Input = input,
				m_SearchTree = m_ObjectsSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
				m_EntityQueue = entityQueue2.AsParallelWriter()
			};
			DequeEntitiesJob jobData5 = new DequeEntitiesJob
			{
				m_EntityQueue = entityQueue2,
				m_EntityList = nativeList2
			};
			JobHandle jobHandle6 = IJobParallelForExtensions.Schedule(jobData4, input.Length, 1, JobHandle.CombineDependencies(base.Dependency, dependencies3));
			jobHandle4 = IJobExtensions.Schedule(jobData5, jobHandle6);
			m_ObjectsSearchSystem.AddStaticSearchTreeReader(jobHandle6);
			entityQueue2.Dispose(jobHandle4);
		}
		if ((typeMask & TypeMask.MovingObjects) != TypeMask.None)
		{
			NativeQueue<EntityResult> entityQueue3 = new NativeQueue<EntityResult>(Allocator.TempJob);
			NativeList<EntityResult> nativeList3 = new NativeList<EntityResult>(Allocator.TempJob);
			NativeArray<int4> ranges = new NativeArray<int4>(input.Length, Allocator.TempJob);
			JobHandle dependencies4;
			FindEntitiesFromTreeJob jobData6 = new FindEntitiesFromTreeJob
			{
				m_TypeMask = TypeMask.MovingObjects,
				m_Input = input,
				m_SearchTree = m_ObjectsSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies4),
				m_EntityQueue = entityQueue3.AsParallelWriter()
			};
			Game.Objects.RaycastJobs.GetSourceRangesJob jobData7 = new Game.Objects.RaycastJobs.GetSourceRangesJob
			{
				m_EdgeList = nativeList,
				m_StaticObjectList = nativeList2,
				m_Ranges = ranges
			};
			Game.Objects.RaycastJobs.ExtractLaneObjectsJob jobData8 = new Game.Objects.RaycastJobs.ExtractLaneObjectsJob
			{
				m_Input = input,
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_EdgeList = nativeList,
				m_StaticObjectList = nativeList2,
				m_Ranges = ranges,
				m_MovingObjectQueue = entityQueue3.AsParallelWriter()
			};
			DequeEntitiesJob jobData9 = new DequeEntitiesJob
			{
				m_EntityQueue = entityQueue3,
				m_EntityList = nativeList3
			};
			Game.Objects.RaycastJobs.RaycastMovingObjectsJob jobData10 = new Game.Objects.RaycastJobs.RaycastMovingObjectsJob
			{
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_Input = input,
				m_ObjectList = nativeList3.AsDeferredJobArray(),
				m_CullingData = cullingData,
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
				m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabQuantityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_QuantityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSharedMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SharedMeshData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_Passengers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferLookup, ref base.CheckedStateRef),
				m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, ref base.CheckedStateRef),
				m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RO_BufferLookup, ref base.CheckedStateRef),
				m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_Meshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_Lods = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LodMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_Vertices = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshVertex_RO_BufferLookup, ref base.CheckedStateRef),
				m_Indices = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshIndex_RO_BufferLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			JobHandle jobHandle7 = IJobParallelForExtensions.Schedule(jobData6, input.Length, 1, JobHandle.CombineDependencies(base.Dependency, dependencies4));
			JobHandle jobHandle8 = IJobParallelForExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(IJobExtensions.Schedule(jobData7, JobHandle.CombineDependencies(jobHandle3, jobHandle4)), jobHandle7), jobData: jobData8, arrayLength: input.Length, innerloopBatchCount: 1);
			JobHandle jobHandle9 = IJobExtensions.Schedule(jobData9, jobHandle8);
			JobHandle jobHandle10 = jobData10.Schedule(nativeList3, 1, JobHandle.CombineDependencies(jobHandle9, dependencies));
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle10);
			m_ObjectsSearchSystem.AddMovingSearchTreeReader(jobHandle7);
			m_PreCullingSystem.AddCullingDataReader(jobHandle10);
			entityQueue3.Dispose(jobHandle9);
			nativeList3.Dispose(jobHandle10);
			ranges.Dispose(jobHandle8);
		}
		if ((typeMask & (TypeMask.StaticObjects | TypeMask.Net)) != TypeMask.None)
		{
			Game.Objects.RaycastJobs.RaycastStaticObjectsJob jobData11 = new Game.Objects.RaycastJobs.RaycastStaticObjectsJob
			{
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_Input = input,
				m_Objects = nativeList2.AsDeferredJobArray(),
				m_CullingData = cullingData,
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_NetObject_RO_ComponentLookup, ref base.CheckedStateRef),
				m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SecondaryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Secondary_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGrowthScaleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GrowthScaleData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabQuantityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_QuantityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OverriddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LotAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, ref base.CheckedStateRef),
				m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RO_BufferLookup, ref base.CheckedStateRef),
				m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabImpostorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ImpostorData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSharedMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SharedMeshData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Meshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_Lods = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LodMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_Vertices = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshVertex_RO_BufferLookup, ref base.CheckedStateRef),
				m_Indices = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshIndex_RO_BufferLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			JobHandle dependsOn;
			if ((raycastFlags & RaycastFlags.BuildingLots) != 0)
			{
				dependsOn = JobHandle.CombineDependencies(jobHandle4, dependencies, jobHandle2);
				jobData11.m_TerrainResults = terrainResults;
			}
			else
			{
				dependsOn = JobHandle.CombineDependencies(jobHandle4, dependencies);
			}
			JobHandle jobHandle11 = jobData11.Schedule(nativeList2, 1, dependsOn);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle11);
			m_PreCullingSystem.AddCullingDataReader(jobHandle11);
		}
		if ((typeMask & TypeMask.Net) != TypeMask.None)
		{
			Game.Net.RaycastJobs.RaycastEdgesJob jobData12 = new Game.Net.RaycastJobs.RaycastEdgesJob
			{
				m_FovTan = num2,
				m_Input = input,
				m_Edges = nativeList.AsDeferredJobArray(),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobData12.Schedule(nativeList, 1, jobHandle3));
		}
		if ((typeMask & TypeMask.Zones) != TypeMask.None)
		{
			JobHandle dependencies5;
			JobHandle jobHandle12 = IJobParallelForExtensions.Schedule(new Game.Zones.RaycastJobs.FindZoneBlockJob
			{
				m_Input = input,
				m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Cells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_Cell_RO_BufferLookup, ref base.CheckedStateRef),
				m_SearchTree = m_ZoneSearchSystem.GetSearchTree(readOnly: true, out dependencies5),
				m_TerrainResults = terrainResults,
				m_Results = accumulator.AsParallelWriter()
			}, num * input.Length, 1, JobHandle.CombineDependencies(jobHandle2, dependencies5));
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle12);
			m_ZoneSearchSystem.AddSearchTreeReader(jobHandle12);
		}
		if ((typeMask & TypeMask.Areas) != TypeMask.None)
		{
			JobHandle dependencies6;
			JobHandle jobHandle13 = IJobParallelForExtensions.Schedule(new Game.Areas.RaycastJobs.FindAreaJob
			{
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_Input = input,
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Space_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_SearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies6),
				m_TerrainResults = terrainResults,
				m_Results = accumulator.AsParallelWriter()
			}, (num + 1) * input.Length, 1, JobHandle.CombineDependencies(jobHandle2, dependencies6));
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle13);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle13);
		}
		if ((typeMask & (TypeMask.RouteWaypoints | TypeMask.RouteSegments)) != TypeMask.None)
		{
			NativeList<Game.Routes.RaycastJobs.RouteItem> nativeList4 = new NativeList<Game.Routes.RaycastJobs.RouteItem>(Allocator.TempJob);
			JobHandle dependencies7;
			Game.Routes.RaycastJobs.FindRoutesFromTreeJob jobData13 = new Game.Routes.RaycastJobs.FindRoutesFromTreeJob
			{
				m_Input = input,
				m_SearchTree = m_RouteSearchSystem.GetSearchTree(readOnly: true, out dependencies7),
				m_RouteList = nativeList4
			};
			Game.Routes.RaycastJobs.RaycastRoutesJob jobData14 = new Game.Routes.RaycastJobs.RaycastRoutesJob
			{
				m_Input = input,
				m_Routes = nativeList4.AsDeferredJobArray(),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaypointData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SegmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_HiddenRoute_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_CurveElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			JobHandle jobHandle14 = IJobExtensions.Schedule(jobData13, JobHandle.CombineDependencies(base.Dependency, dependencies7));
			JobHandle jobHandle15 = jobData14.Schedule(nativeList4, 1, jobHandle14);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle15);
			nativeList4.Dispose(jobHandle15);
			m_RouteSearchSystem.AddSearchTreeReader(jobHandle14);
		}
		if ((typeMask & TypeMask.Lanes) != TypeMask.None)
		{
			NativeQueue<EntityResult> entityQueue4 = new NativeQueue<EntityResult>(Allocator.TempJob);
			NativeList<EntityResult> nativeList5 = new NativeList<EntityResult>(Allocator.TempJob);
			JobHandle dependencies8;
			FindEntitiesFromTreeJob jobData15 = new FindEntitiesFromTreeJob
			{
				m_LaneExpandFovTan = num2,
				m_TypeMask = TypeMask.Lanes,
				m_Input = input,
				m_SearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out dependencies8),
				m_EntityQueue = entityQueue4.AsParallelWriter()
			};
			DequeEntitiesJob jobData16 = new DequeEntitiesJob
			{
				m_EntityQueue = entityQueue4,
				m_EntityList = nativeList5
			};
			Game.Net.RaycastJobs.RaycastLanesJob jobData17 = new Game.Net.RaycastJobs.RaycastLanesJob
			{
				m_FovTan = num2,
				m_Input = input,
				m_Lanes = nativeList5.AsDeferredJobArray(),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabUtilityLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLaneGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			JobHandle jobHandle16 = IJobParallelForExtensions.Schedule(jobData15, input.Length, 1, JobHandle.CombineDependencies(base.Dependency, dependencies8));
			JobHandle jobHandle17 = IJobExtensions.Schedule(jobData16, jobHandle16);
			JobHandle jobHandle18 = jobData17.Schedule(nativeList5, 1, jobHandle17);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle18);
			m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle16);
			entityQueue4.Dispose(jobHandle17);
			nativeList5.Dispose(jobHandle18);
		}
		if ((typeMask & TypeMask.Labels) != TypeMask.None)
		{
			Game.Areas.RaycastJobs.RaycastLabelsJob jobData18 = new Game.Areas.RaycastJobs.RaycastLabelsJob
			{
				m_Input = input,
				m_CameraRight = main.transform.right,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_GeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LabelExtentsType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_LabelExtents_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			Game.Net.RaycastJobs.RaycastLabelsJob jobData19 = new Game.Net.RaycastJobs.RaycastLabelsJob
			{
				m_Input = input,
				m_CameraRight = main.transform.right,
				m_Edges = nativeList.AsDeferredJobArray(),
				m_AggregatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Aggregated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LabelExtentsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LabelExtents_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LabelPositions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LabelPosition_RO_BufferLookup, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			jobHandle = JobHandle.CombineDependencies(jobHandle, JobChunkExtensions.ScheduleParallel(jobData18, m_LabelQuery, base.Dependency));
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobData19.Schedule(nativeList, 1, jobHandle3));
		}
		if ((typeMask & (TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Icons)) != TypeMask.None)
		{
			JobHandle dependencies9;
			JobHandle outJobHandle;
			Game.Notifications.RaycastJobs.RaycastIconsJob jobData20 = new Game.Notifications.RaycastJobs.RaycastIconsJob
			{
				m_Input = input,
				m_CameraUp = main.transform.up,
				m_CameraRight = main.transform.right,
				m_ClusterData = m_IconClusterSystem.GetIconClusterData(readOnly: true, out dependencies9),
				m_IconChunks = m_IconQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_IconType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StaticData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Static_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Object_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceholderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IconDisplayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Results = accumulator.AsParallelWriter()
			};
			JobHandle jobHandle19 = IJobParallelForExtensions.Schedule(jobData20, input.Length, 1, JobHandle.CombineDependencies(base.Dependency, dependencies9, outJobHandle));
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle19);
			jobData20.m_IconChunks.Dispose(jobHandle19);
			m_IconClusterSystem.AddIconClusterReader(jobHandle19);
		}
		if ((typeMask & TypeMask.WaterSources) != TypeMask.None)
		{
			JobHandle jobHandle20 = JobChunkExtensions.ScheduleParallel(new RaycastWaterSourcesJob
			{
				m_Input = input,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_WaterSourceDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TerrainHeightData = terrainHeightData,
				m_PositionOffset = m_TerrainSystem.positionOffset,
				m_useLegacySource = m_WaterSystem.UseLegacyWaterSources,
				m_TerrainResults = terrainResults,
				m_Results = accumulator.AsParallelWriter()
			}, m_WaterSourceQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle2));
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle20);
			m_TerrainSystem.AddCPUHeightReader(jobHandle20);
		}
		if ((typeMask & (TypeMask.Zones | TypeMask.Areas | TypeMask.WaterSources)) != TypeMask.None || (raycastFlags & RaycastFlags.BuildingLots) != 0)
		{
			terrainResults.Dispose(jobHandle);
		}
		if ((typeMask & (TypeMask.MovingObjects | TypeMask.Net | TypeMask.Labels)) != TypeMask.None)
		{
			nativeList.Dispose(jobHandle);
		}
		if ((typeMask & (TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Net)) != TypeMask.None)
		{
			nativeList2.Dispose(jobHandle);
		}
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
	public RaycastSystem()
	{
	}
}
