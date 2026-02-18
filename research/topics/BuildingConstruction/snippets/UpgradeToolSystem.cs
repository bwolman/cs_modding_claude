using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class UpgradeToolSystem : ObjectToolBaseSystem
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PlaceableObjectData>(true);
		}
	}

	[CompilerGenerated]
	private sealed class <get_toolActions>d__19 : System.Collections.Generic.IEnumerable<IProxyAction>, System.Collections.IEnumerable, System.Collections.Generic.IEnumerator<IProxyAction>, System.Collections.IEnumerator, System.IDisposable
	{
		private int <>1__state;

		private IProxyAction <>2__current;

		private int <>l__initialThreadId;

		public UpgradeToolSystem <>4__this;

		IProxyAction System.Collections.Generic.IEnumerator<IProxyAction>.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		object System.Collections.IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		[DebuggerHidden]
		public <get_toolActions>d__19(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void System.IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			int num = <>1__state;
			UpgradeToolSystem upgradeToolSystem = <>4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
				<>1__state = -1;
				<>2__current = upgradeToolSystem.m_PlaceUpgrade;
				<>1__state = 1;
				return true;
			case 1:
				<>1__state = -1;
				<>2__current = upgradeToolSystem.m_Rebuild;
				<>1__state = 2;
				return true;
			case 2:
				<>1__state = -1;
				return false;
			}
		}

		bool System.Collections.IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void System.Collections.IEnumerator.Reset()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		System.Collections.Generic.IEnumerator<IProxyAction> System.Collections.Generic.IEnumerable<IProxyAction>.GetEnumerator()
		{
			<get_toolActions>d__19 result;
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				result = this;
			}
			else
			{
				result = new <get_toolActions>d__19(0)
				{
					<>4__this = <>4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return (System.Collections.IEnumerator)((System.Collections.Generic.IEnumerable<IProxyAction>)this).GetEnumerator();
		}
	}

	public const string kToolID = "Upgrade Tool";

	private CityConfigurationSystem m_CityConfigurationSystem;

	private AudioManager m_AudioManager;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_ContainerQuery;

	private Entity m_UpgradingObject;

	private NativeList<ControlPoint> m_ControlPoints;

	private RandomSeed m_RandomSeed;

	private bool m_AlreadyCreated;

	private ObjectPrefab m_Prefab;

	private IProxyAction m_PlaceUpgrade;

	private IProxyAction m_Rebuild;

	private TypeHandle __TypeHandle;

	public override string toolID => "Upgrade Tool";

	public ObjectPrefab prefab
	{
		get
		{
			return m_Prefab;
		}
		set
		{
			if ((Object)(object)value != (Object)(object)m_Prefab)
			{
				m_Prefab = value;
				m_ForceUpdate = true;
			}
		}
	}

	private protected override System.Collections.Generic.IEnumerable<IProxyAction> toolActions
	{
		[IteratorStateMachine(typeof(<get_toolActions>d__19))]
		get
		{
			yield return m_PlaceUpgrade;
			yield return m_Rebuild;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_CityConfigurationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_AudioManager = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<AudioManager>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_ContainerQuery = GetContainerQuery();
		m_SoundQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<ToolUXSoundSettingsData>() });
		m_PlaceUpgrade = InputManager.instance.toolActionCollection.GetActionState("Place Upgrade", "UpgradeToolSystem");
		m_Rebuild = InputManager.instance.toolActionCollection.GetActionState("Rebuild", "UpgradeToolSystem");
		m_ControlPoints = new NativeList<ControlPoint>(1, AllocatorHandle.op_Implicit((Allocator)4));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ControlPoints.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ControlPoints.Clear();
		m_RandomSeed = RandomSeed.Next();
		m_AlreadyCreated = false;
		base.requireZones = true;
		base.requireAreas = AreaTypeMask.Lots;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			base.applyActionOverride = (((Object)(object)prefab != (Object)null) ? m_PlaceUpgrade : m_Rebuild);
			base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApply();
			base.cancelActionOverride = m_MouseCancel;
			base.cancelAction.shouldBeEnabled = base.actionsEnabled;
		}
	}

	public override PrefabBase GetPrefab()
	{
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (!m_ToolSystem.actionMode.IsEditor() && prefab is ObjectPrefab objectPrefab && prefab.Has<Game.Prefabs.ServiceUpgrade>())
		{
			Entity entity = m_PrefabSystem.GetEntity(prefab);
			if (InternalCompilerInterface.HasComponentAfterCompletingDependency<PlaceableObjectData>(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef, entity))
			{
				return false;
			}
			this.prefab = objectPrefab;
			return true;
		}
		return false;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		m_UpgradingObject = m_ToolSystem.selected;
		EntityManager entityManager;
		if ((Object)(object)prefab != (Object)null)
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (!((EntityManager)(ref entityManager)).HasBuffer<InstalledUpgrade>(m_UpgradingObject))
			{
				m_UpgradingObject = Entity.Null;
			}
			if (m_PrefabSystem.TryGetComponentData<BuildingExtensionData>((PrefabBase)prefab, out BuildingExtensionData component) && component.m_HasUndergroundElements)
			{
				base.requireNet |= Layer.Road;
			}
			UpdateInfoview(m_PrefabSystem.GetEntity(prefab));
		}
		else
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (!((EntityManager)(ref entityManager)).HasComponent<Destroyed>(m_UpgradingObject))
			{
				m_UpgradingObject = Entity.Null;
			}
			UpdateInfoview(Entity.Null);
		}
		GetAvailableSnapMask(out m_SnapOnMask, out m_SnapOffMask);
		UpdateActions();
		if (m_UpgradingObject != Entity.Null && !m_ToolSystem.fullUpdateRequired && (m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
		{
			if (base.cancelAction.WasPressedThisFrame())
			{
				return Cancel(inputDeps);
			}
			if (base.applyAction.WasPressedThisFrame())
			{
				return Apply(inputDeps);
			}
			return Update(inputDeps);
		}
		return Clear(inputDeps);
	}

	private JobHandle Cancel(JobHandle inputDeps)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		m_ToolSystem.activeTool = m_DefaultToolSystem;
		base.applyMode = ApplyMode.Clear;
		m_AlreadyCreated = false;
		return inputDeps;
	}

	private JobHandle Apply(JobHandle inputDeps)
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		if (GetAllowApply())
		{
			m_ToolSystem.activeTool = m_DefaultToolSystem;
			base.applyMode = ApplyMode.Apply;
			m_RandomSeed = RandomSeed.Next();
			m_AlreadyCreated = false;
			m_AudioManager.PlayUISound(((EntityQuery)(ref m_SoundQuery)).GetSingleton<ToolUXSoundSettingsData>().m_PlaceUpgradeSound);
			if (m_ToolSystem.actionMode.IsGame() && (Object)(object)prefab != (Object)null)
			{
				EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
				Transform componentData = ((EntityManager)(ref entityManager)).GetComponentData<Transform>(m_UpgradingObject);
				Telemetry.PlaceBuilding(m_UpgradingObject, prefab, componentData.m_Position);
			}
			return inputDeps;
		}
		m_AudioManager.PlayUISound(((EntityQuery)(ref m_SoundQuery)).GetSingleton<ToolUXSoundSettingsData>().m_PlaceBuildingFailSound);
		return Update(inputDeps);
	}

	private JobHandle Update(JobHandle inputDeps)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (m_ToolSystem.selected == Entity.Null)
		{
			base.applyMode = ApplyMode.Clear;
			m_AlreadyCreated = false;
			return inputDeps;
		}
		if (m_AlreadyCreated && !m_ForceUpdate)
		{
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		base.applyMode = ApplyMode.Clear;
		m_AlreadyCreated = true;
		return CreateTempObject(inputDeps);
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		base.applyMode = ApplyMode.Clear;
		m_AlreadyCreated = false;
		return inputDeps;
	}

	private JobHandle CreateTempObject(JobHandle inputDeps)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		Transform componentData = ((EntityManager)(ref entityManager)).GetComponentData<Transform>(m_UpgradingObject);
		ControlPoint controlPoint = new ControlPoint
		{
			m_Position = componentData.m_Position,
			m_Rotation = componentData.m_Rotation
		};
		if ((Object)(object)prefab != (Object)null && m_PrefabSystem.HasComponent<BuildingExtensionData>(prefab))
		{
			BuildingExtensionData componentData2 = m_PrefabSystem.GetComponentData<BuildingExtensionData>((PrefabBase)prefab);
			controlPoint.m_Position = ObjectUtils.LocalToWorld(componentData, componentData2.m_Position);
		}
		m_ControlPoints.Clear();
		m_ControlPoints.Add(ref controlPoint);
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		JobHandle val = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (m_UpgradingObject != Entity.Null)
		{
			Entity objectPrefab = Entity.Null;
			if ((Object)(object)prefab != (Object)null)
			{
				objectPrefab = m_PrefabSystem.GetEntity(prefab);
			}
			Entity laneContainer = Entity.Null;
			if (m_ToolSystem.actionMode.IsEditor())
			{
				GetContainers(m_ContainerQuery, out laneContainer, out var _);
			}
			val = JobHandle.CombineDependencies(val, CreateDefinitions(objectPrefab, Entity.Null, Entity.Null, m_UpgradingObject, Entity.Null, laneContainer, m_CityConfigurationSystem.defaultTheme, m_ControlPoints, default(NativeReference<AttachmentData>), m_ToolSystem.actionMode.IsEditor(), m_CityConfigurationSystem.leftHandTraffic, removing: false, stamping: false, base.brushSize, math.radians(base.brushAngle), base.brushStrength, 0f, Time.deltaTime, m_RandomSeed, GetActualSnap(), AgeMask.Sapling, inputDeps));
		}
		return val;
	}

	[MethodImpl((MethodImplOptions)256)]
	private void __AssignQueries(ref SystemState state)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		((EntityQueryBuilder)(ref val)).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public UpgradeToolSystem()
	{
	}
}
