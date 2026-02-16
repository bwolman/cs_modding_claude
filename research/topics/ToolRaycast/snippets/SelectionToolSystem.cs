using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Audio;
using Game.Common;
using Game.Input;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class SelectionToolSystem : ToolBaseSystem
{
	public enum State
	{
		Default,
		Selecting,
		Deselecting
	}

	[BurstCompile]
	private struct FindEntitiesJob : IJob
	{
		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Quad2 m_Quad;

			public AreaType m_AreaType;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<AreaGeometryData> m_AreaGeometryData;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public NativeList<Entity> m_Entities;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Quad);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Quad))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[areaItem.m_Area];
				if (m_AreaGeometryData[prefabRef.m_Prefab].m_Type == m_AreaType)
				{
					Triangle2 triangle = AreaUtils.GetTriangle2(m_Nodes[areaItem.m_Area], m_Triangles[areaItem.m_Area][areaItem.m_Triangle]);
					if (MathUtils.Intersect(m_Quad, triangle))
					{
						m_Entities.Add(in areaItem.m_Area);
					}
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		[ReadOnly]
		public ControlPoint m_StartPoint;

		[ReadOnly]
		public ControlPoint m_EndPoint;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public Quad2 m_SelectionQuad;

		[ReadOnly]
		public AreaType m_AreaType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_AreaGeometryData;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		public NativeList<Entity> m_Entities;

		public void Execute()
		{
			if (m_StartPoint.m_OriginalEntity != Entity.Null && m_AreaType != AreaType.None && m_Nodes.HasBuffer(m_StartPoint.m_OriginalEntity))
			{
				m_Entities.Add(in m_StartPoint.m_OriginalEntity);
			}
			if (m_EndPoint.m_OriginalEntity != Entity.Null && m_AreaType != AreaType.None && m_Nodes.HasBuffer(m_EndPoint.m_OriginalEntity))
			{
				m_Entities.Add(in m_EndPoint.m_OriginalEntity);
			}
			if (!m_StartPoint.Equals(default(ControlPoint)) && !m_EndPoint.Equals(default(ControlPoint)) && m_AreaType != AreaType.None)
			{
				AreaIterator iterator = new AreaIterator
				{
					m_Quad = m_SelectionQuad,
					m_AreaType = m_AreaType,
					m_PrefabRefData = m_PrefabRefData,
					m_AreaGeometryData = m_AreaGeometryData,
					m_Nodes = m_Nodes,
					m_Triangles = m_Triangles,
					m_Entities = m_Entities
				};
				m_AreaSearchTree.Iterate(ref iterator);
			}
			m_Entities.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num = 0;
			int num2 = 0;
			while (num < m_Entities.Length)
			{
				Entity entity2 = m_Entities[num++];
				if (entity2 != entity)
				{
					m_Entities[num2++] = entity2;
					entity = entity2;
				}
			}
			if (num2 < m_Entities.Length)
			{
				m_Entities.RemoveRange(num2, m_Entities.Length - num2);
			}
		}
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<MapTile> m_MapTileData;

		[ReadOnly]
		public BufferLookup<Node> m_AreaNodes;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			if (m_AreaNodes.HasBuffer(entity) && (m_EditorMode || !m_MapTileData.HasComponent(entity) || m_NativeData.HasComponent(entity)))
			{
				Entity e = m_CommandBuffer.CreateEntity(index);
				CreationDefinition component = new CreationDefinition
				{
					m_Original = entity
				};
				component.m_Flags |= CreationFlags.Select;
				m_CommandBuffer.AddComponent(index, e, component);
				m_CommandBuffer.AddComponent(index, e, default(Updated));
				DynamicBuffer<Node> dynamicBuffer = m_AreaNodes[entity];
				DynamicBuffer<Node> dynamicBuffer2 = m_CommandBuffer.AddBuffer<Node>(index, e);
				dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
				dynamicBuffer2.CopyFrom(dynamicBuffer.AsNativeArray());
			}
		}
	}

	[BurstCompile]
	private struct ToggleEntityJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_SelectionEntity;

		[ReadOnly]
		public bool m_Select;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Native> m_NativeType;

		[ReadOnly]
		public ComponentTypeHandle<MapTile> m_MapTileType;

		public BufferLookup<SelectionElement> m_SelectionElements;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (!m_EditorMode && chunk.Has(ref m_MapTileType) && !chunk.Has(ref m_NativeType))
			{
				return;
			}
			NativeArray<Temp> nativeArray = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Temp temp = nativeArray[i];
				if (!(temp.m_Original != Entity.Null) || !m_SelectionElements.HasBuffer(m_SelectionEntity))
				{
					continue;
				}
				DynamicBuffer<SelectionElement> dynamicBuffer = m_SelectionElements[m_SelectionEntity];
				int num = 0;
				while (true)
				{
					if (num < dynamicBuffer.Length)
					{
						if (dynamicBuffer[num].m_Entity.Equals(temp.m_Original))
						{
							if (!m_Select)
							{
								dynamicBuffer.RemoveAt(num);
							}
							break;
						}
						num++;
						continue;
					}
					if (m_Select)
					{
						dynamicBuffer.Add(new SelectionElement(temp.m_Original));
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
	private struct CopyStartTilesJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectionEntity;

		[ReadOnly]
		public NativeList<Entity> m_StartTiles;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public BufferLookup<SelectionElement> m_SelectionElements;

		public void Execute()
		{
			if (!m_SelectionElements.HasBuffer(m_SelectionEntity))
			{
				return;
			}
			DynamicBuffer<SelectionElement> dynamicBuffer = m_SelectionElements[m_SelectionEntity];
			dynamicBuffer.Clear();
			dynamicBuffer.EnsureCapacity(m_StartTiles.Length);
			for (int i = 0; i < m_StartTiles.Length; i++)
			{
				Entity entity = m_StartTiles[i];
				if (m_PrefabRefData.HasComponent(entity))
				{
					dynamicBuffer.Add(new SelectionElement(entity));
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdateStartTilesJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectionEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<SelectionElement> m_SelectionElements;

		public NativeList<Entity> m_StartTiles;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (!m_SelectionElements.HasBuffer(m_SelectionEntity))
			{
				return;
			}
			DynamicBuffer<SelectionElement> dynamicBuffer = m_SelectionElements[m_SelectionEntity];
			for (int i = 0; i < m_StartTiles.Length; i++)
			{
				Entity entity = m_StartTiles[i];
				if (m_PrefabRefData.HasComponent(entity))
				{
					m_CommandBuffer.AddComponent(entity, default(Updated));
				}
			}
			m_StartTiles.ResizeUninitialized(dynamicBuffer.Length);
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				Entity entity2 = dynamicBuffer[j].m_Entity;
				m_StartTiles[j] = entity2;
				m_CommandBuffer.AddComponent(entity2, default(Updated));
			}
		}
	}

	[BurstCompile]
	private struct CopyServiceDistrictsJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectionEntity;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public BufferLookup<SelectionElement> m_SelectionElements;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (!m_OwnerData.HasComponent(m_SelectionEntity) || !m_SelectionElements.HasBuffer(m_SelectionEntity))
			{
				return;
			}
			Owner owner = m_OwnerData[m_SelectionEntity];
			DynamicBuffer<SelectionElement> dynamicBuffer = m_SelectionElements[m_SelectionEntity];
			if (!m_ServiceDistricts.HasBuffer(owner.m_Owner))
			{
				return;
			}
			DynamicBuffer<ServiceDistrict> dynamicBuffer2 = m_ServiceDistricts[owner.m_Owner];
			dynamicBuffer.Clear();
			dynamicBuffer.EnsureCapacity(dynamicBuffer2.Length);
			for (int i = 0; i < dynamicBuffer2.Length; i++)
			{
				Entity district = dynamicBuffer2[i].m_District;
				if (m_PrefabRefData.HasComponent(district))
				{
					dynamicBuffer.Add(new SelectionElement(district));
				}
			}
			m_CommandBuffer.AddComponent<Updated>(owner.m_Owner);
		}
	}

	[BurstCompile]
	private struct UpdateServiceDistrictsJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectionEntity;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public BufferLookup<SelectionElement> m_SelectionElements;

		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (!m_OwnerData.HasComponent(m_SelectionEntity) || !m_SelectionElements.HasBuffer(m_SelectionEntity))
			{
				return;
			}
			Owner owner = m_OwnerData[m_SelectionEntity];
			DynamicBuffer<SelectionElement> dynamicBuffer = m_SelectionElements[m_SelectionEntity];
			if (m_ServiceDistricts.HasBuffer(owner.m_Owner))
			{
				DynamicBuffer<ServiceDistrict> dynamicBuffer2 = m_ServiceDistricts[owner.m_Owner];
				dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					dynamicBuffer2[i] = new ServiceDistrict(dynamicBuffer[i].m_Entity);
				}
				m_CommandBuffer.AddComponent<Updated>(owner.m_Owner);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MapTile> __Game_Areas_MapTile_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Native> __Game_Common_Native_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MapTile> __Game_Areas_MapTile_RO_ComponentTypeHandle;

		public BufferLookup<SelectionElement> __Game_Tools_SelectionElement_RW_BufferLookup;

		[ReadOnly]
		public BufferLookup<SelectionElement> __Game_Tools_SelectionElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> __Game_Areas_ServiceDistrict_RO_BufferLookup;

		public BufferLookup<ServiceDistrict> __Game_Areas_ServiceDistrict_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Areas_MapTile_RO_ComponentLookup = state.GetComponentLookup<MapTile>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Native>(isReadOnly: true);
			__Game_Areas_MapTile_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MapTile>(isReadOnly: true);
			__Game_Tools_SelectionElement_RW_BufferLookup = state.GetBufferLookup<SelectionElement>();
			__Game_Tools_SelectionElement_RO_BufferLookup = state.GetBufferLookup<SelectionElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Areas_ServiceDistrict_RO_BufferLookup = state.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
			__Game_Areas_ServiceDistrict_RW_BufferLookup = state.GetBufferLookup<ServiceDistrict>();
		}
	}

	public const string kToolID = "Selection Tool";

	private SearchSystem m_AreaSearchSystem;

	private MapTileSystem m_MapTileSystem;

	private MapTilePurchaseSystem m_MapTilePurchaseSystem;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private AudioManager m_AudioManager;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_DefinitionGroup;

	private EntityQuery m_TempGroup;

	private EntityQuery m_SoundQuery;

	private Entity m_SelectionEntity;

	private Entity m_LastOwner;

	private SelectionType m_LastType;

	private EntityArchetype m_SelectionArchetype;

	private State m_State;

	private ControlPoint m_StartPoint;

	private ControlPoint m_RaycastPoint;

	private IProxyAction m_SelectArea;

	private IProxyAction m_DeselectArea;

	private IProxyAction m_DiscardSelect;

	private IProxyAction m_DiscardDeselect;

	private bool m_ApplyBlocked;

	private TypeHandle __TypeHandle;

	public override string toolID => "Selection Tool";

	public SelectionType selectionType { get; set; }

	public Entity selectionOwner { get; set; }

	public bool requestSelectionUpdate { get; set; }

	public State state => m_State;

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_SelectArea;
			yield return m_DeselectArea;
			yield return m_DiscardSelect;
			yield return m_DiscardDeselect;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_MapTileSystem = base.World.GetOrCreateSystemManaged<MapTileSystem>();
		m_MapTilePurchaseSystem = base.World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_DefinitionGroup = GetDefinitionQuery();
		m_TempGroup = GetEntityQuery(ComponentType.ReadOnly<Temp>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_SelectionArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<SelectionInfo>(), ComponentType.ReadWrite<SelectionElement>());
		m_SelectArea = InputManager.instance.toolActionCollection.GetActionState("Select Area", "SelectionToolSystem");
		m_DeselectArea = InputManager.instance.toolActionCollection.GetActionState("Deselect Area", "SelectionToolSystem");
		m_DiscardSelect = InputManager.instance.toolActionCollection.GetActionState("Discard Select", "SelectionToolSystem");
		m_DiscardDeselect = InputManager.instance.toolActionCollection.GetActionState("Discard Deselect", "SelectionToolSystem");
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_State = State.Default;
		m_StartPoint = default(ControlPoint);
		m_ApplyBlocked = false;
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		if (m_SelectionEntity != Entity.Null)
		{
			base.EntityManager.DestroyEntity(m_SelectionEntity);
			m_SelectionEntity = Entity.Null;
		}
		base.OnStopRunning();
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			switch (selectionType)
			{
			case SelectionType.MapTiles:
				if (m_ToolSystem.actionMode.IsGame() && m_MapTilePurchaseSystem.GetAvailableTiles() == 0)
				{
					base.applyAction.shouldBeEnabled = false;
					base.applyActionOverride = null;
					base.secondaryApplyAction.shouldBeEnabled = false;
					base.secondaryApplyActionOverride = null;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					break;
				}
				if (m_TempGroup.CalculateEntityCount() <= 1)
				{
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = m_SelectArea;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
					base.secondaryApplyActionOverride = m_DeselectArea;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					break;
				}
				switch (m_State)
				{
				case State.Default:
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = m_SelectArea;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
					base.secondaryApplyActionOverride = m_DeselectArea;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					break;
				case State.Selecting:
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = m_SelectArea;
					base.secondaryApplyAction.shouldBeEnabled = false;
					base.secondaryApplyActionOverride = null;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled;
					base.cancelActionOverride = m_DiscardSelect;
					break;
				case State.Deselecting:
					base.applyAction.shouldBeEnabled = false;
					base.applyActionOverride = null;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
					base.secondaryApplyActionOverride = m_DeselectArea;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled;
					base.cancelActionOverride = m_DiscardDeselect;
					break;
				}
				break;
			case SelectionType.ServiceDistrict:
				switch (m_State)
				{
				case State.Default:
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = m_SelectArea;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
					base.secondaryApplyActionOverride = m_DeselectArea;
					base.cancelAction.shouldBeEnabled = false;
					base.cancelActionOverride = null;
					break;
				case State.Selecting:
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = m_SelectArea;
					base.secondaryApplyAction.shouldBeEnabled = false;
					base.secondaryApplyActionOverride = null;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled;
					base.cancelActionOverride = m_DiscardSelect;
					break;
				case State.Deselecting:
					base.applyAction.shouldBeEnabled = false;
					base.applyActionOverride = null;
					base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
					base.secondaryApplyActionOverride = m_DeselectArea;
					base.cancelAction.shouldBeEnabled = base.actionsEnabled;
					base.cancelActionOverride = m_DiscardDeselect;
					break;
				}
				break;
			default:
				base.applyAction.shouldBeEnabled = false;
				base.applyActionOverride = null;
				base.secondaryApplyAction.shouldBeEnabled = false;
				base.secondaryApplyActionOverride = null;
				base.cancelAction.shouldBeEnabled = false;
				base.cancelActionOverride = null;
				break;
			}
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

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		SelectionType selectionType = this.selectionType;
		if ((uint)(selectionType - 1) <= 1u)
		{
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Areas | TypeMask.Water;
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.None;
		}
		m_ToolRaycastSystem.areaTypeMask = AreaUtils.GetTypeMask(GetAreaType(this.selectionType));
	}

	private AreaType GetAreaType(SelectionType selectionType)
	{
		return selectionType switch
		{
			SelectionType.ServiceDistrict => AreaType.District, 
			SelectionType.MapTiles => AreaType.MapTile, 
			_ => AreaType.None, 
		};
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		if (requestSelectionUpdate || m_LastOwner != selectionOwner || m_LastType != selectionType || m_SelectionEntity == Entity.Null)
		{
			if (m_SelectionEntity != Entity.Null)
			{
				base.EntityManager.DestroyEntity(m_SelectionEntity);
			}
			m_SelectionEntity = base.EntityManager.CreateEntity(m_SelectionArchetype);
			SelectionInfo componentData = default(SelectionInfo);
			componentData.m_SelectionType = selectionType;
			componentData.m_AreaType = GetAreaType(selectionType);
			base.EntityManager.SetComponentData(m_SelectionEntity, componentData);
			if (selectionOwner != Entity.Null)
			{
				base.EntityManager.AddComponentData(m_SelectionEntity, new Owner(selectionOwner));
			}
			m_LastOwner = selectionOwner;
			m_LastType = selectionType;
			requestSelectionUpdate = false;
			base.requireAreas = m_ToolRaycastSystem.areaTypeMask;
			inputDeps = CopySelection(inputDeps);
		}
		UpdateActions();
		if (m_State != State.Default && !base.applyAction.enabled && !base.secondaryApplyAction.enabled)
		{
			m_State = State.Default;
		}
		if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
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
					return Update(inputDeps);
				}
				if (base.secondaryApplyAction.WasPressedThisFrame())
				{
					return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
				}
				if (base.applyAction.WasPressedThisFrame())
				{
					return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
				}
				break;
			case State.Selecting:
				if (base.cancelAction.WasPressedThisFrame())
				{
					m_ApplyBlocked = true;
					return Cancel(inputDeps);
				}
				if (base.applyAction.WasPressedThisFrame() || base.applyAction.WasReleasedThisFrame())
				{
					return Apply(inputDeps);
				}
				break;
			case State.Deselecting:
				if (base.cancelAction.WasPressedThisFrame())
				{
					m_ApplyBlocked = true;
					return Apply(inputDeps);
				}
				if (base.secondaryApplyAction.WasPressedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
				{
					return Cancel(inputDeps);
				}
				break;
			}
			return Update(inputDeps);
		}
		if (m_State != State.Default && (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame()))
		{
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
		}
		return Clear(inputDeps);
	}

	private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		switch (m_State)
		{
		case State.Selecting:
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			base.applyMode = ApplyMode.Clear;
			m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_AreaMarqueeEndSound);
			return UpdateDefinitions(inputDeps);
		case State.Deselecting:
			if (!m_RaycastPoint.Equals(default(ControlPoint)) && GetAllowApply())
			{
				inputDeps = ToggleTempEntity(inputDeps, select: false);
				inputDeps = UpdateSelection(inputDeps);
			}
			if (math.distance(m_StartPoint.m_Position, m_RaycastPoint.m_Position) > 1f)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_AreaMarqueeClearEndSound);
			}
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			base.applyMode = ApplyMode.Clear;
			return UpdateDefinitions(inputDeps);
		default:
			if (!m_RaycastPoint.Equals(default(ControlPoint)))
			{
				if (singleFrameOnly)
				{
					if (GetAllowApply())
					{
						inputDeps = ToggleTempEntity(inputDeps, select: false);
						inputDeps = UpdateSelection(inputDeps);
						GetRaycastResult(out m_RaycastPoint);
						base.applyMode = ApplyMode.Clear;
						return UpdateDefinitions(inputDeps);
					}
				}
				else
				{
					m_StartPoint = m_RaycastPoint;
					m_State = State.Deselecting;
				}
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_AreaMarqueeClearStartSound);
			}
			return Update(inputDeps);
		}
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		switch (m_State)
		{
		case State.Selecting:
			if (!m_RaycastPoint.Equals(default(ControlPoint)) && GetAllowApply())
			{
				inputDeps = ToggleTempEntity(inputDeps, select: true);
				inputDeps = UpdateSelection(inputDeps);
			}
			if (math.distance(m_StartPoint.m_Position, m_RaycastPoint.m_Position) > 1f)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_AreaMarqueeEndSound);
			}
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			base.applyMode = ApplyMode.Clear;
			return UpdateDefinitions(inputDeps);
		case State.Deselecting:
			if (math.distance(m_StartPoint.m_Position, m_RaycastPoint.m_Position) > 1f)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_AreaMarqueeClearEndSound);
			}
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			base.applyMode = ApplyMode.Clear;
			return UpdateDefinitions(inputDeps);
		default:
			if (!m_RaycastPoint.Equals(default(ControlPoint)))
			{
				if (singleFrameOnly)
				{
					if (GetAllowApply())
					{
						inputDeps = ToggleTempEntity(inputDeps, select: true);
						inputDeps = UpdateSelection(inputDeps);
						GetRaycastResult(out m_RaycastPoint);
						base.applyMode = ApplyMode.Clear;
						return UpdateDefinitions(inputDeps);
					}
				}
				else
				{
					m_StartPoint = m_RaycastPoint;
					m_State = State.Selecting;
				}
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_AreaMarqueeStartSound);
			}
			return Update(inputDeps);
		}
	}

	private JobHandle Update(JobHandle inputDeps)
	{
		State state = m_State;
		if ((uint)(state - 1) <= 1u)
		{
			if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate) && controlPoint.Equals(m_RaycastPoint) && !forceUpdate)
			{
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			m_RaycastPoint = controlPoint;
			base.applyMode = ApplyMode.Clear;
			return UpdateDefinitions(inputDeps);
		}
		if (GetRaycastResult(out ControlPoint controlPoint2, out bool forceUpdate2) && controlPoint2.m_OriginalEntity == m_RaycastPoint.m_OriginalEntity && !forceUpdate2 && !base.EntityManager.HasComponent<Updated>(m_RaycastPoint.m_OriginalEntity))
		{
			m_RaycastPoint = controlPoint2;
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		m_RaycastPoint = controlPoint2;
		base.applyMode = ApplyMode.Clear;
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		return inputDeps;
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionGroup, m_ToolOutputBarrier, inputDeps);
		if (m_State != State.Default || m_RaycastPoint.m_OriginalEntity != Entity.Null)
		{
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			GetSelectionQuad(out var quad);
			JobHandle dependencies;
			FindEntitiesJob jobData = new FindEntitiesJob
			{
				m_StartPoint = ((m_State != State.Default) ? m_StartPoint : default(ControlPoint)),
				m_EndPoint = m_RaycastPoint,
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
				m_SelectionQuad = quad.xz,
				m_AreaType = GetAreaType(selectionType),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_Entities = nativeList
			};
			CreateDefinitionsJob jobData2 = new CreateDefinitionsJob
			{
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MapTileData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_MapTile_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Entities = nativeList.AsDeferredJobArray(),
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(inputDeps, dependencies));
			JobHandle jobHandle3 = jobData2.Schedule(nativeList, 4, jobHandle2);
			nativeList.Dispose(jobHandle3);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle2);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle3);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
		}
		return jobHandle;
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint)
	{
		if (selectionType == SelectionType.MapTiles && m_ToolSystem.actionMode.IsGame() && m_MapTilePurchaseSystem.GetAvailableTiles() == 0)
		{
			controlPoint = default(ControlPoint);
			return false;
		}
		return base.GetRaycastResult(out controlPoint);
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate)
	{
		if (selectionType == SelectionType.MapTiles && m_ToolSystem.actionMode.IsGame() && m_MapTilePurchaseSystem.GetAvailableTiles() == 0)
		{
			controlPoint = default(ControlPoint);
			forceUpdate = false;
			return false;
		}
		return base.GetRaycastResult(out controlPoint, out forceUpdate);
	}

	private JobHandle ToggleTempEntity(JobHandle inputDeps, bool select)
	{
		if (m_TempGroup.IsEmptyIgnoreFilter)
		{
			return inputDeps;
		}
		return JobChunkExtensions.Schedule(new ToggleEntityJob
		{
			m_SelectionEntity = m_SelectionEntity,
			m_Select = select,
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NativeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Native_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MapTileType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_MapTile_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SelectionElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_SelectionElement_RW_BufferLookup, ref base.CheckedStateRef)
		}, m_TempGroup, inputDeps);
	}

	private JobHandle CopySelection(JobHandle inputDeps)
	{
		switch (selectionType)
		{
		case SelectionType.ServiceDistrict:
			return CopyServiceDistricts(inputDeps);
		case SelectionType.MapTiles:
			if (m_ToolSystem.actionMode.IsEditor())
			{
				return CopyStartTiles(inputDeps);
			}
			return inputDeps;
		default:
			return inputDeps;
		}
	}

	private JobHandle UpdateSelection(JobHandle inputDeps)
	{
		switch (selectionType)
		{
		case SelectionType.ServiceDistrict:
			return UpdateServiceDistricts(inputDeps);
		case SelectionType.MapTiles:
			if (m_ToolSystem.actionMode.IsEditor())
			{
				return UpdateStartTiles(inputDeps);
			}
			return inputDeps;
		default:
			return inputDeps;
		}
	}

	private JobHandle CopyStartTiles(JobHandle inputDeps)
	{
		return IJobExtensions.Schedule(new CopyStartTilesJob
		{
			m_SelectionEntity = m_SelectionEntity,
			m_StartTiles = m_MapTileSystem.GetStartTiles(),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SelectionElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_SelectionElement_RW_BufferLookup, ref base.CheckedStateRef)
		}, inputDeps);
	}

	private JobHandle UpdateStartTiles(JobHandle inputDeps)
	{
		JobHandle jobHandle = IJobExtensions.Schedule(new UpdateStartTilesJob
		{
			m_SelectionEntity = m_SelectionEntity,
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SelectionElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_SelectionElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_StartTiles = m_MapTileSystem.GetStartTiles(),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, inputDeps);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		return jobHandle;
	}

	private JobHandle CopyServiceDistricts(JobHandle inputDeps)
	{
		JobHandle jobHandle = IJobExtensions.Schedule(new CopyServiceDistrictsJob
		{
			m_SelectionEntity = m_SelectionEntity,
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDistricts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_ServiceDistrict_RO_BufferLookup, ref base.CheckedStateRef),
			m_SelectionElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_SelectionElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, inputDeps);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		return jobHandle;
	}

	private JobHandle UpdateServiceDistricts(JobHandle inputDeps)
	{
		JobHandle jobHandle = IJobExtensions.Schedule(new UpdateServiceDistrictsJob
		{
			m_SelectionEntity = m_SelectionEntity,
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SelectionElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_SelectionElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceDistricts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_ServiceDistrict_RW_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, inputDeps);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		return jobHandle;
	}

	public bool GetSelectionQuad(out Quad3 quad)
	{
		Camera main = Camera.main;
		if (main == null)
		{
			quad = default(Quad3);
			return false;
		}
		Transform transform = main.transform;
		float3 @float = math.normalizesafe(new float3
		{
			xz = ((float3)transform.right).xz
		});
		float3 float2 = new float3
		{
			xz = MathUtils.Right(@float.xz)
		};
		float3 hitPosition = m_StartPoint.m_HitPosition;
		float3 x = m_RaycastPoint.m_HitPosition - hitPosition;
		float num = math.dot(x, @float);
		float num2 = math.dot(x, float2);
		if (num < 0f)
		{
			@float = -@float;
			num = 0f - num;
		}
		if (num2 < 0f)
		{
			float2 = -float2;
			num2 = 0f - num2;
		}
		quad.a = hitPosition;
		quad.b = hitPosition + float2 * num2;
		quad.c = hitPosition + @float * num + float2 * num2;
		quad.d = hitPosition + @float * num;
		TerrainHeightData terrainData = m_TerrainSystem.GetHeightData();
		JobHandle deps;
		WaterSurfaceData<SurfaceWater> data = m_WaterSystem.GetSurfaceData(out deps);
		deps.Complete();
		quad.a.y = WaterUtils.SampleHeight(ref data, ref terrainData, quad.a);
		quad.b.y = WaterUtils.SampleHeight(ref data, ref terrainData, quad.b);
		quad.c.y = WaterUtils.SampleHeight(ref data, ref terrainData, quad.c);
		quad.d.y = WaterUtils.SampleHeight(ref data, ref terrainData, quad.d);
		if (!m_StartPoint.Equals(default(ControlPoint)))
		{
			return !m_RaycastPoint.Equals(default(ControlPoint));
		}
		return false;
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
	public SelectionToolSystem()
	{
	}
}
