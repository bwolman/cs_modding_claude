using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Input;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class DefaultToolSystem : ToolBaseSystem
{
	private enum State
	{
		Default,
		MouseDownPrepare,
		MouseDown,
		Dragging
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public float3 m_Position;

		[ReadOnly]
		public bool m_SetPosition;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_LotData;

		[ReadOnly]
		public ComponentLookup<Position> m_RoutePositionData;

		[ReadOnly]
		public ComponentLookup<Connected> m_RouteConnectedData;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<AggregateElement> m_AggregateElements;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Entity entity = m_Entity;
			OwnerDefinition ownerDefinition = default(OwnerDefinition);
			bool isParent = false;
			bool attachParentCreated = false;
			Attached componentData4;
			Attachment componentData5;
			if (m_ServiceUpgradeData.HasComponent(m_Entity) && m_OwnerData.TryGetComponent(m_Entity, out var componentData) && m_TransformData.TryGetComponent(componentData.m_Owner, out var componentData2))
			{
				entity = componentData.m_Owner;
				isParent = true;
				AddEntity(entity, Entity.Null, default(OwnerDefinition), isParent: true, attachParentCreated: false);
				if (m_AttachmentData.TryGetComponent(entity, out var componentData3) && m_TransformData.HasComponent(componentData3.m_Attached))
				{
					AddEntity(componentData3.m_Attached, Entity.Null, default(OwnerDefinition), isParent: true, attachParentCreated: true);
				}
				ownerDefinition = new OwnerDefinition
				{
					m_Prefab = m_PrefabRefData[entity].m_Prefab,
					m_Position = componentData2.m_Position,
					m_Rotation = componentData2.m_Rotation
				};
			}
			else if (m_AttachedData.TryGetComponent(m_Entity, out componentData4) && m_AttachmentData.TryGetComponent(componentData4.m_Parent, out componentData5) && componentData5.m_Attached == m_Entity)
			{
				entity = componentData4.m_Parent;
				attachParentCreated = true;
				AddEntity(entity, Entity.Null, default(OwnerDefinition), isParent: false, attachParentCreated: false);
			}
			AddEntity(m_Entity, Entity.Null, ownerDefinition, isParent: false, attachParentCreated);
			if (!m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			componentData2 = m_TransformData[entity];
			ownerDefinition = new OwnerDefinition
			{
				m_Prefab = m_PrefabRefData[entity].m_Prefab,
				m_Position = componentData2.m_Position,
				m_Rotation = componentData2.m_Rotation
			};
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity entity2 = bufferData[i];
				if (entity2 != m_Entity)
				{
					AddEntity(entity2, Entity.Null, ownerDefinition, isParent, attachParentCreated: false);
				}
			}
		}

		private void AddEntity(Entity entity, Entity owner, OwnerDefinition ownerDefinition, bool isParent, bool attachParentCreated)
		{
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Original = entity
			};
			if (isParent)
			{
				component.m_Flags |= CreationFlags.Parent | CreationFlags.Duplicate;
			}
			else
			{
				component.m_Flags |= CreationFlags.Select;
			}
			m_CommandBuffer.AddComponent(e, default(Updated));
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
			}
			if (m_EdgeData.HasComponent(entity))
			{
				if (m_EditorContainerData.HasComponent(entity))
				{
					component.m_SubPrefab = m_EditorContainerData[entity].m_Prefab;
				}
				Edge edge = m_EdgeData[entity];
				NetCourse component2 = default(NetCourse);
				component2.m_Curve = m_CurveData[entity].m_Bezier;
				component2.m_Length = MathUtils.Length(component2.m_Curve);
				component2.m_FixedIndex = -1;
				component2.m_StartPosition.m_Entity = edge.m_Start;
				component2.m_StartPosition.m_Position = component2.m_Curve.a;
				component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve));
				component2.m_StartPosition.m_CourseDelta = 0f;
				component2.m_EndPosition.m_Entity = edge.m_End;
				component2.m_EndPosition.m_Position = component2.m_Curve.d;
				component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve));
				component2.m_EndPosition.m_CourseDelta = 1f;
				m_CommandBuffer.AddComponent(e, component2);
			}
			else if (m_NodeData.HasComponent(entity))
			{
				if (m_EditorContainerData.HasComponent(entity))
				{
					component.m_SubPrefab = m_EditorContainerData[entity].m_Prefab;
				}
				Game.Net.Node node = m_NodeData[entity];
				NetCourse component3 = new NetCourse
				{
					m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position),
					m_Length = 0f,
					m_FixedIndex = -1,
					m_StartPosition = 
					{
						m_Entity = entity,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 0f
					},
					m_EndPosition = 
					{
						m_Entity = entity,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 1f
					}
				};
				m_CommandBuffer.AddComponent(e, component3);
			}
			else if (m_TransformData.HasComponent(entity))
			{
				Transform transform = m_TransformData[entity];
				if (m_SetPosition)
				{
					transform.m_Position = m_Position;
					component.m_Flags |= CreationFlags.Dragging;
				}
				ObjectDefinition component4 = new ObjectDefinition
				{
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation
				};
				if (m_ElevationData.TryGetComponent(entity, out var componentData))
				{
					component4.m_Elevation = componentData.m_Elevation;
					component4.m_ParentMesh = ObjectUtils.GetSubParentMesh(componentData.m_Flags);
				}
				else
				{
					component4.m_ParentMesh = -1;
				}
				Entity entity2 = entity;
				if (m_AttachedData.HasComponent(entity))
				{
					component.m_Attached = m_AttachedData[entity].m_Parent;
					component.m_Flags |= CreationFlags.Attach;
					if (m_AttachmentData.TryGetComponent(component.m_Attached, out var componentData2) && componentData2.m_Attached == entity)
					{
						entity2 = component.m_Attached;
					}
					if (attachParentCreated && m_PrefabRefData.TryGetComponent(component.m_Attached, out var componentData3))
					{
						component.m_Attached = componentData3.m_Prefab;
					}
				}
				component4.m_Probability = 100;
				component4.m_PrefabSubIndex = -1;
				if (m_LocalTransformCacheData.HasComponent(entity))
				{
					LocalTransformCache localTransformCache = m_LocalTransformCacheData[entity];
					component4.m_LocalPosition = localTransformCache.m_Position;
					component4.m_LocalRotation = localTransformCache.m_Rotation;
					component4.m_ParentMesh = localTransformCache.m_ParentMesh;
					component4.m_GroupIndex = localTransformCache.m_GroupIndex;
					component4.m_Probability = localTransformCache.m_Probability;
					component4.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
				}
				else if (ownerDefinition.m_Prefab != Entity.Null)
				{
					Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(new Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation)), transform);
					component4.m_LocalPosition = transform2.m_Position;
					component4.m_LocalRotation = transform2.m_Rotation;
				}
				else if (m_TransformData.HasComponent(owner))
				{
					Transform transform3 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(m_TransformData[owner]), transform);
					component4.m_LocalPosition = transform3.m_Position;
					component4.m_LocalRotation = transform3.m_Rotation;
				}
				else
				{
					component4.m_LocalPosition = transform.m_Position;
					component4.m_LocalRotation = transform.m_Rotation;
				}
				if (m_EditorContainerData.HasComponent(entity))
				{
					EditorContainer editorContainer = m_EditorContainerData[entity];
					component.m_SubPrefab = editorContainer.m_Prefab;
					component4.m_Scale = editorContainer.m_Scale;
					component4.m_Intensity = editorContainer.m_Intensity;
					component4.m_GroupIndex = editorContainer.m_GroupIndex;
				}
				m_CommandBuffer.AddComponent(e, component4);
				if (m_SubAreas.TryGetBuffer(entity2, out var bufferData))
				{
					OwnerDefinition ownerDefinition2 = new OwnerDefinition
					{
						m_Prefab = m_PrefabRefData[entity].m_Prefab,
						m_Position = transform.m_Position,
						m_Rotation = transform.m_Rotation
					};
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity area = bufferData[i].m_Area;
						if (m_LotData.HasComponent(area))
						{
							AddEntity(area, Entity.Null, ownerDefinition2, isParent, attachParentCreated: false);
						}
					}
				}
			}
			else if (m_AreaNodes.HasBuffer(entity))
			{
				DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_AreaNodes[entity];
				DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
				dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
				dynamicBuffer2.CopyFrom(dynamicBuffer.AsNativeArray());
			}
			else if (m_RouteWaypoints.HasBuffer(entity))
			{
				DynamicBuffer<RouteWaypoint> dynamicBuffer3 = m_RouteWaypoints[entity];
				DynamicBuffer<WaypointDefinition> dynamicBuffer4 = m_CommandBuffer.AddBuffer<WaypointDefinition>(e);
				dynamicBuffer4.ResizeUninitialized(dynamicBuffer3.Length);
				for (int j = 0; j < dynamicBuffer3.Length; j++)
				{
					RouteWaypoint routeWaypoint = dynamicBuffer3[j];
					WaypointDefinition value = new WaypointDefinition
					{
						m_Position = m_RoutePositionData[routeWaypoint.m_Waypoint].m_Position,
						m_Original = routeWaypoint.m_Waypoint
					};
					if (m_RouteConnectedData.HasComponent(routeWaypoint.m_Waypoint))
					{
						value.m_Connection = m_RouteConnectedData[routeWaypoint.m_Waypoint].m_Connected;
					}
					dynamicBuffer4[j] = value;
				}
			}
			else if (m_IconData.HasComponent(entity))
			{
				Icon icon = m_IconData[entity];
				m_CommandBuffer.AddComponent(e, new IconDefinition(icon));
			}
			else if (m_AggregateElements.HasBuffer(entity))
			{
				DynamicBuffer<AggregateElement> dynamicBuffer5 = m_AggregateElements[entity];
				DynamicBuffer<AggregateElement> dynamicBuffer6 = m_CommandBuffer.AddBuffer<AggregateElement>(e);
				dynamicBuffer6.ResizeUninitialized(dynamicBuffer5.Length);
				dynamicBuffer6.CopyFrom(dynamicBuffer5.AsNativeArray());
			}
			m_CommandBuffer.AddComponent(e, component);
		}
	}

	public struct SelectEntityJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Attachment> m_AttachmentType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<Debug> m_DebugData;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public bool m_DebugSelect;

		public NativeReference<Entity> m_Selected;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			Entity entity = Entity.Null;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Owner> nativeArray = archetypeChunk.GetNativeArray(ref m_OwnerType);
				NativeArray<Temp> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TempType);
				NativeArray<Attachment> nativeArray3 = archetypeChunk.GetNativeArray(ref m_AttachmentType);
				NativeArray<Controller> nativeArray4 = archetypeChunk.GetNativeArray(ref m_ControllerType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (((!CollectionUtils.TryGet(nativeArray, j, out var value) && (!CollectionUtils.TryGet(nativeArray4, j, out var value2) || !m_OwnerData.TryGetComponent(value2.m_Controller, out value))) || !m_TempData.TryGetComponent(value.m_Owner, out var componentData) || (componentData.m_Flags & TempFlags.Select) == 0) && (!CollectionUtils.TryGet(nativeArray3, j, out var value3) || !m_TempData.TryGetComponent(value3.m_Attached, out var componentData2) || (componentData2.m_Flags & TempFlags.Select) == 0))
					{
						Temp temp = nativeArray2[j];
						if (m_EntityLookup.Exists(temp.m_Original) && (temp.m_Flags & TempFlags.Select) != 0)
						{
							entity = temp.m_Original;
						}
					}
				}
			}
			if (m_IconData.HasComponent(entity) && !m_OwnerData.HasComponent(entity) && m_TargetData.HasComponent(entity))
			{
				Target target = m_TargetData[entity];
				if (m_EntityLookup.Exists(target.m_Target))
				{
					if (m_TempData.HasComponent(target.m_Target))
					{
						Temp temp2 = m_TempData[target.m_Target];
						entity = ((!m_EntityLookup.Exists(temp2.m_Original)) ? Entity.Null : temp2.m_Original);
					}
					else
					{
						entity = target.m_Target;
					}
				}
				else
				{
					entity = Entity.Null;
				}
			}
			if (m_IconData.HasComponent(entity))
			{
				for (int k = 0; k < 4; k++)
				{
					if (m_VehicleData.HasComponent(entity))
					{
						break;
					}
					if (m_BuildingData.HasComponent(entity))
					{
						break;
					}
					if (!m_OwnerData.HasComponent(entity))
					{
						break;
					}
					entity = m_OwnerData[entity].m_Owner;
				}
			}
			if (!(entity != Entity.Null))
			{
				return;
			}
			m_Selected.Value = entity;
			if (m_DebugSelect)
			{
				if (m_DebugData.HasComponent(entity))
				{
					m_CommandBuffer.RemoveComponent<Debug>(entity);
				}
				else
				{
					m_CommandBuffer.AddComponent(entity, default(Debug));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AggregateElement> __Game_Net_AggregateElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Attachment> __Game_Objects_Attachment_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Debug> __Game_Tools_Debug_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Net_AggregateElement_RO_BufferLookup = state.GetBufferLookup<AggregateElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attachment>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Tools_Debug_RO_ComponentLookup = state.GetComponentLookup<Debug>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
		}
	}

	public const string kToolID = "Default Tool";

	private ToolOutputBarrier m_ToolOutputBarrier;

	private AudioManager m_AudioManager;

	private RenderingSystem m_RenderingSystem;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_DragQuery;

	private EntityQuery m_TempQuery;

	private EntityQuery m_InfomodeQuery;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_UpdateQuery;

	private Entity m_LastRaycastEntity;

	private float3 m_MouseDownPosition;

	private State m_State;

	private IProxyAction m_DefaultToolApply;

	private int m_LastSelectedIndex;

	private TypeHandle __TypeHandle;

	public override string toolID => "Default Tool";

	public override bool allowUnderground => true;

	public bool underground { get; set; }

	public bool ignoreErrors { get; set; }

	public bool allowManipulation { get; set; }

	public bool debugSelect { get; set; }

	public bool debugLandValue { get; set; }

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_DefaultToolApply;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_DragQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Owner>());
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>());
		m_InfomodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<InfomodeActive>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<InfoviewRouteData>(),
				ComponentType.ReadOnly<InfoviewNetStatusData>(),
				ComponentType.ReadOnly<InfoviewHeatmapData>()
			}
		});
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_UpdateQuery = GetEntityQuery(ComponentType.ReadOnly<ColorUpdated>());
		m_DefaultToolApply = InputManager.instance.toolActionCollection.GetActionState("Default Tool", GetType().Name);
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_LastRaycastEntity = Entity.Null;
		SetState(State.Default);
		base.applyMode = ApplyMode.None;
		base.requireUnderground = false;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			base.applyActionOverride = ((m_LastRaycastEntity != Entity.Null) ? m_DefaultToolApply : m_MouseApply);
			base.applyAction.shouldBeEnabled = base.actionsEnabled;
			base.cancelActionOverride = m_MouseCancel;
			base.cancelAction.shouldBeEnabled = base.actionsEnabled;
		}
	}

	public override PrefabBase GetPrefab()
	{
		return null;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		return false;
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
		if (underground)
		{
			m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
		}
		else
		{
			m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
		}
		if (m_State != State.Default)
		{
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Net;
			m_ToolRaycastSystem.netLayerMask = Layer.Road;
			m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
			m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.MovingObjects | TypeMask.Labels | TypeMask.Icons;
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.OutsideConnections | RaycastFlags.Decals | RaycastFlags.BuildingLots;
			m_ToolRaycastSystem.netLayerMask = Layer.None;
			m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
			m_ToolRaycastSystem.iconLayerMask = IconLayerMask.Default;
			if (!underground)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Areas;
				m_ToolRaycastSystem.areaTypeMask |= AreaTypeMask.Lots;
			}
			if (debugSelect)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Net;
				m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
				m_ToolRaycastSystem.netLayerMask |= Layer.All;
				if (m_RenderingSystem.markersVisible)
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Markers;
				}
			}
			else if (debugLandValue)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.Terrain;
			}
			if (!m_InfomodeQuery.IsEmptyIgnoreFilter)
			{
				SetInfomodeRaycastSettings();
			}
		}
		if (m_ToolSystem.actionMode.IsEditor())
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.Markers | RaycastFlags.UpgradeIsMain | RaycastFlags.EditorContainers;
		}
		else
		{
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubBuildings;
		}
	}

	private void SetInfomodeRaycastSettings()
	{
		NativeArray<Entity> nativeArray = m_InfomodeQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				if (base.EntityManager.TryGetComponent<InfoviewRouteData>(entity, out var component))
				{
					m_ToolRaycastSystem.typeMask |= TypeMask.RouteWaypoints | TypeMask.RouteSegments;
					m_ToolRaycastSystem.routeType = component.m_Type;
				}
				if (base.EntityManager.TryGetComponent<InfoviewNetStatusData>(entity, out var component2))
				{
					switch (component2.m_Type)
					{
					case NetStatusType.LowVoltageFlow:
						m_ToolRaycastSystem.typeMask |= TypeMask.Lanes;
						m_ToolRaycastSystem.utilityTypeMask |= UtilityTypes.LowVoltageLine;
						break;
					case NetStatusType.HighVoltageFlow:
						m_ToolRaycastSystem.typeMask |= TypeMask.Lanes;
						m_ToolRaycastSystem.utilityTypeMask |= UtilityTypes.HighVoltageLine;
						break;
					case NetStatusType.PipeWaterFlow:
						m_ToolRaycastSystem.typeMask |= TypeMask.Lanes;
						m_ToolRaycastSystem.utilityTypeMask |= UtilityTypes.WaterPipe;
						break;
					case NetStatusType.PipeSewageFlow:
						m_ToolRaycastSystem.typeMask |= TypeMask.Lanes;
						m_ToolRaycastSystem.utilityTypeMask |= UtilityTypes.SewagePipe;
						break;
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void PlaySelectedSound(Entity selected, bool forcePlay = false)
	{
		Game.Creatures.Resident component;
		Citizen component2;
		PrefabRef component3;
		PrefabRef component4;
		SelectedSoundData component5;
		Entity clipEntity = ((base.EntityManager.TryGetComponent<Game.Creatures.Resident>(selected, out component) && base.EntityManager.TryGetComponent<Citizen>(component.m_Citizen, out component2) && base.EntityManager.TryGetComponent<PrefabRef>(component.m_Citizen, out component3)) ? CitizenUtils.GetCitizenSelectedSound(base.EntityManager, component.m_Citizen, component2, component3.m_Prefab) : ((!base.EntityManager.TryGetComponent<PrefabRef>(selected, out component4) || !base.EntityManager.TryGetComponent<SelectedSoundData>(component4.m_Prefab, out component5)) ? m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_SelectEntitySound : component5.m_selectedSound));
		if (forcePlay)
		{
			m_AudioManager.PlayUISound(clipEntity);
		}
		else
		{
			m_AudioManager.PlayUISoundIfNotPlaying(clipEntity);
		}
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		base.requireUnderground = underground;
		m_ForceUpdate |= !m_UpdateQuery.IsEmptyIgnoreFilter;
		if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
		{
			JobHandle result = ((m_State == State.Default && base.applyAction.WasPressedThisFrame()) ? Apply(inputDeps, base.applyAction.WasReleasedThisFrame(), base.cancelAction.WasPressedThisFrame()) : ((m_State != State.Default && base.applyAction.WasReleasedThisFrame()) ? Apply(inputDeps) : ((!base.cancelAction.IsInProgress()) ? Update(inputDeps) : Cancel(inputDeps))));
			UpdateActions();
			return result;
		}
		if (m_State == State.Default)
		{
			m_LastRaycastEntity = Entity.Null;
		}
		else if (base.applyAction.WasReleasedThisFrame())
		{
			m_LastRaycastEntity = Entity.Null;
			SetState(State.Default);
		}
		UpdateActions();
		return Clear(inputDeps);
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		return inputDeps;
	}

	private JobHandle Cancel(JobHandle inputDeps)
	{
		switch (m_State)
		{
		case State.Dragging:
			StopDragging();
			base.applyMode = ApplyMode.None;
			return inputDeps;
		case State.Default:
			base.applyMode = ApplyMode.None;
			m_ToolSystem.selected = Entity.Null;
			return inputDeps;
		default:
			SetState(State.Default);
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false, bool toggleSelected = false)
	{
		switch (m_State)
		{
		case State.Default:
			if (!singleFrameOnly)
			{
				SetState(State.MouseDownPrepare);
			}
			base.applyMode = ApplyMode.None;
			return SelectTempEntity(inputDeps, toggleSelected);
		case State.Dragging:
			StopDragging();
			base.applyMode = ApplyMode.Apply;
			return inputDeps;
		default:
			SetState(State.Default);
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
	}

	private JobHandle Update(JobHandle inputDeps)
	{
		switch (m_State)
		{
		case State.Default:
		{
			if (GetRaycastResult(out var entity2, out var hit2, out var forceUpdate) && entity2 == m_LastRaycastEntity && !forceUpdate)
			{
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			m_LastRaycastEntity = entity2;
			base.applyMode = ApplyMode.Clear;
			return UpdateDefinitions(inputDeps, entity2, hit2.m_CellIndex.x, default(float3), setPosition: false);
		}
		case State.MouseDownPrepare:
		{
			if (GetRaycastResult(out Entity _, out RaycastHit hit4))
			{
				m_MouseDownPosition = hit4.m_HitPosition;
				SetState(State.MouseDown);
			}
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		case State.MouseDown:
		{
			if (GetRaycastResult(out Entity _, out RaycastHit hit3) && math.distance(hit3.m_HitPosition, m_MouseDownPosition) > 1f)
			{
				StartDragging(hit3);
			}
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		case State.Dragging:
		{
			if (GetRaycastResult(out Entity _, out RaycastHit hit))
			{
				if (!m_DragQuery.IsEmptyIgnoreFilter)
				{
					NativeArray<ArchetypeChunk> nativeArray = m_DragQuery.ToArchetypeChunkArray(Allocator.TempJob);
					Entity e = nativeArray[0].GetNativeArray(GetEntityTypeHandle())[0];
					ComponentTypeHandle<Transform> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
					Transform component = nativeArray[0].GetNativeArray(ref typeHandle)[0];
					nativeArray.Dispose();
					EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
					component.m_Position = hit.m_HitPosition;
					entityCommandBuffer.SetComponent(e, component);
				}
				else if (base.EntityManager.Exists(m_LastRaycastEntity))
				{
					return UpdateDefinitions(inputDeps, m_LastRaycastEntity, hit.m_CellIndex.x, hit.m_HitPosition, setPosition: true);
				}
			}
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		default:
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
	}

	private void StartDragging(RaycastHit raycastHit)
	{
		Entity entity = Entity.Null;
		Temp component = default(Temp);
		Transform component2 = default(Transform);
		bool flag = false;
		if (allowManipulation && !m_DragQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_DragQuery.ToArchetypeChunkArray(Allocator.TempJob);
			try
			{
				entity = nativeArray[0].GetNativeArray(GetEntityTypeHandle())[0];
				ComponentTypeHandle<Temp> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
				component = nativeArray[0].GetNativeArray(ref typeHandle)[0];
				ComponentTypeHandle<Transform> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef);
				NativeArray<Transform> nativeArray2 = nativeArray[0].GetNativeArray(ref typeHandle2);
				if (nativeArray2.Length != 0)
				{
					component2 = nativeArray2[0];
					flag = base.EntityManager.HasComponent<Moving>(entity) || base.EntityManager.HasComponent<Game.Objects.Marker>(entity);
				}
				else
				{
					flag = false;
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
		}
		if (flag)
		{
			EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
			component.m_Flags |= TempFlags.Dragging;
			component2.m_Position = raycastHit.m_HitPosition;
			entityCommandBuffer.SetComponent(entity, component);
			entityCommandBuffer.SetComponent(entity, component2);
			entityCommandBuffer.AddComponent(entity, default(Updated));
			SetState(State.Dragging);
		}
		else
		{
			SetState(State.Default);
		}
	}

	private void StopDragging()
	{
		if (!m_DragQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_DragQuery.ToArchetypeChunkArray(Allocator.TempJob);
			Entity e = nativeArray[0].GetNativeArray(GetEntityTypeHandle())[0];
			ComponentTypeHandle<Temp> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			Temp component = nativeArray[0].GetNativeArray(ref typeHandle)[0];
			nativeArray.Dispose();
			EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
			component.m_Flags &= ~TempFlags.Dragging;
			entityCommandBuffer.SetComponent(e, component);
			entityCommandBuffer.AddComponent(e, default(Updated));
		}
		SetState(State.Default);
	}

	private void SetState(State state)
	{
		m_State = state;
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps, Entity entity, int index, float3 position, bool setPosition)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (entity != Entity.Null)
		{
			JobHandle jobHandle2 = IJobExtensions.Schedule(new CreateDefinitionsJob
			{
				m_Entity = entity,
				m_Position = position,
				m_SetPosition = setPosition,
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RoutePositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
				m_AggregateElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_AggregateElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
			}, inputDeps);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			m_LastSelectedIndex = index;
		}
		return jobHandle;
	}

	private JobHandle SelectTempEntity(JobHandle inputDeps, bool toggleSelected)
	{
		if (m_TempQuery.IsEmptyIgnoreFilter)
		{
			m_ToolSystem.selected = Entity.Null;
			return inputDeps;
		}
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_TempQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		NativeReference<Entity> selected = new NativeReference<Entity>(Allocator.TempJob);
		JobHandle jobHandle = IJobExtensions.Schedule(new SelectEntityJob
		{
			m_Chunks = chunks,
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachmentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DebugData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Debug_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DebugSelect = debugSelect,
			m_Selected = selected,
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(inputDeps, outJobHandle));
		chunks.Dispose(jobHandle);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		jobHandle.Complete();
		if (!base.EntityManager.HasBuffer<AggregateElement>(selected.Value))
		{
			m_LastSelectedIndex = -1;
		}
		if (m_ToolSystem.selected != selected.Value || m_ToolSystem.selectedIndex != m_LastSelectedIndex)
		{
			m_ToolSystem.selected = selected.Value;
			m_ToolSystem.selectedIndex = m_LastSelectedIndex;
			PlaySelectedSound(selected.Value, forcePlay: true);
		}
		else if (toggleSelected)
		{
			m_ToolSystem.selected = Entity.Null;
		}
		else
		{
			PlaySelectedSound(selected.Value);
		}
		selected.Dispose();
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
	public DefaultToolSystem()
	{
	}
}
