using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Audio;
using Game.City;
using Game.Common;
using Game.Input;
using Game.PSI;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Serialization;
using Game.Settings;
using Game.Simulation;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialSystem : GameSystemBase, ITutorialSystem, IPreDeserialize
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<TutorialCompleted> __Game_Tutorials_TutorialCompleted_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TutorialAlternative> __Game_Tutorials_TutorialAlternative_RO_BufferLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			__Game_Tutorials_TutorialCompleted_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<TutorialCompleted>(true);
			__Game_Tutorials_TutorialAlternative_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<TutorialAlternative>(true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private AudioManager m_AudioManager;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private MapTilePurchaseSystem m_MapTilePurchaseSystem;

	private GameScreenUISystem m_GameScreenUISystem;

	private static readonly float kBalloonCompletionDelay = 0f;

	private static readonly float kCompletionDelay = 1.5f;

	private static readonly float kActivationDelay = 3f;

	private static readonly string kWelcomeIntroKey = "WelcomeIntro";

	protected static readonly string kListIntroKey = "ListIntro";

	private static readonly string kListOutroKey = "ListOutro";

	private EntityQuery m_TutorialConfigurationQuery;

	protected EntityQuery m_TutorialQuery;

	private EntityQuery m_TutorialListQuery;

	private EntityQuery m_TutorialPhaseQuery;

	private EntityQuery m_ActiveTutorialListQuery;

	protected EntityQuery m_ActiveTutorialQuery;

	private EntityQuery m_ActiveTutorialPhaseQuery;

	protected EntityQuery m_PendingTutorialListQuery;

	protected EntityQuery m_PendingTutorialQuery;

	protected EntityQuery m_PendingPriorityTutorialQuery;

	protected EntityQuery m_LockedTutorialQuery;

	private EntityQuery m_LockedTutorialPhaseQuery;

	private EntityQuery m_LockedTutorialTriggerQuery;

	private EntityQuery m_LockedTutorialListQuery;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_ForceAdvisorQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private float m_AccumulatedDelay;

	protected TutorialMode m_Mode;

	protected Setting m_Setting = SharedSettings.instance.userState;

	private TypeHandle __TypeHandle;

	protected virtual Dictionary<string, bool> ShownTutorials => SharedSettings.instance.userState.shownTutorials;

	public TutorialMode mode
	{
		get
		{
			return m_Mode;
		}
		set
		{
			if (value != m_Mode)
			{
				if (m_Mode == TutorialMode.Intro)
				{
					UpdateSettings(kWelcomeIntroKey, passed: true);
				}
				else if (m_Mode == TutorialMode.ListIntro)
				{
					UpdateSettings(kListIntroKey, passed: true);
				}
				else if (m_Mode == TutorialMode.ListOutro)
				{
					UpdateSettings(kListOutroKey, passed: true);
				}
			}
			m_Mode = value;
		}
	}

	public Entity activeTutorial
	{
		get
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (!((EntityQuery)(ref m_ActiveTutorialQuery)).IsEmptyIgnoreFilter)
			{
				return ((EntityQuery)(ref m_ActiveTutorialQuery)).GetSingletonEntity();
			}
			return Entity.Null;
		}
	}

	public Entity activeTutorialPhase
	{
		get
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (!((EntityQuery)(ref m_ActiveTutorialPhaseQuery)).IsEmptyIgnoreFilter)
			{
				return ((EntityQuery)(ref m_ActiveTutorialPhaseQuery)).GetSingletonEntity();
			}
			return Entity.Null;
		}
	}

	public virtual bool tutorialEnabled
	{
		get
		{
			return SharedSettings.instance.gameplay.showTutorials;
		}
		set
		{
			SharedSettings.instance.gameplay.showTutorials = value;
			if (!value)
			{
				mode = TutorialMode.Default;
			}
		}
	}

	public Entity activeTutorialList
	{
		get
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (!((EntityQuery)(ref m_ActiveTutorialListQuery)).IsEmptyIgnoreFilter)
			{
				return ((EntityQuery)(ref m_ActiveTutorialListQuery)).GetSingletonEntity();
			}
			return Entity.Null;
		}
	}

	public Entity tutorialPending => FindNextTutorial();

	public Entity nextListTutorial
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			Entity val = activeTutorialList;
			if (val != Entity.Null)
			{
				EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
				DynamicBuffer<TutorialRef> buffer = ((EntityManager)(ref entityManager)).GetBuffer<TutorialRef>(val, true);
				ComponentLookup<TutorialCompleted> componentLookup = ((SystemBase)this).GetComponentLookup<TutorialCompleted>(true);
				BufferLookup<TutorialAlternative> bufferLookup = ((SystemBase)this).GetBufferLookup<TutorialAlternative>(true);
				Enumerator<TutorialRef> enumerator = buffer.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						TutorialRef current = enumerator.Current;
						if (!IsCompleted(current.m_Tutorial, bufferLookup, componentLookup))
						{
							entityManager = ((ComponentSystemBase)this).EntityManager;
							if (!((EntityManager)(ref entityManager)).HasComponent<TutorialActive>(current.m_Tutorial))
							{
								return current.m_Tutorial;
							}
							break;
						}
					}
				}
				finally
				{
					((System.IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
				}
			}
			return Entity.Null;
		}
	}

	public bool showListReminder => activeTutorialList == ((EntityQuery)(ref m_TutorialConfigurationQuery)).GetSingleton<TutorialsConfigurationData>().m_TutorialsIntroList;

	[Preserve]
	protected override void OnCreate()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		((ComponentSystemBase)this).Enabled = false;
		m_PrefabSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<PrefabSystem>();
		m_AudioManager = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<AudioManager>();
		m_CityConfigurationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_MapTilePurchaseSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
		m_GameScreenUISystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<GameScreenUISystem>();
		m_TutorialConfigurationQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<TutorialsConfigurationData>() });
		m_TutorialQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialData>(),
			ComponentType.Exclude<EditorTutorial>()
		});
		m_TutorialPhaseQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialPhaseData>(),
			ComponentType.Exclude<EditorTutorial>()
		});
		m_TutorialListQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<TutorialListData>() });
		m_ActiveTutorialListQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<TutorialListData>(),
			ComponentType.ReadOnly<TutorialRef>(),
			ComponentType.ReadOnly<TutorialActive>()
		});
		m_ActiveTutorialQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialData>(),
			ComponentType.ReadOnly<TutorialActive>()
		});
		m_ActiveTutorialPhaseQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialPhaseData>(),
			ComponentType.ReadOnly<TutorialPhaseActive>()
		});
		m_PendingTutorialListQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<TutorialListData>(),
			ComponentType.ReadOnly<TutorialRef>(),
			ComponentType.ReadOnly<TutorialActivated>(),
			ComponentType.Exclude<TutorialCompleted>()
		});
		m_PendingTutorialQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[6]
		{
			ComponentType.ReadOnly<TutorialData>(),
			ComponentType.ReadOnly<TutorialPhaseRef>(),
			ComponentType.ReadOnly<TutorialActivated>(),
			ComponentType.Exclude<TutorialActive>(),
			ComponentType.Exclude<TutorialCompleted>(),
			ComponentType.Exclude<EditorTutorial>()
		});
		m_PendingPriorityTutorialQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[7]
		{
			ComponentType.ReadOnly<TutorialData>(),
			ComponentType.ReadOnly<TutorialPhaseRef>(),
			ComponentType.ReadOnly<TutorialActivated>(),
			ComponentType.ReadOnly<ReplaceActiveData>(),
			ComponentType.Exclude<TutorialActive>(),
			ComponentType.Exclude<TutorialCompleted>(),
			ComponentType.Exclude<EditorTutorial>()
		});
		m_LockedTutorialQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<TutorialData>(),
			ComponentType.ReadOnly<Locked>(),
			ComponentType.Exclude<EditorTutorial>()
		});
		m_LockedTutorialPhaseQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialPhaseData>(),
			ComponentType.ReadOnly<Locked>()
		});
		m_LockedTutorialTriggerQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialTriggerData>(),
			ComponentType.ReadOnly<Locked>()
		});
		m_LockedTutorialListQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialListData>(),
			ComponentType.ReadOnly<Locked>()
		});
		m_SoundQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<ToolUXSoundSettingsData>() });
		m_ForceAdvisorQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<TutorialData>(),
			ComponentType.ReadOnly<AdvisorActivationData>()
		});
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		m_UnlockEventArchetype = ((EntityManager)(ref entityManager)).CreateArchetype((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadWrite<Event>(),
			ComponentType.ReadWrite<Unlock>()
		});
	}

	public virtual void OnResetTutorials()
	{
		if (GameManager.instance.gameMode.IsGameOrEditor())
		{
			ResetState();
			ClearComponents();
			m_Mode = TutorialMode.Intro;
		}
	}

	private bool IsCompleted(Entity tutorial, BufferLookup<TutorialAlternative> alternativeData, ComponentLookup<TutorialCompleted> completionData)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (completionData.HasComponent(tutorial))
		{
			return true;
		}
		DynamicBuffer<TutorialAlternative> val = default(DynamicBuffer<TutorialAlternative>);
		if (alternativeData.TryGetBuffer(tutorial, ref val))
		{
			for (int i = 0; i < val.Length; i++)
			{
				if (completionData.HasComponent(val[i].m_Alternative))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode gameMode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		base.OnGamePreload(purpose, gameMode);
		ResetState();
		((ComponentSystemBase)this).Enabled = gameMode.IsGame();
	}

	protected override void OnGameLoadingComplete(Purpose purpose, GameMode gameMode)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (gameMode == GameMode.Game && tutorialEnabled && !ShownTutorials.ContainsKey(kWelcomeIntroKey))
		{
			m_Mode = TutorialMode.Intro;
		}
		ReadSettings();
		bool flag = default(bool);
		if (m_CityConfigurationSystem.unlockMapTiles && (!tutorialEnabled || (ShownTutorials.TryGetValue(kListIntroKey, ref flag) && flag)))
		{
			TutorialsConfigurationData singleton = ((EntityQuery)(ref m_TutorialConfigurationQuery)).GetSingleton<TutorialsConfigurationData>();
			if (EntitiesExtensions.HasEnabledComponent<Locked>(((ComponentSystemBase)this).EntityManager, singleton.m_MapTilesFeature))
			{
				EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
				Entity val = ((EntityManager)(ref entityManager)).CreateEntity(m_UnlockEventArchetype);
				entityManager = ((ComponentSystemBase)this).EntityManager;
				((EntityManager)(ref entityManager)).SetComponentData<Unlock>(val, new Unlock(singleton.m_MapTilesFeature));
			}
			m_MapTilePurchaseSystem.UnlockMapTiles();
		}
		ForceAdvisorVisibility();
	}

	private void ResetState()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		m_Mode = TutorialMode.Default;
		m_AccumulatedDelay = 0f;
		SetTutorial(Entity.Null);
		SetTutorialList(Entity.Null);
	}

	private void ReadSettings()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		NativeArray<Entity> val = ((EntityQuery)(ref m_TutorialListQuery)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
		Enumerator<Entity> enumerator = val.GetEnumerator();
		try
		{
			bool flag = default(bool);
			while (enumerator.MoveNext())
			{
				Entity current = enumerator.Current;
				if (m_PrefabSystem.TryGetPrefab<PrefabBase>(current, out var prefab) && ShownTutorials.TryGetValue(((Object)prefab).name, ref flag))
				{
					if (flag)
					{
						CleanupTutorialList(current, passed: true, updateSettings: false);
					}
					else
					{
						SetTutorialShown(current, updateSettings: false);
					}
				}
			}
		}
		finally
		{
			((System.IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
		}
		val.Dispose();
		NativeArray<Entity> val2 = ((EntityQuery)(ref m_TutorialQuery)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
		bool flag2 = default(bool);
		for (int i = 0; i < val2.Length; i++)
		{
			Entity val3 = val2[i];
			if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(val3, out var prefab2) || !ShownTutorials.TryGetValue(((Object)prefab2).name, ref flag2))
			{
				continue;
			}
			if (flag2)
			{
				CleanupTutorial(val3, passed: true, updateSettings: false);
				continue;
			}
			SetTutorialShown(val3, updateSettings: false);
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			NativeArray<TutorialPhaseRef> val4 = ((EntityManager)(ref entityManager)).GetBuffer<TutorialPhaseRef>(val3, true).ToNativeArray(AllocatorHandle.op_Implicit((Allocator)3));
			for (int j = 0; j < val4.Length; j++)
			{
				Entity phase = val4[j].m_Phase;
				if (m_PrefabSystem.TryGetPrefab<PrefabBase>(phase, out var prefab3) && ShownTutorials.ContainsKey(((Object)prefab3).name))
				{
					SetTutorialShown(phase, updateSettings: false);
				}
			}
			val4.Dispose();
		}
		val2.Dispose();
	}

	private void SetTutorialShown(Entity entity, bool updateSettings = true)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		if (((EntityManager)(ref entityManager)).HasComponent<TutorialPhaseData>(entity))
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).AddComponent<TutorialPhaseShown>(entity);
			if (updateSettings)
			{
				UpdateSettings(entity);
			}
		}
		else
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).AddComponent<TutorialShown>(entity);
			if (updateSettings)
			{
				UpdateSettings(entity);
			}
		}
		UIObjectData uIObjectData = default(UIObjectData);
		if (EntitiesExtensions.TryGetComponent<UIObjectData>(((ComponentSystemBase)this).EntityManager, entity, ref uIObjectData) && uIObjectData.m_Group != Entity.Null)
		{
			SetTutorialShown(uIObjectData.m_Group, updateSettings: false);
		}
	}

	private void ForceAdvisorVisibility()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		NativeArray<Entity> val = ((EntityQuery)(ref m_ForceAdvisorQuery)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
		for (int i = 0; i < val.Length; i++)
		{
			ForceAdvisor(val[i]);
		}
		val.Dispose();
	}

	private void ForceAdvisor(Entity entity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		DynamicBuffer<TutorialPhaseRef> val = default(DynamicBuffer<TutorialPhaseRef>);
		if (EntitiesExtensions.TryGetBuffer<TutorialPhaseRef>(((ComponentSystemBase)this).EntityManager, entity, true, ref val))
		{
			NativeArray<TutorialPhaseRef> val2 = val.ToNativeArray(AllocatorHandle.op_Implicit((Allocator)3));
			for (int i = 0; i < val2.Length; i++)
			{
				Entity phase = val2[i].m_Phase;
				ForceAdvisor(phase);
			}
			val2.Dispose();
		}
		UIObjectData uIObjectData = default(UIObjectData);
		if (EntitiesExtensions.TryGetComponent<UIObjectData>(((ComponentSystemBase)this).EntityManager, entity, ref uIObjectData) && uIObjectData.m_Group != Entity.Null)
		{
			ForceAdvisor(uIObjectData.m_Group);
		}
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		if (!((EntityManager)(ref entityManager)).HasComponent<ForceAdvisor>(entity))
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).AddComponent<ForceAdvisor>(entity);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (mode != TutorialMode.Default)
		{
			return;
		}
		if (tutorialEnabled)
		{
			UpdateActiveTutorialList();
		}
		if (!tutorialEnabled)
		{
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			if (!((EntityManager)(ref entityManager)).HasComponent<AdvisorActivation>(activeTutorial))
			{
				ClearTutorialLocks();
				SetTutorial(Entity.Null);
				SetTutorialList(Entity.Null);
				return;
			}
		}
		UpdateActiveTutorial();
	}

	public void ForceTutorial(Entity tutorial, Entity phase, bool advisorActivation)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (tutorial != Entity.Null)
		{
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).AddComponent<ForceActivation>(tutorial);
			if (advisorActivation)
			{
				entityManager = ((ComponentSystemBase)this).EntityManager;
				((EntityManager)(ref entityManager)).AddComponent<AdvisorActivation>(tutorial);
			}
		}
		SetTutorial(tutorial, phase, passed: false);
	}

	private void SetTutorial(Entity tutorial, bool passed = false)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		SetTutorial(tutorial, Entity.Null, passed);
	}

	public void SetTutorial(Entity tutorial, Entity phase, bool passed)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorial;
		Entity val2 = activeTutorialPhase;
		if (tutorial != val)
		{
			if (val != Entity.Null)
			{
				CleanupTutorial(val, passed);
			}
			if (tutorial != Entity.Null)
			{
				SetTutorialShown(tutorial);
				EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
				((EntityManager)(ref entityManager)).AddComponent<TutorialActive>(tutorial);
				Entity firstTutorialPhase = GetFirstTutorialPhase(tutorial);
				SetTutorialPhase((phase == Entity.Null) ? firstTutorialPhase : phase, passed);
			}
			else
			{
				SetTutorialPhase(Entity.Null, passed);
			}
			m_AccumulatedDelay = 0f;
		}
		else if (phase != val2)
		{
			SetTutorialPhase(phase, passed);
			m_AccumulatedDelay = 0f;
		}
	}

	public void SetTutorial(Entity tutorial, Entity phase)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		SetTutorial(tutorial, phase, passed: false);
	}

	private void SetTutorialPhase(Entity tutorialPhase, bool passed)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorialPhase;
		if (!(tutorialPhase != val))
		{
			return;
		}
		if (val != Entity.Null)
		{
			CleanupTutorialPhase(val, passed);
		}
		if (tutorialPhase != Entity.Null)
		{
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).AddComponent<TutorialPhaseActive>(tutorialPhase);
			ManualUnlock(tutorialPhase, m_UnlockEventArchetype, ((ComponentSystemBase)this).EntityManager);
			SetTutorialShown(tutorialPhase);
			if (!((EntityQuery)(ref m_SoundQuery)).IsEmptyIgnoreFilter && !m_GameScreenUISystem.isMenuActive && !GameManager.instance.isGameLoading)
			{
				m_AudioManager.PlayUISound(((EntityQuery)(ref m_SoundQuery)).GetSingleton<ToolUXSoundSettingsData>().m_TutorialStartedSound);
			}
			TutorialTrigger tutorialTrigger = default(TutorialTrigger);
			if (EntitiesExtensions.TryGetComponent<TutorialTrigger>(((ComponentSystemBase)this).EntityManager, tutorialPhase, ref tutorialTrigger))
			{
				entityManager = ((ComponentSystemBase)this).EntityManager;
				((EntityManager)(ref entityManager)).AddComponent<TriggerActive>(tutorialTrigger.m_Trigger);
			}
		}
	}

	public void CompleteCurrentTutorialPhase()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorialPhase;
		TutorialTrigger tutorialTrigger = default(TutorialTrigger);
		TutorialNextPhase tutorialNextPhase = default(TutorialNextPhase);
		if (val != Entity.Null && EntitiesExtensions.TryGetComponent<TutorialTrigger>(((ComponentSystemBase)this).EntityManager, val, ref tutorialTrigger) && EntitiesExtensions.TryGetComponent<TutorialNextPhase>(((ComponentSystemBase)this).EntityManager, tutorialTrigger.m_Trigger, ref tutorialNextPhase))
		{
			CompleteCurrentTutorialPhase(tutorialNextPhase.m_NextPhase);
		}
		else
		{
			CompleteCurrentTutorialPhase(Entity.Null);
		}
	}

	public void CompleteCurrentTutorialPhase(Entity nextPhase)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorial;
		if (val != Entity.Null)
		{
			Entity val2 = GetNextPhase(val, activeTutorialPhase, nextPhase);
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			if (((EntityManager)(ref entityManager)).HasComponent<ForceTutorialCompletion>(activeTutorialPhase))
			{
				val2 = Entity.Null;
			}
			if (val2 != Entity.Null)
			{
				SetTutorialPhase(val2, passed: true);
			}
			else
			{
				CompleteTutorial(val);
			}
		}
	}

	private void UpdateActiveTutorial()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (activeTutorial != Entity.Null)
		{
			if (CheckCurrentPhaseCompleted(out var nextPhase))
			{
				CompleteCurrentTutorialPhase(nextPhase);
			}
			if (ShouldReplaceActiveTutorial())
			{
				ActivateNextTutorial();
			}
		}
		if (activeTutorial == Entity.Null)
		{
			ActivateNextTutorial(delay: true);
		}
	}

	private bool ShouldReplaceActiveTutorial()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorial;
		if (val != Entity.Null)
		{
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			if (((EntityManager)(ref entityManager)).HasComponent<ForceActivation>(val))
			{
				return false;
			}
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (!((EntityManager)(ref entityManager)).HasComponent<TutorialActivated>(val))
			{
				return true;
			}
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (!((EntityManager)(ref entityManager)).HasComponent<ReplaceActiveData>(val))
			{
				return !((EntityQuery)(ref m_PendingPriorityTutorialQuery)).IsEmptyIgnoreFilter;
			}
			return false;
		}
		return false;
	}

	private void CleanupTutorial(Entity tutorial, bool passed = false, bool updateSettings = true)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		if (!(tutorial != Entity.Null))
		{
			return;
		}
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<AdvisorActivation>(tutorial);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialActive>(tutorial);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<ForceActivation>(tutorial);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		NativeArray<TutorialPhaseRef> val = ((EntityManager)(ref entityManager)).GetBuffer<TutorialPhaseRef>(tutorial, true).ToNativeArray(AllocatorHandle.op_Implicit((Allocator)3));
		for (int i = 0; i < val.Length; i++)
		{
			Entity phase = val[i].m_Phase;
			CleanupTutorialPhase(phase, passed, updateSettings);
		}
		val.Dispose();
		if (!passed)
		{
			return;
		}
		SetTutorialShown(tutorial);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).AddComponent<TutorialCompleted>(tutorial);
		if (updateSettings)
		{
			UpdateSettings(tutorial, passed: true);
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (((EntityManager)(ref entityManager)).HasComponent<TutorialFireTelemetry>(tutorial))
			{
				Telemetry.TutorialEvent(tutorial);
			}
		}
		ManualUnlock(tutorial, m_UnlockEventArchetype, ((ComponentSystemBase)this).EntityManager);
	}

	private void UpdateSettings(Entity tutorial, bool passed = false)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		if (m_PrefabSystem.TryGetPrefab<PrefabBase>(tutorial, out var prefab))
		{
			UpdateSettings(((Object)prefab).name, passed);
		}
	}

	private void UpdateSettings(string name, bool passed)
	{
		Setting setting = m_Setting;
		if (ShownTutorials.TryAdd(name, passed))
		{
			setting.ApplyAndSave();
		}
		else if (passed)
		{
			ShownTutorials[name] = true;
			setting.ApplyAndSave();
		}
	}

	private void CleanupTutorialPhase(Entity tutorialPhase, bool passed = false, bool updateSettings = true)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		if (!(tutorialPhase != Entity.Null))
		{
			return;
		}
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialPhaseActive>(tutorialPhase);
		if (passed)
		{
			SetTutorialShown(tutorialPhase);
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).AddComponent<TutorialPhaseCompleted>(tutorialPhase);
			ManualUnlock(tutorialPhase, m_UnlockEventArchetype, ((ComponentSystemBase)this).EntityManager);
			if (updateSettings)
			{
				UpdateSettings(tutorialPhase, passed: true);
			}
		}
		TutorialTrigger tutorialTrigger = default(TutorialTrigger);
		if (EntitiesExtensions.TryGetComponent<TutorialTrigger>(((ComponentSystemBase)this).EntityManager, tutorialPhase, ref tutorialTrigger))
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).RemoveComponent<TriggerActive>(tutorialTrigger.m_Trigger);
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).RemoveComponent<TriggerCompleted>(tutorialTrigger.m_Trigger);
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).RemoveComponent<TriggerPreCompleted>(tutorialTrigger.m_Trigger);
			entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).RemoveComponent<TutorialNextPhase>(tutorialTrigger.m_Trigger);
			if (passed)
			{
				ManualUnlock(tutorialTrigger.m_Trigger, m_UnlockEventArchetype, ((ComponentSystemBase)this).EntityManager);
			}
		}
	}

	private void ActivateNextTutorial(bool delay = false)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (delay)
		{
			m_AccumulatedDelay += Time.deltaTime;
			if (m_AccumulatedDelay < kActivationDelay)
			{
				return;
			}
			m_AccumulatedDelay = 0f;
		}
		Entity tutorial = FindNextTutorial();
		SetTutorial(tutorial);
	}

	private Entity FindNextTutorial()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		int num = -1;
		int num2 = -1;
		Entity val = FindNextTutorial(m_PendingPriorityTutorialQuery);
		if (val == Entity.Null)
		{
			val = FindNextTutorial(m_PendingTutorialQuery);
		}
		EntityManager entityManager;
		if (val != Entity.Null)
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			num = ((EntityManager)(ref entityManager)).GetComponentData<TutorialData>(val).m_Priority;
		}
		Entity val2 = activeTutorialList;
		if (val2 != Entity.Null)
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			num2 = ((EntityManager)(ref entityManager)).GetComponentData<TutorialListData>(val2).m_Priority;
		}
		if (val2 != Entity.Null && (num2 < num || val == Entity.Null))
		{
			Entity val3 = nextListTutorial;
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (((EntityManager)(ref entityManager)).HasComponent<TutorialActivated>(val3))
			{
				return val3;
			}
		}
		else if (val != Entity.Null && (num <= num2 || val2 == Entity.Null))
		{
			return val;
		}
		return Entity.Null;
	}

	private Entity FindNextTutorial(EntityQuery query)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (!((EntityQuery)(ref query)).IsEmptyIgnoreFilter)
		{
			NativeArray<TutorialData> val = ((EntityQuery)(ref query)).ToComponentDataArray<TutorialData>(AllocatorHandle.op_Implicit((Allocator)3));
			NativeArray<Entity> val2 = ((EntityQuery)(ref query)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
			int num = 0;
			for (int i = 0; i < val.Length; i++)
			{
				if (val[i].m_Priority < val[num].m_Priority)
				{
					num = i;
				}
			}
			Entity result = val2[num];
			val.Dispose();
			val2.Dispose();
			return result;
		}
		return Entity.Null;
	}

	private bool CheckCurrentPhaseCompleted(out Entity nextPhase)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		nextPhase = Entity.Null;
		TutorialPhaseData phase = default(TutorialPhaseData);
		TutorialTrigger tutorialTrigger = default(TutorialTrigger);
		if (EntitiesExtensions.TryGetComponent<TutorialPhaseData>(((ComponentSystemBase)this).EntityManager, activeTutorialPhase, ref phase) && EntitiesExtensions.TryGetComponent<TutorialTrigger>(((ComponentSystemBase)this).EntityManager, activeTutorialPhase, ref tutorialTrigger))
		{
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			if (!((EntityManager)(ref entityManager)).HasComponent<TriggerCompleted>(tutorialTrigger.m_Trigger))
			{
				return false;
			}
			if (m_AccumulatedDelay < GetCompletionDelay(phase))
			{
				m_AccumulatedDelay += Time.deltaTime;
				return false;
			}
			m_AccumulatedDelay = 0f;
			TutorialNextPhase tutorialNextPhase = default(TutorialNextPhase);
			if (EntitiesExtensions.TryGetComponent<TutorialNextPhase>(((ComponentSystemBase)this).EntityManager, activeTutorialPhase, ref tutorialNextPhase))
			{
				nextPhase = tutorialNextPhase.m_NextPhase;
			}
			if (EntitiesExtensions.TryGetComponent<TutorialNextPhase>(((ComponentSystemBase)this).EntityManager, tutorialTrigger.m_Trigger, ref tutorialNextPhase))
			{
				nextPhase = tutorialNextPhase.m_NextPhase;
			}
			return true;
		}
		return false;
	}

	private static float GetCompletionDelay(TutorialPhaseData phase)
	{
		if (phase.m_OverrideCompletionDelay >= 0f)
		{
			return phase.m_OverrideCompletionDelay;
		}
		if (phase.m_Type == TutorialPhaseType.Balloon)
		{
			return kBalloonCompletionDelay;
		}
		return kCompletionDelay;
	}

	private Entity GetNextPhase(Entity tutorial, Entity currentPhase, Entity nextPhase)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		NativeArray<TutorialPhaseRef> val = ((EntityManager)(ref entityManager)).GetBuffer<TutorialPhaseRef>(tutorial, true).ToNativeArray(AllocatorHandle.op_Implicit((Allocator)3));
		for (int i = 0; i < val.Length; i++)
		{
			Entity phase = val[i].m_Phase;
			if (nextPhase == Entity.Null)
			{
				if (!(phase == currentPhase))
				{
					continue;
				}
				for (int j = i; j < val.Length - 1; j++)
				{
					nextPhase = val[j + 1].m_Phase;
					if (IsValidControlScheme(nextPhase, m_PrefabSystem))
					{
						val.Dispose();
						return nextPhase;
					}
				}
			}
			else if (phase == nextPhase)
			{
				val.Dispose();
				if (!IsValidControlScheme(nextPhase, m_PrefabSystem))
				{
					return Entity.Null;
				}
				return nextPhase;
			}
		}
		val.Dispose();
		return Entity.Null;
	}

	private Entity GetFirstTutorialPhase(Entity tutorial)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		DynamicBuffer<TutorialPhaseRef> buffer = ((EntityManager)(ref entityManager)).GetBuffer<TutorialPhaseRef>(tutorial, true);
		for (int i = 0; i < buffer.Length; i++)
		{
			Entity phase = buffer[i].m_Phase;
			if (IsValidControlScheme(phase, m_PrefabSystem))
			{
				return phase;
			}
		}
		return Entity.Null;
	}

	public static bool IsValidControlScheme(Entity phase, PrefabSystem prefabSystem)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		TutorialPhasePrefab prefab = prefabSystem.GetPrefab<TutorialPhasePrefab>(phase);
		if ((prefab == null || (prefab.m_ControlScheme & TutorialPhasePrefab.ControlScheme.All) != TutorialPhasePrefab.ControlScheme.All) && (InputManager.instance.activeControlScheme != InputManager.ControlScheme.KeyboardAndMouse || prefab == null || (prefab.m_ControlScheme & TutorialPhasePrefab.ControlScheme.KeyboardAndMouse) != TutorialPhasePrefab.ControlScheme.KeyboardAndMouse))
		{
			if (InputManager.instance.activeControlScheme == InputManager.ControlScheme.Gamepad)
			{
				if (prefab == null)
				{
					return false;
				}
				return (prefab.m_ControlScheme & TutorialPhasePrefab.ControlScheme.Gamepad) == TutorialPhasePrefab.ControlScheme.Gamepad;
			}
			return false;
		}
		return true;
	}

	private void ClearTutorialLocks()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if (GameManager.instance.gameMode != GameMode.Game)
		{
			return;
		}
		ClearLocks(m_LockedTutorialQuery);
		ClearLocks(m_LockedTutorialPhaseQuery);
		ClearLocks(m_LockedTutorialTriggerQuery);
		ClearLocks(m_LockedTutorialListQuery);
		if (m_CityConfigurationSystem.unlockMapTiles)
		{
			TutorialsConfigurationData singleton = ((EntityQuery)(ref m_TutorialConfigurationQuery)).GetSingleton<TutorialsConfigurationData>();
			if (EntitiesExtensions.HasEnabledComponent<Locked>(((ComponentSystemBase)this).EntityManager, singleton.m_MapTilesFeature))
			{
				EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
				Entity val = ((EntityManager)(ref entityManager)).CreateEntity(m_UnlockEventArchetype);
				entityManager = ((ComponentSystemBase)this).EntityManager;
				((EntityManager)(ref entityManager)).SetComponentData<Unlock>(val, new Unlock(singleton.m_MapTilesFeature));
			}
			m_MapTilePurchaseSystem.UnlockMapTiles();
		}
	}

	private void ClearLocks(EntityQuery query)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (GameManager.instance.gameMode == GameMode.Game && !((EntityQuery)(ref query)).IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> val = ((EntityQuery)(ref query)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
			for (int i = 0; i < val.Length; i++)
			{
				ClearLock(val[i]);
			}
			val.Dispose();
		}
	}

	private void ClearLock(Entity entity)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (GameManager.instance.gameMode != GameMode.Game)
		{
			return;
		}
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		NativeArray<UnlockRequirement> val = ((EntityManager)(ref entityManager)).GetBuffer<UnlockRequirement>(entity, true).ToNativeArray(AllocatorHandle.op_Implicit((Allocator)3));
		for (int i = 0; i < val.Length; i++)
		{
			UnlockRequirement unlockRequirement = val[i];
			if (unlockRequirement.m_Prefab == entity && (unlockRequirement.m_Flags & UnlockFlags.RequireAll) != 0)
			{
				ManualUnlock(entity, m_UnlockEventArchetype, ((ComponentSystemBase)this).EntityManager);
				val.Dispose();
				return;
			}
		}
		val.Dispose();
	}

	public void SetAllTutorialsShown()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if (!((EntityQuery)(ref m_TutorialQuery)).IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> val = ((EntityQuery)(ref m_TutorialQuery)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
			for (int i = 0; i < val.Length; i++)
			{
				SetTutorialShown(val[i]);
			}
			val.Dispose();
		}
		if (!((EntityQuery)(ref m_TutorialPhaseQuery)).IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> val2 = ((EntityQuery)(ref m_TutorialPhaseQuery)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
			for (int j = 0; j < val2.Length; j++)
			{
				SetTutorialShown(val2[j]);
			}
			val2.Dispose();
		}
	}

	public void CompleteTutorial(Entity tutorial)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (!((EntityQuery)(ref m_SoundQuery)).IsEmptyIgnoreFilter)
		{
			m_AudioManager.PlayUISound(((EntityQuery)(ref m_SoundQuery)).GetSingleton<ToolUXSoundSettingsData>().m_TutorialCompletedSound);
		}
		CleanupTutorial(tutorial, passed: true);
	}

	public static void ManualUnlock(Entity entity, EntityArchetype unlockEventArchetype, EntityManager entityManager)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		DynamicBuffer<UnlockRequirement> val = default(DynamicBuffer<UnlockRequirement>);
		if (!EntitiesExtensions.TryGetBuffer<UnlockRequirement>(entityManager, entity, true, ref val) || val.Length <= 0 || !(val[0].m_Prefab == entity) || (val[0].m_Flags & UnlockFlags.RequireAll) == 0)
		{
			return;
		}
		Entity val2 = ((EntityManager)(ref entityManager)).CreateEntity(unlockEventArchetype);
		((EntityManager)(ref entityManager)).SetComponentData<Unlock>(val2, new Unlock(entity));
		DynamicBuffer<ForceUIGroupUnlockData> val3 = default(DynamicBuffer<ForceUIGroupUnlockData>);
		if (EntitiesExtensions.TryGetBuffer<ForceUIGroupUnlockData>(entityManager, entity, true, ref val3))
		{
			NativeArray<ForceUIGroupUnlockData> val4 = val3.ToNativeArray(AllocatorHandle.op_Implicit((Allocator)3));
			for (int i = 0; i < val4.Length; i++)
			{
				Entity val5 = ((EntityManager)(ref entityManager)).CreateEntity(unlockEventArchetype);
				((EntityManager)(ref entityManager)).SetComponentData<Unlock>(val5, new Unlock(val4[i].m_Entity));
			}
			val4.Dispose();
		}
	}

	public static void ManualUnlock(Entity entity, EntityArchetype unlockEventArchetype, EntityManager entityManager, EntityCommandBuffer commandBuffer)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		DynamicBuffer<UnlockRequirement> val = default(DynamicBuffer<UnlockRequirement>);
		if (!EntitiesExtensions.TryGetBuffer<UnlockRequirement>(entityManager, entity, true, ref val) || val.Length <= 0 || !(val[0].m_Prefab == entity) || (val[0].m_Flags & UnlockFlags.RequireAll) == 0)
		{
			return;
		}
		Entity val2 = ((EntityCommandBuffer)(ref commandBuffer)).CreateEntity(unlockEventArchetype);
		((EntityCommandBuffer)(ref commandBuffer)).SetComponent<Unlock>(val2, new Unlock(entity));
		DynamicBuffer<ForceUIGroupUnlockData> val3 = default(DynamicBuffer<ForceUIGroupUnlockData>);
		if (EntitiesExtensions.TryGetBuffer<ForceUIGroupUnlockData>(entityManager, entity, true, ref val3))
		{
			for (int i = 0; i < val3.Length; i++)
			{
				Entity val4 = ((EntityCommandBuffer)(ref commandBuffer)).CreateEntity(unlockEventArchetype);
				((EntityCommandBuffer)(ref commandBuffer)).SetComponent<Unlock>(val4, new Unlock(val3[i].m_Entity));
			}
		}
	}

	public static void ManualUnlock(Entity entity, EntityArchetype unlockEventArchetype, ref BufferLookup<ForceUIGroupUnlockData> forcedUnlocksFromEntity, ref BufferLookup<UnlockRequirement> unlockRequirementsFromEntity, ParallelWriter commandBuffer, int sortKey)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		DynamicBuffer<UnlockRequirement> val = default(DynamicBuffer<UnlockRequirement>);
		if (!unlockRequirementsFromEntity.TryGetBuffer(entity, ref val) || val.Length <= 0 || !(val[0].m_Prefab == entity) || (val[0].m_Flags & UnlockFlags.RequireAll) == 0)
		{
			return;
		}
		Entity val2 = ((ParallelWriter)(ref commandBuffer)).CreateEntity(sortKey, unlockEventArchetype);
		((ParallelWriter)(ref commandBuffer)).SetComponent<Unlock>(sortKey, val2, new Unlock(entity));
		DynamicBuffer<ForceUIGroupUnlockData> val3 = default(DynamicBuffer<ForceUIGroupUnlockData>);
		if (forcedUnlocksFromEntity.TryGetBuffer(entity, ref val3))
		{
			for (int i = 0; i < val3.Length; i++)
			{
				Entity val4 = ((ParallelWriter)(ref commandBuffer)).CreateEntity(sortKey, unlockEventArchetype);
				((ParallelWriter)(ref commandBuffer)).SetComponent<Unlock>(sortKey, val4, new Unlock(val3[i].m_Entity));
			}
		}
	}

	private void UpdateActiveTutorialList()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		if (activeTutorialList != Entity.Null)
		{
			CheckListIntro();
			if (CheckCurrentTutorialListCompleted())
			{
				CompleteCurrentTutorialList();
			}
			if (ShouldReplaceActiveTutorialList())
			{
				ActivateNextTutorialList();
			}
		}
		else
		{
			ActivateNextTutorialList();
		}
	}

	private void CompleteCurrentTutorialList()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorialList;
		if (m_CityConfigurationSystem.unlockMapTiles)
		{
			TutorialsConfigurationData singleton = ((EntityQuery)(ref m_TutorialConfigurationQuery)).GetSingleton<TutorialsConfigurationData>();
			if (activeTutorialList == singleton.m_TutorialsIntroList)
			{
				if (EntitiesExtensions.HasEnabledComponent<Locked>(((ComponentSystemBase)this).EntityManager, singleton.m_MapTilesFeature))
				{
					EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
					Entity val2 = ((EntityManager)(ref entityManager)).CreateEntity(m_UnlockEventArchetype);
					entityManager = ((ComponentSystemBase)this).EntityManager;
					((EntityManager)(ref entityManager)).SetComponentData<Unlock>(val2, new Unlock(singleton.m_MapTilesFeature));
				}
				m_MapTilePurchaseSystem.UnlockMapTiles();
			}
		}
		if (val != Entity.Null)
		{
			SetTutorialList(Entity.Null, passed: true);
		}
	}

	private void ActivateNextTutorialList()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		Entity tutorialList = FindNextTutorialList(m_PendingTutorialListQuery);
		SetTutorialList(tutorialList);
	}

	private void CheckListIntro()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		TutorialsConfigurationData singleton = ((EntityQuery)(ref m_TutorialConfigurationQuery)).GetSingleton<TutorialsConfigurationData>();
		if (activeTutorialList == singleton.m_TutorialsIntroList && !ShownTutorials.ContainsKey(kListIntroKey) && activeTutorial == Entity.Null && !NonListTutorialPending())
		{
			mode = TutorialMode.ListIntro;
			UpdateSettings(kListIntroKey, passed: true);
		}
	}

	private bool NonListTutorialPending()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		Entity val = FindNextTutorial();
		if (val != Entity.Null)
		{
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			DynamicBuffer<TutorialRef> buffer = ((EntityManager)(ref entityManager)).GetBuffer<TutorialRef>(activeTutorialList, true);
			for (int i = 0; i < buffer.Length; i++)
			{
				if (buffer[i].m_Tutorial == val)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	private bool CheckCurrentTutorialListCompleted()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		if (((EntityManager)(ref entityManager)).HasComponent<TutorialRef>(activeTutorialList))
		{
			ComponentLookup<TutorialCompleted> componentLookup = InternalCompilerInterface.GetComponentLookup<TutorialCompleted>(ref __TypeHandle.__Game_Tutorials_TutorialCompleted_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef);
			BufferLookup<TutorialAlternative> bufferLookup = InternalCompilerInterface.GetBufferLookup<TutorialAlternative>(ref __TypeHandle.__Game_Tutorials_TutorialAlternative_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef);
			entityManager = ((ComponentSystemBase)this).EntityManager;
			DynamicBuffer<TutorialRef> buffer = ((EntityManager)(ref entityManager)).GetBuffer<TutorialRef>(activeTutorialList, true);
			for (int i = 0; i < buffer.Length; i++)
			{
				if (!IsCompleted(buffer[i].m_Tutorial, bufferLookup, componentLookup))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool ShouldReplaceActiveTutorialList()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorialList;
		if (val != Entity.Null)
		{
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			return !((EntityManager)(ref entityManager)).HasComponent<TutorialActivated>(val);
		}
		return false;
	}

	private Entity FindNextTutorialList(EntityQuery query)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (!((EntityQuery)(ref query)).IsEmptyIgnoreFilter)
		{
			NativeArray<TutorialListData> val = ((EntityQuery)(ref query)).ToComponentDataArray<TutorialListData>(AllocatorHandle.op_Implicit((Allocator)3));
			NativeArray<Entity> val2 = ((EntityQuery)(ref query)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)3));
			int num = 0;
			for (int i = 0; i < val.Length; i++)
			{
				if (val[i].m_Priority < val[num].m_Priority)
				{
					num = i;
				}
			}
			Entity result = val2[num];
			val.Dispose();
			val2.Dispose();
			return result;
		}
		return Entity.Null;
	}

	private void SetTutorialList(Entity tutorialList, bool passed = false, bool updateSettings = true)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		Entity val = activeTutorialList;
		if (!(tutorialList != val))
		{
			return;
		}
		if (val != Entity.Null)
		{
			CleanupTutorialList(val, passed, updateSettings);
			if (passed)
			{
				TutorialsConfigurationData singleton = ((EntityQuery)(ref m_TutorialConfigurationQuery)).GetSingleton<TutorialsConfigurationData>();
				if (updateSettings && val == singleton.m_TutorialsIntroList && !ShownTutorials.ContainsKey(kListOutroKey))
				{
					mode = TutorialMode.ListOutro;
					UpdateSettings(kListOutroKey, passed: true);
				}
			}
		}
		if (tutorialList != Entity.Null)
		{
			SetTutorialShown(tutorialList, updateSettings);
			EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
			((EntityManager)(ref entityManager)).AddComponent<TutorialActive>(tutorialList);
		}
	}

	private void CleanupTutorialList(Entity tutorialList, bool passed = false, bool updateSettings = true)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if (!(tutorialList != Entity.Null))
		{
			return;
		}
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialActivated>(tutorialList);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialActive>(tutorialList);
		if (!passed)
		{
			return;
		}
		SetTutorialShown(tutorialList);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).AddComponent<TutorialCompleted>(tutorialList);
		ManualUnlock(tutorialList, m_UnlockEventArchetype, ((ComponentSystemBase)this).EntityManager);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		if (((EntityManager)(ref entityManager)).HasComponent<TutorialRef>(tutorialList))
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			NativeArray<TutorialRef> val = ((EntityManager)(ref entityManager)).GetBuffer<TutorialRef>(tutorialList, true).ToNativeArray(AllocatorHandle.op_Implicit((Allocator)3));
			for (int i = 0; i < val.Length; i++)
			{
				CleanupTutorial(val[i].m_Tutorial, passed, updateSettings);
			}
			val.Dispose();
		}
		if (updateSettings)
		{
			UpdateSettings(tutorialList, passed: true);
		}
	}

	public void SkipActiveList()
	{
		CompleteCurrentTutorialList();
	}

	public void PreDeserialize(Context context)
	{
		ClearComponents();
	}

	private void ClearComponents()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialActive>(m_TutorialListQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialCompleted>(m_TutorialListQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialShown>(m_TutorialListQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<AdvisorActivation>(m_TutorialQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialActive>(m_TutorialQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialCompleted>(m_TutorialQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialShown>(m_TutorialQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<ForceActivation>(m_TutorialQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialActivated>(m_TutorialQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialPhaseActive>(m_TutorialPhaseQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialPhaseCompleted>(m_TutorialPhaseQuery);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		((EntityManager)(ref entityManager)).RemoveComponent<TutorialPhaseShown>(m_TutorialPhaseQuery);
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
		((ComponentSystemBase)this).OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public TutorialSystem()
	{
	}
}
