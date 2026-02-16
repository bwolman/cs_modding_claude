using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Entities;
using Game.Areas;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Notifications;
using Game.Prefabs;
using Game.Routes;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public abstract class ToolBaseSystem : GameSystemBase, IEquatable<ToolBaseSystem>
{
	[BurstCompile]
	private struct DestroyDefinitionsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, nativeArray[i]);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct InvertBrushesJob : IJobChunk
	{
		public ComponentTypeHandle<Brush> m_BrushType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Brush> nativeArray = chunk.GetNativeArray(ref m_BrushType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Brush value = nativeArray[i];
				value.m_Strength = 0f - value.m_Strength;
				nativeArray[i] = value;
			}
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

		public ComponentTypeHandle<Brush> __Game_Tools_Brush_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BrushData> __Game_Prefabs_BrushData_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Brush_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Brush>();
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_BrushData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BrushData>(isReadOnly: true);
		}
	}

	public const Snap kSnapAllIgnoredMask = Snap.AutoParent | Snap.PrefabType | Snap.ContourLines;

	protected ToolSystem m_ToolSystem;

	protected PrefabSystem m_PrefabSystem;

	protected DefaultToolSystem m_DefaultToolSystem;

	protected ToolRaycastSystem m_ToolRaycastSystem;

	protected OriginalDeletedSystem m_OriginalDeletedSystem;

	protected EntityQuery m_ErrorQuery;

	protected Snap m_SnapOnMask;

	protected Snap m_SnapOffMask;

	protected bool m_HasFocus;

	protected bool m_FocusChanged;

	protected bool m_ForceUpdate;

	private bool m_ActionsEnabled;

	private IProxyAction m_ApplyAction;

	private IProxyAction m_SecondaryApplyAction;

	private IProxyAction m_CancelAction;

	private protected IProxyAction m_DefaultApply;

	private protected IProxyAction m_DefaultSecondaryApply;

	private protected IProxyAction m_DefaultCancel;

	private protected IProxyAction m_MouseApply;

	private protected IProxyAction m_MouseCancel;

	private TypeHandle __TypeHandle;

	public abstract string toolID { get; }

	public virtual int uiModeIndex => 0;

	public virtual Color32 color { get; set; }

	public BrushPrefab brushType { get; set; }

	public BrushPrefab defaultBrushType { get; set; }

	public float brushSize { get; set; }

	public float brushAngle { get; set; }

	public float brushStrength { get; set; }

	public bool requireZones { get; protected set; }

	public bool requireUnderground { get; protected set; }

	public bool requirePipelines { get; protected set; }

	public bool requireNetArrows { get; protected set; }

	public bool requireStopIcons { get; protected set; }

	public AreaTypeMask requireAreas { get; protected set; }

	public RouteType requireRoutes { get; protected set; }

	public TransportType requireStops { get; protected set; }

	public Layer requireNet { get; protected set; }

	public InfoviewPrefab infoview { get; private set; }

	public List<InfomodePrefab> infomodes { get; private set; }

	public virtual Snap selectedSnap { get; set; }

	public ApplyMode applyMode { get; protected set; }

	public virtual bool allowUnderground { get; protected set; }

	public virtual bool brushing => false;

	protected IProxyAction applyAction => m_ApplyAction ?? (m_ApplyAction = m_DefaultApply);

	protected IProxyAction secondaryApplyAction => m_SecondaryApplyAction ?? (m_SecondaryApplyAction = m_DefaultSecondaryApply);

	protected IProxyAction cancelAction => m_CancelAction ?? (m_CancelAction = m_DefaultCancel);

	protected IProxyAction applyActionOverride
	{
		get
		{
			if (m_ApplyAction == m_DefaultApply)
			{
				return null;
			}
			return m_ApplyAction;
		}
		set
		{
			SetAction(ref m_ApplyAction, value ?? m_DefaultApply);
		}
	}

	protected IProxyAction secondaryApplyActionOverride
	{
		get
		{
			if (m_SecondaryApplyAction == m_DefaultSecondaryApply)
			{
				return null;
			}
			return m_SecondaryApplyAction;
		}
		set
		{
			SetAction(ref m_SecondaryApplyAction, value ?? m_DefaultSecondaryApply);
		}
	}

	protected IProxyAction cancelActionOverride
	{
		get
		{
			if (m_CancelAction == m_DefaultCancel)
			{
				return null;
			}
			return m_CancelAction;
		}
		set
		{
			SetAction(ref m_CancelAction, value ?? m_DefaultCancel);
		}
	}

	private protected virtual IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield break;
		}
	}

	private IEnumerable<IProxyAction> baseToolActions
	{
		get
		{
			yield return m_DefaultApply;
			yield return m_DefaultSecondaryApply;
			yield return m_DefaultCancel;
			yield return m_MouseCancel;
			yield return m_MouseApply;
		}
	}

	internal IEnumerable<IProxyAction> actions => baseToolActions.Concat(toolActions);

	private protected bool actionsEnabled
	{
		get
		{
			if (m_ActionsEnabled)
			{
				return !Game.Input.InputManager.instance.hasInputFieldFocus;
			}
			return false;
		}
		private set
		{
			m_ActionsEnabled = value;
		}
	}

	public static event Action<ProxyAction> EventToolActionPerformed;

	public virtual void GetUIModes(List<ToolMode> modes)
	{
	}

	public bool Equals(ToolBaseSystem other)
	{
		return this == other;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_OriginalDeletedSystem = base.World.GetOrCreateSystemManaged<OriginalDeletedSystem>();
		string name = GetType().Name;
		m_DefaultApply = Game.Input.InputManager.instance.toolActionCollection.GetActionState("Apply", name);
		m_DefaultSecondaryApply = Game.Input.InputManager.instance.toolActionCollection.GetActionState("Secondary Apply", name);
		m_DefaultCancel = Game.Input.InputManager.instance.toolActionCollection.GetActionState("Cancel", name);
		m_MouseApply = Game.Input.InputManager.instance.toolActionCollection.GetActionState("Mouse Apply", name);
		m_MouseCancel = Game.Input.InputManager.instance.toolActionCollection.GetActionState("Mouse Cancel", name);
		requireAreas = AreaTypeMask.None;
		requireRoutes = RouteType.None;
		requireStops = TransportType.None;
		selectedSnap = Snap.All;
		base.Enabled = false;
		m_HasFocus = true;
		m_ActionsEnabled = true;
		m_ErrorQuery = GetEntityQuery(ComponentType.ReadOnly<Error>());
		infomodes = new List<InfomodePrefab>();
		m_ToolSystem.tools.Add(this);
	}

	protected override void OnFocusChanged(bool hasfocus)
	{
		m_FocusChanged = hasfocus != m_HasFocus;
		m_HasFocus = hasfocus;
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ForceUpdate = true;
		SetActions();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		infoview = null;
		infomodes.Clear();
		ResetActions();
		base.OnStopRunning();
	}

	private protected virtual void SetActions()
	{
		UpdateActions();
		SetInteraction(set: true);
	}

	private protected virtual void ResetActions()
	{
		SetInteraction(set: false);
		using (ProxyAction.DeferStateUpdating())
		{
			applyAction.shouldBeEnabled = false;
			secondaryApplyAction.shouldBeEnabled = false;
			cancelAction.shouldBeEnabled = false;
			applyActionOverride = null;
			secondaryApplyActionOverride = null;
			cancelActionOverride = null;
			actionsEnabled = true;
		}
	}

	private protected virtual void UpdateActions()
	{
	}

	private void SetInteraction(bool set)
	{
		HashSet<ProxyAction> hashSet = new HashSet<ProxyAction>();
		foreach (IProxyAction action in actions)
		{
			if (!(action is UIBaseInputAction.IState state))
			{
				if (action is ProxyAction item)
				{
					hashSet.Add(item);
				}
				continue;
			}
			foreach (ProxyAction action2 in state.actions)
			{
				hashSet.Add(action2);
			}
		}
		foreach (ProxyAction item2 in hashSet)
		{
			if (set)
			{
				item2.onInteraction += OnActionInteraction;
			}
			else
			{
				item2.onInteraction -= OnActionInteraction;
			}
		}
	}

	public void ToggleToolOptions(bool enabled)
	{
		actionsEnabled = !enabled;
		UpdateActions();
	}

	private void OnActionInteraction(ProxyAction action, InputActionPhase phase)
	{
		if (phase == InputActionPhase.Performed)
		{
			ToolBaseSystem.EventToolActionPerformed?.Invoke(action);
		}
	}

	[Preserve]
	protected sealed override void OnUpdate()
	{
		base.Dependency = OnUpdate(base.Dependency);
		m_FocusChanged = false;
		m_ForceUpdate = false;
	}

	[Preserve]
	protected virtual JobHandle OnUpdate(JobHandle inputDeps)
	{
		return inputDeps;
	}

	[CanBeNull]
	public abstract PrefabBase GetPrefab();

	public abstract bool TrySetPrefab(PrefabBase prefab);

	public virtual void InitializeRaycast()
	{
		m_ToolRaycastSystem.raycastFlags &= ~(RaycastFlags.ElevateOffset | RaycastFlags.SubElements | RaycastFlags.Placeholders | RaycastFlags.Markers | RaycastFlags.NoMainElements | RaycastFlags.UpgradeIsMain | RaycastFlags.OutsideConnections | RaycastFlags.Outside | RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.Decals | RaycastFlags.EditorContainers | RaycastFlags.SubBuildings | RaycastFlags.PartialSurface | RaycastFlags.BuildingLots | RaycastFlags.IgnoreSecondary);
		m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
		m_ToolRaycastSystem.typeMask = TypeMask.None;
		m_ToolRaycastSystem.netLayerMask = Layer.None;
		m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
		m_ToolRaycastSystem.routeType = RouteType.None;
		m_ToolRaycastSystem.transportType = TransportType.None;
		m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
		m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.None;
		m_ToolRaycastSystem.rayOffset = default(float3);
		m_ToolRaycastSystem.owner = Entity.Null;
	}

	public virtual void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
	{
		onMask = Snap.None;
		offMask = Snap.None;
	}

	public virtual void SetUnderground(bool underground)
	{
	}

	public virtual void ElevationUp()
	{
	}

	public virtual void ElevationDown()
	{
	}

	public virtual void ElevationScroll()
	{
	}

	public static Snap GetActualSnap(Snap selectedSnap, Snap onMask, Snap offMask)
	{
		return (selectedSnap | ~offMask) & onMask;
	}

	protected Snap GetActualSnap()
	{
		return GetActualSnap(selectedSnap, m_SnapOnMask, m_SnapOffMask);
	}

	protected void UpdateInfoview(Entity prefab)
	{
		infomodes.Clear();
		if (base.EntityManager.HasComponent<NetData>(prefab) && base.EntityManager.TryGetBuffer(prefab, isReadOnly: true, out DynamicBuffer<SubObject> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				SubObject subObject = buffer[i];
				if ((subObject.m_Flags & SubObjectFlags.MakeOwner) != 0)
				{
					prefab = subObject.m_Prefab;
					break;
				}
			}
		}
		if (base.EntityManager.TryGetBuffer(prefab, isReadOnly: true, out DynamicBuffer<PlaceableInfoviewItem> buffer2) && buffer2.Length != 0)
		{
			infoview = m_PrefabSystem.GetPrefab<InfoviewPrefab>(buffer2[0].m_Item);
			for (int j = 1; j < buffer2.Length; j++)
			{
				infomodes.Add(m_PrefabSystem.GetPrefab<InfomodePrefab>(buffer2[j].m_Item));
			}
		}
		else
		{
			infoview = null;
		}
	}

	protected JobHandle DestroyDefinitions(EntityQuery group, ToolOutputBarrier barrier, JobHandle inputDeps)
	{
		if (group.IsEmptyIgnoreFilter)
		{
			return inputDeps;
		}
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new DestroyDefinitionsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CommandBuffer = barrier.CreateCommandBuffer().AsParallelWriter()
		}, group, inputDeps);
		barrier.AddJobHandleForProducer(jobHandle);
		return jobHandle;
	}

	protected JobHandle InvertBrushes(EntityQuery group, JobHandle inputDeps)
	{
		if (group.IsEmptyIgnoreFilter)
		{
			return inputDeps;
		}
		return JobChunkExtensions.ScheduleParallel(new InvertBrushesJob
		{
			m_BrushType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Brush_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		}, group, inputDeps);
	}

	protected virtual bool GetAllowApply()
	{
		if (m_ToolSystem.ignoreErrors || m_ErrorQuery.IsEmptyIgnoreFilter)
		{
			return !m_OriginalDeletedSystem.GetOriginalDeletedResult(0);
		}
		return false;
	}

	protected bool GetRaycastResult(out Entity entity, out RaycastHit hit)
	{
		if (m_ToolRaycastSystem.GetRaycastResult(out var result) && !base.EntityManager.HasComponent<Deleted>(result.m_Owner))
		{
			entity = result.m_Owner;
			hit = result.m_Hit;
			return true;
		}
		entity = Entity.Null;
		hit = default(RaycastHit);
		return false;
	}

	protected bool GetRaycastResult(out Entity entity, out RaycastHit hit, out bool forceUpdate)
	{
		forceUpdate = m_OriginalDeletedSystem.GetOriginalDeletedResult(1) || m_ForceUpdate;
		return GetRaycastResult(out entity, out hit);
	}

	protected virtual bool GetRaycastResult(out ControlPoint controlPoint)
	{
		if (GetRaycastResult(out Entity entity, out RaycastHit hit))
		{
			controlPoint = new ControlPoint(entity, hit);
			return true;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	protected virtual bool GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate)
	{
		if (GetRaycastResult(out var entity, out var hit, out forceUpdate))
		{
			controlPoint = new ControlPoint(entity, hit);
			return true;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	protected bool GetContainers(EntityQuery group, out Entity laneContainer, out Entity transformContainer)
	{
		laneContainer = Entity.Null;
		transformContainer = Entity.Null;
		if (group.IsEmptyIgnoreFilter)
		{
			return false;
		}
		NativeArray<Entity> nativeArray = group.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity entity = nativeArray[i];
			if (base.EntityManager.HasComponent<NetData>(entity))
			{
				laneContainer = entity;
			}
			else if (base.EntityManager.HasComponent<ObjectData>(entity))
			{
				transformContainer = entity;
			}
		}
		nativeArray.Dispose();
		return true;
	}

	protected BrushPrefab FindDefaultBrush(EntityQuery query)
	{
		BrushPrefab result = null;
		int num = int.MaxValue;
		ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<BrushData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BrushData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		NativeArray<ArchetypeChunk> nativeArray = query.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<BrushData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					BrushData brushData = nativeArray3[j];
					if (brushData.m_Priority < num)
					{
						result = m_PrefabSystem.GetPrefab<BrushPrefab>(nativeArray2[j]);
						num = brushData.m_Priority;
					}
				}
			}
			return result;
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	protected void EnsureCachedBrushData()
	{
		if (!(brushType != null))
		{
			return;
		}
		Entity entity = m_PrefabSystem.GetEntity(brushType);
		BrushData componentData = base.EntityManager.GetComponentData<BrushData>(entity);
		if (math.all(componentData.m_Resolution != 0) || brushType.m_Texture == null || brushType.m_Texture.width == 0 || brushType.m_Texture.height == 0)
		{
			return;
		}
		int2 @int = (componentData.m_Resolution = new int2(brushType.m_Texture.width, brushType.m_Texture.height));
		int num = 1;
		float num2 = 1f;
		while (math.any(componentData.m_Resolution > 128) && math.all(componentData.m_Resolution > 1))
		{
			componentData.m_Resolution /= 2;
			num *= 2;
			num2 *= 0.25f;
		}
		base.EntityManager.SetComponentData(entity, componentData);
		DynamicBuffer<BrushCell> buffer = base.EntityManager.GetBuffer<BrushCell>(entity);
		UnityEngine.Color[] pixels = brushType.m_Texture.GetPixels();
		buffer.ResizeUninitialized(componentData.m_Resolution.x * componentData.m_Resolution.y);
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < componentData.m_Resolution.y; i++)
		{
			for (int j = 0; j < componentData.m_Resolution.x; j++)
			{
				BrushCell value = default(BrushCell);
				int num5 = num3;
				for (int k = 0; k < num; k++)
				{
					for (int l = 0; l < num; l++)
					{
						value.m_Opacity += pixels[num5++].a;
					}
					num5 += @int.x - num;
				}
				value.m_Opacity *= num2;
				buffer[num4++] = value;
				num3 += num;
			}
			num3 += @int.x * (num - 1);
		}
	}

	protected EntityQuery GetDefinitionQuery()
	{
		return GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.Exclude<Updated>());
	}

	protected EntityQuery GetContainerQuery()
	{
		return GetEntityQuery(ComponentType.ReadOnly<EditorContainerData>());
	}

	protected EntityQuery GetBrushQuery()
	{
		return GetEntityQuery(ComponentType.ReadOnly<BrushData>());
	}

	protected void SetAction(ref IProxyAction action, IProxyAction newAction)
	{
		if (newAction != action)
		{
			if (action == null)
			{
				action = newAction;
			}
			else if (newAction == null)
			{
				action.shouldBeEnabled = false;
				action = null;
			}
			else
			{
				newAction.shouldBeEnabled = action.shouldBeEnabled;
				action.shouldBeEnabled = false;
				action = newAction;
			}
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
	protected ToolBaseSystem()
	{
	}
}
