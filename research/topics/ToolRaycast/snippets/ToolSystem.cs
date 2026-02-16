using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Serialization.Entities;
using Game.Input;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Serialization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ToolSystem : GameSystemBase, IPreDeserialize
{
	protected const string kToolKeyGroup = "tool";

	protected const string kToolCancelKeyGroup = "tool cancel";

	protected const string kToolApplyKeyAction = "tool apply";

	protected const string kToolCancelKeyAction = "tool cancel";

	public Action<ToolBaseSystem> EventToolChanged;

	public Action<PrefabBase> EventPrefabChanged;

	public Action<InfoviewPrefab, InfoviewPrefab> EventInfoviewChanged;

	public Action EventInfomodesChanged;

	private ToolBaseSystem m_ActiveTool;

	private Entity m_Selected;

	private ToolBaseSystem m_LastTool;

	private InfoviewPrefab m_CurrentInfoview;

	private InfoviewPrefab m_LastToolInfoview;

	private PrefabSystem m_PrefabSystem;

	private UpdateSystem m_UpdateSystem;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private List<ToolBaseSystem> m_Tools;

	private List<InfomodePrefab> m_LastToolInfomodes;

	private Dictionary<InfoviewPrefab, List<InfomodeInfo>> m_InfomodeMap;

	private Vector4[] m_InfomodeColors;

	private Vector4[] m_InfomodeParams;

	private int[] m_InfomodeCounts;

	private NativeList<Entity> m_Infomodes;

	private NativeArray<Vector4> m_ActiveInfoviewColors;

	private float m_InfoviewTimer;

	private bool m_FullUpdateRequired;

	private bool m_InfoviewUpdateRequired;

	private bool m_IsUpdating;

	private InputBarrier m_ToolActionBarrier;

	private Dictionary<ProxyAction, InputBarrier> m_MouseToolBarriers;

	public ToolBaseSystem activeTool
	{
		get
		{
			return m_ActiveTool;
		}
		set
		{
			if (value != m_ActiveTool)
			{
				m_ActiveTool = value;
				RequireFullUpdate();
				EventToolChanged?.Invoke(value);
			}
		}
	}

	public Entity selected
	{
		get
		{
			return m_Selected;
		}
		set
		{
			m_Selected = value;
		}
	}

	public int selectedIndex { get; set; } = -1;

	[CanBeNull]
	public PrefabBase activePrefab => m_ActiveTool.GetPrefab();

	public InfoviewPrefab infoview
	{
		get
		{
			return m_CurrentInfoview;
		}
		set
		{
			if (value != m_CurrentInfoview)
			{
				SetInfoview(value, null);
			}
		}
	}

	public InfoviewPrefab activeInfoview
	{
		get
		{
			if (!(m_CurrentInfoview != null) || !m_CurrentInfoview.isValid)
			{
				return null;
			}
			return m_CurrentInfoview;
		}
	}

	public GameMode actionMode { get; private set; } = GameMode.Other;

	public ApplyMode applyMode
	{
		get
		{
			if (m_LastTool == null)
			{
				return ApplyMode.None;
			}
			return m_LastTool.applyMode;
		}
	}

	public bool ignoreErrors { get; set; }

	public bool fullUpdateRequired { get; private set; }

	public List<ToolBaseSystem> tools
	{
		get
		{
			if (m_Tools == null)
			{
				m_Tools = new List<ToolBaseSystem>();
			}
			return m_Tools;
		}
	}

	public NativeArray<Vector4> GetActiveInfoviewColors()
	{
		return m_ActiveInfoviewColors;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		actionMode = mode;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		activeTool = m_DefaultToolSystem;
		m_LastToolInfomodes = new List<InfomodePrefab>();
		m_InfomodeMap = new Dictionary<InfoviewPrefab, List<InfomodeInfo>>();
		m_InfomodeColors = new Vector4[303];
		m_InfomodeParams = new Vector4[101];
		m_InfomodeCounts = new int[3];
		m_Infomodes = new NativeList<Entity>(10, Allocator.Persistent);
		m_ActiveInfoviewColors = new NativeArray<Vector4>(m_InfomodeColors.Length, Allocator.Persistent);
		m_ToolActionBarrier = Game.Input.InputManager.instance.CreateMapBarrier("Tool", "ToolSystem");
		m_ToolActionBarrier.blocked = true;
		m_MouseToolBarriers = Game.Input.InputManager.instance.FindActionMap("Tool").actions.Values.Where((ProxyAction a) => a.isMouseAction).ToDictionary((ProxyAction i) => i, (ProxyAction i) => new InputBarrier("Mouse Tool", i, Game.Input.InputManager.DeviceType.Mouse));
		foreach (KeyValuePair<ProxyAction, InputBarrier> item in m_MouseToolBarriers)
		{
			var (action, _) = (KeyValuePair<ProxyAction, InputBarrier>)(ref item);
			action.onInteraction += delegate(ProxyAction _, InputActionPhase phase)
			{
				if (phase == InputActionPhase.Canceled && m_MouseToolBarriers.TryGetValue(action, out var value))
				{
					value.blocked = ShouldBlockBarrier(Game.Input.InputManager.instance.activeControlScheme, Game.Input.InputManager.instance.mouseOverUI);
				}
			};
		}
		Game.Input.InputManager.instance.EventControlSchemeChanged += delegate(Game.Input.InputManager.ControlScheme activeControlScheme)
		{
			RefreshInputBarrier(activeControlScheme, Game.Input.InputManager.instance.mouseOverUI);
		};
		Game.Input.InputManager.instance.EventMouseOverUIChanged += delegate(bool mouseOverUI)
		{
			RefreshInputBarrier(Game.Input.InputManager.instance.activeControlScheme, mouseOverUI);
		};
		Shader.SetGlobalInt("colossal_InfoviewOn", 0);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		ClearInfomodes();
		m_CurrentInfoview = null;
		Shader.SetGlobalInt("colossal_InfoviewOn", 0);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Infomodes.Dispose();
		m_ActiveInfoviewColors.Dispose();
		Shader.SetGlobalInt("colossal_InfoviewOn", 0);
		m_ToolActionBarrier.Dispose();
		foreach (KeyValuePair<ProxyAction, InputBarrier> item in m_MouseToolBarriers)
		{
			item.Deconstruct(out var _, out var value);
			value.Dispose();
		}
		m_MouseToolBarriers = null;
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_ToolActionBarrier.blocked = !GameManager.instance.gameMode.IsGameOrEditor() || GameManager.instance.isGameLoading;
		m_IsUpdating = true;
		m_UpdateSystem.Update(SystemUpdatePhase.PreTool);
		ToolUpdate();
		m_UpdateSystem.Update(SystemUpdatePhase.PostTool);
		fullUpdateRequired = m_FullUpdateRequired;
		m_FullUpdateRequired = false;
		m_IsUpdating = false;
	}

	public bool ActivatePrefabTool([CanBeNull] PrefabBase prefab)
	{
		if (prefab != null)
		{
			foreach (ToolBaseSystem tool in tools)
			{
				if (tool.TrySetPrefab(prefab))
				{
					activeTool = tool;
					return true;
				}
			}
		}
		activeTool = m_DefaultToolSystem;
		return false;
	}

	private void RefreshInputBarrier(Game.Input.InputManager.ControlScheme activeControlScheme, bool mouseOverUI)
	{
		bool flag = ShouldBlockBarrier(activeControlScheme, mouseOverUI);
		foreach (KeyValuePair<ProxyAction, InputBarrier> item in m_MouseToolBarriers)
		{
			item.Deconstruct(out var key, out var value);
			ProxyAction proxyAction = key;
			value.blocked = flag && !proxyAction.IsInProgress();
		}
	}

	private bool ShouldBlockBarrier(Game.Input.InputManager.ControlScheme activeControlScheme, bool mouseOverUI)
	{
		return activeControlScheme == Game.Input.InputManager.ControlScheme.KeyboardAndMouse && mouseOverUI;
	}

	public void RequireFullUpdate()
	{
		if (m_IsUpdating)
		{
			m_FullUpdateRequired = true;
		}
		else
		{
			fullUpdateRequired = true;
		}
	}

	private void ToolUpdate()
	{
		m_InfoviewTimer += UnityEngine.Time.deltaTime;
		m_InfoviewTimer %= 60f;
		if (activeTool != m_LastTool)
		{
			if (m_LastTool != null)
			{
				m_LastTool.Enabled = false;
				m_LastTool.Update();
			}
			m_LastTool = activeTool;
		}
		InfoviewPrefab infoviewPrefab = null;
		List<InfomodePrefab> list = null;
		if (m_LastTool != null)
		{
			m_LastTool.Enabled = true;
		}
		m_UpdateSystem.Update(SystemUpdatePhase.ToolUpdate);
		if (m_LastTool != null)
		{
			infoviewPrefab = m_LastTool.infoview;
			list = m_LastTool.infomodes;
		}
		if (infoviewPrefab != m_LastToolInfoview)
		{
			SetInfoview(infoviewPrefab, list);
			m_LastToolInfoview = infoviewPrefab;
			m_LastToolInfomodes.Clear();
			if (list != null)
			{
				m_LastToolInfomodes.AddRange(list);
			}
		}
		else if (infoviewPrefab != null && infoviewPrefab == activeInfoview)
		{
			if ((list != null && list.Count != 0) || m_LastToolInfomodes.Count != 0)
			{
				List<InfomodeInfo> infomodes = GetInfomodes(infoviewPrefab);
				for (int i = 0; i < infomodes.Count; i++)
				{
					InfomodeInfo infomodeInfo = infomodes[i];
					if (infomodeInfo.m_Supplemental || (infomodeInfo.m_Optional && actionMode.IsGame()))
					{
						bool num = m_LastToolInfomodes.Contains(infomodeInfo.m_Mode);
						bool flag = list?.Contains(infomodeInfo.m_Mode) ?? false;
						if (num != flag)
						{
							Entity entity = m_PrefabSystem.GetEntity(infomodeInfo.m_Mode);
							SetInfomodeActive(entity, flag, infomodeInfo.m_Priority);
						}
					}
				}
			}
			m_LastToolInfomodes.Clear();
			if (list != null)
			{
				m_LastToolInfomodes.AddRange(list);
			}
		}
		if (m_InfoviewUpdateRequired)
		{
			m_InfoviewUpdateRequired = false;
			UpdateInfoviewColors();
		}
		Shader.SetGlobalFloat("colossal_InfoviewTime", m_InfoviewTimer);
	}

	private void SetInfoview(InfoviewPrefab value, List<InfomodePrefab> infomodes)
	{
		InfoviewPrefab arg = activeInfoview;
		m_CurrentInfoview = value;
		ClearInfomodes();
		if (activeInfoview != null)
		{
			List<InfomodeInfo> infomodes2 = GetInfomodes(value);
			for (int i = 0; i < infomodes2.Count; i++)
			{
				InfomodeInfo infomodeInfo = infomodes2[i];
				if ((!infomodeInfo.m_Supplemental || (infomodes != null && infomodes.Contains(infomodeInfo.m_Mode))) && (!infomodeInfo.m_Optional || !actionMode.IsGame() || infomodes == null || infomodes.Contains(infomodeInfo.m_Mode)))
				{
					Entity value2 = m_PrefabSystem.GetEntity(infomodeInfo.m_Mode);
					Activate(value2, infomodeInfo.m_Mode, infomodeInfo.m_Priority);
					m_Infomodes.Add(in value2);
				}
			}
		}
		m_InfoviewTimer = 0f;
		m_InfoviewUpdateRequired = true;
		EventInfoviewChanged?.Invoke(value, arg);
	}

	private void ClearInfomodes()
	{
		for (int i = 0; i < m_InfomodeCounts.Length; i++)
		{
			m_InfomodeCounts[i] = 0;
		}
		base.EntityManager.RemoveComponent<InfomodeActive>(m_Infomodes.AsArray());
		m_Infomodes.Clear();
	}

	private void UpdateInfoviewColors()
	{
		if (activeInfoview != null)
		{
			m_InfomodeColors[0] = activeInfoview.m_DefaultColor.linear;
			m_InfomodeColors[1] = activeInfoview.m_DefaultColor.linear;
			m_InfomodeColors[2] = activeInfoview.m_SecondaryColor.linear;
			m_InfomodeParams[0] = new Vector4(1f, 0f, 0f, 0f);
			for (int i = 0; i < m_Infomodes.Length; i++)
			{
				Entity entity = m_Infomodes[i];
				InfomodePrefab prefab = m_PrefabSystem.GetPrefab<InfomodePrefab>(entity);
				InfomodeActive componentData = base.EntityManager.GetComponentData<InfomodeActive>(entity);
				prefab.GetColors(out var color, out var color2, out var color3, out var steps, out var speed, out var tiling, out var fill);
				m_InfomodeColors[componentData.m_Index * 3] = color.linear;
				m_InfomodeColors[componentData.m_Index * 3 + 1] = color2.linear;
				m_InfomodeColors[componentData.m_Index * 3 + 2] = color3.linear;
				m_InfomodeParams[componentData.m_Index] = new Vector4(steps, speed, tiling, fill);
				if (componentData.m_SecondaryIndex != -1)
				{
					m_InfomodeColors[componentData.m_SecondaryIndex * 3] = color.linear;
					m_InfomodeColors[componentData.m_SecondaryIndex * 3 + 1] = color2.linear;
					m_InfomodeColors[componentData.m_SecondaryIndex * 3 + 2] = color3.linear;
					m_InfomodeParams[componentData.m_SecondaryIndex] = new Vector4(steps, speed, tiling, fill);
				}
			}
			for (int j = 0; j < m_InfomodeCounts.Length; j++)
			{
				for (int k = m_InfomodeCounts[j]; k < 4; k++)
				{
					int num = 1 + j * 4 + k;
					m_InfomodeColors[num * 3] = default(Vector4);
					m_InfomodeColors[num * 3 + 1] = default(Vector4);
					m_InfomodeColors[num * 3 + 2] = default(Vector4);
					m_InfomodeParams[num] = new Vector4(1f, 0f, 0f, 0f);
				}
			}
			m_ActiveInfoviewColors.CopyFrom(m_InfomodeColors);
			Shader.SetGlobalInt("colossal_InfoviewOn", 1);
			Shader.SetGlobalVectorArray("colossal_InfomodeColors", m_InfomodeColors);
			Shader.SetGlobalVectorArray("colossal_InfomodeParams", m_InfomodeParams);
		}
		else
		{
			Shader.SetGlobalInt("colossal_InfoviewOn", 0);
		}
	}

	[CanBeNull]
	public List<InfomodeInfo> GetInfomodes(InfoviewPrefab infoview)
	{
		if (infoview == null)
		{
			return null;
		}
		if (!m_InfomodeMap.TryGetValue(infoview, out var value))
		{
			value = new List<InfomodeInfo>();
			DynamicBuffer<InfoviewMode> buffer = m_PrefabSystem.GetBuffer<InfoviewMode>(infoview, isReadOnly: true);
			for (int i = 0; i < buffer.Length; i++)
			{
				InfoviewMode infoviewMode = buffer[i];
				InfomodeInfo item = new InfomodeInfo
				{
					m_Mode = m_PrefabSystem.GetPrefab<InfomodePrefab>(infoviewMode.m_Mode),
					m_Priority = infoviewMode.m_Priority,
					m_Supplemental = infoviewMode.m_Supplemental,
					m_Optional = infoviewMode.m_Optional
				};
				value.Add(item);
			}
			value.Sort();
			m_InfomodeMap.Add(infoview, value);
		}
		return value;
	}

	public List<InfomodeInfo> GetInfoviewInfomodes()
	{
		return GetInfomodes(activeInfoview);
	}

	public bool IsInfomodeActive(InfomodePrefab prefab)
	{
		Entity entity = m_PrefabSystem.GetEntity(prefab);
		return base.EntityManager.HasComponent<InfomodeActive>(entity);
	}

	public void SetInfomodeActive(InfomodePrefab prefab, bool active, int priority)
	{
		Entity entity = m_PrefabSystem.GetEntity(prefab);
		SetInfomodeActive(entity, active, priority);
	}

	public void SetInfomodeActive(Entity entity, bool active, int priority)
	{
		if (!base.EntityManager.Exists(entity))
		{
			return;
		}
		if (!active)
		{
			int num = m_Infomodes.IndexOf(entity);
			if (num >= 0)
			{
				InfomodeActive componentData = base.EntityManager.GetComponentData<InfomodeActive>(entity);
				InfomodePrefab prefab = m_PrefabSystem.GetPrefab<InfomodePrefab>(entity);
				Deactivate(entity, prefab, componentData);
				m_Infomodes.RemoveAtSwapBack(num);
				m_InfoviewUpdateRequired = true;
				EventInfomodesChanged?.Invoke();
			}
		}
		else
		{
			if (m_Infomodes.Contains(entity))
			{
				return;
			}
			InfomodePrefab prefab2 = m_PrefabSystem.GetPrefab<InfomodePrefab>(entity);
			int secondaryGroup;
			int colorGroup = prefab2.GetColorGroup(out secondaryGroup);
			bool flag = false;
			for (int i = 0; i < m_Infomodes.Length; i++)
			{
				Entity entity2 = m_Infomodes[i];
				InfomodePrefab prefab3 = m_PrefabSystem.GetPrefab<InfomodePrefab>(entity2);
				int secondaryGroup2;
				int colorGroup2 = prefab3.GetColorGroup(out secondaryGroup2);
				if ((colorGroup2 == colorGroup || colorGroup2 == secondaryGroup || secondaryGroup2 == colorGroup || (secondaryGroup2 == secondaryGroup && secondaryGroup2 != -1)) && !prefab2.CanActivateBoth(prefab3))
				{
					InfomodeActive componentData2 = base.EntityManager.GetComponentData<InfomodeActive>(entity2);
					Deactivate(entity2, prefab3, componentData2);
					m_Infomodes[i] = entity;
					flag = true;
					break;
				}
			}
			Activate(entity, prefab2, priority);
			if (!flag)
			{
				m_Infomodes.Add(in entity);
			}
			m_InfoviewUpdateRequired = true;
			EventInfomodesChanged?.Invoke();
		}
	}

	private void Activate(Entity entity, InfomodePrefab prefab, int priority)
	{
		int secondaryGroup;
		int colorGroup = prefab.GetColorGroup(out secondaryGroup);
		int index = colorGroup * 4 + ++m_InfomodeCounts[colorGroup];
		int secondaryIndex = -1;
		if (secondaryGroup != -1)
		{
			secondaryIndex = secondaryGroup * 4 + ++m_InfomodeCounts[secondaryGroup];
		}
		base.EntityManager.AddComponentData(entity, new InfomodeActive(priority, index, secondaryIndex));
	}

	private void Deactivate(Entity entity, InfomodePrefab prefab, InfomodeActive infomodeActive)
	{
		int secondaryGroup;
		int colorGroup = prefab.GetColorGroup(out secondaryGroup);
		Deactivate(colorGroup, infomodeActive.m_Index);
		if (secondaryGroup != -1)
		{
			Deactivate(secondaryGroup, infomodeActive.m_SecondaryIndex);
		}
		base.EntityManager.RemoveComponent<InfomodeActive>(entity);
	}

	private void Deactivate(int colorGroup, int activeIndex)
	{
		int num = colorGroup * 4 + m_InfomodeCounts[colorGroup]--;
		if (activeIndex >= num)
		{
			return;
		}
		for (int i = 0; i < m_Infomodes.Length; i++)
		{
			Entity entity = m_Infomodes[i];
			InfomodeActive componentData = base.EntityManager.GetComponentData<InfomodeActive>(entity);
			if (componentData.m_Index == num)
			{
				componentData.m_Index = activeIndex;
				base.EntityManager.SetComponentData(entity, componentData);
				break;
			}
			if (componentData.m_SecondaryIndex == num)
			{
				componentData.m_SecondaryIndex = activeIndex;
				base.EntityManager.SetComponentData(entity, componentData);
				break;
			}
		}
	}

	public void PreDeserialize(Context context)
	{
		ClearInfomodes();
		activeTool = m_DefaultToolSystem;
	}

	[Preserve]
	public ToolSystem()
	{
	}
}
