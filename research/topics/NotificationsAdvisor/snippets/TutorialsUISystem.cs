using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TutorialsUISystem : UISystemBase
{
	internal static class BindingNames
	{
		internal const string kEditorTutorialsUnavailable = "editorTutorialsUnavailable";

		internal const string kTutorialsEnabled = "tutorialsEnabled";

		internal const string kEditorTutorialsEnabled = "editorTutorialsEnabled";

		internal const string kIntroActive = "introActive";

		internal const string kNext = "next";

		internal const string kActiveTutorial = "activeTutorial";

		internal const string kActiveTutorialPhase = "activeTutorialPhase";

		internal const string kCategories = "categories";

		internal const string kTutorials = "tutorials";

		internal const string kPending = "pending";

		internal const string kActiveList = "activeList";

		internal const string kActivateTutorial = "activateTutorial";

		internal const string kActivateTutorialPhase = "activateTutorialPhase";

		internal const string kForceTutorial = "forceTutorial";

		internal const string kActivateTutorialTrigger = "activateTutorialTrigger";

		internal const string kDisactivateTutorialTrigger = "disactivateTutorialTrigger";

		internal const string kSetTutorialTagActive = "setTutorialTagActive";

		internal const string kCompleteActiveTutorialPhase = "completeActiveTutorialPhase";

		internal const string kCompleteActiveTutorial = "completeActiveTutorial";

		internal const string kCompleteIntro = "completeIntro";

		internal const string kCompleteListIntro = "completeListIntro";

		internal const string kCompleteListOutro = "completeListOutro";

		internal const string kListIntroActive = "listIntroActive";

		internal const string kListOutroActive = "listOutroActive";

		internal const string kControlScheme = "controlScheme";

		internal const string kAdvisorPanelVisible = "advisorPanelVisible";

		internal const string kToggleTutorials = "toggleTutorials";
	}

	private enum AdvisorItemType
	{
		Tutorial,
		Group
	}

	private struct TypeHandle
	{
		public ComponentLookup<TutorialActivationData> __Game_Tutorials_TutorialActivationData_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tutorials_TutorialActivationData_RW_ComponentLookup = state.GetComponentLookup<TutorialActivationData>();
		}
	}

	protected const string kGroup = "tutorials";

	protected PrefabSystem m_PrefabSystem;

	protected ITutorialSystem m_TutorialSystem;

	private ITutorialSystem m_EditorTutorialSystem;

	protected ITutorialUIActivationSystem m_ActivationSystem;

	protected ITutorialUIDeactivationSystem m_DeactivationSystem;

	protected ITutorialUITriggerSystem m_TriggerSystem;

	private EntityQuery m_TutorialConfigurationQuery;

	protected EntityQuery m_TutorialCategoryQuery;

	protected EntityQuery m_UnlockQuery;

	protected RawValueBinding m_ActiveTutorialListBinding;

	protected RawValueBinding m_TutorialCategoriesBinding;

	protected RawMapBinding<Entity> m_TutorialsBinding;

	protected RawValueBinding m_ActiveTutorialBinding;

	protected RawValueBinding m_ActiveTutorialPhaseBinding;

	protected GetterValueBinding<Entity> m_TutorialPendingBinding;

	private int m_TutorialActiveVersion;

	private int m_PhaseActiveVersion;

	private int m_TriggerActiveVersion;

	private int m_TriggerCompletedVersion;

	private int m_PhaseCompletedVersion;

	protected bool m_WasEnabled;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		base.Enabled = false;
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ActivationSystem = base.World.GetOrCreateSystemManaged<TutorialUIActivationSystem>();
		m_DeactivationSystem = base.World.GetOrCreateSystemManaged<TutorialUIDeactivationSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TutorialUITriggerSystem>();
		m_UnlockQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		AddBinding(m_ActiveTutorialListBinding = new RawValueBinding("tutorials", "activeList", BindActiveTutorialList));
		AddBinding(new TriggerBinding<Entity>("tutorials", "activateTutorial", ActivateTutorial));
		AddBinding(new TriggerBinding<Entity, Entity>("tutorials", "activateTutorialPhase", ActivateTutorialPhase));
		AddBinding(new TriggerBinding<Entity, Entity, bool>("tutorials", "forceTutorial", ForceTutorial));
		AddBinding(new TriggerBinding("tutorials", "completeActiveTutorialPhase", CompleteActiveTutorialPhase));
		AddBinding(new TriggerBinding("tutorials", "completeActiveTutorial", CompleteActiveTutorial));
		AddBinding(new TriggerBinding<string, bool>("tutorials", "setTutorialTagActive", OnSetTutorialTagActive));
		AddBinding(new TriggerBinding<string>("tutorials", "activateTutorialTrigger", ActivateTutorialTrigger));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		int componentOrderVersion = base.EntityManager.GetComponentOrderVersion<TutorialActive>();
		int componentOrderVersion2 = base.EntityManager.GetComponentOrderVersion<TutorialPhaseActive>();
		int componentOrderVersion3 = base.EntityManager.GetComponentOrderVersion<TutorialPhaseCompleted>();
		base.EntityManager.GetComponentOrderVersion<TutorialPhaseShown>();
		int componentOrderVersion4 = base.EntityManager.GetComponentOrderVersion<TriggerActive>();
		int componentOrderVersion5 = base.EntityManager.GetComponentOrderVersion<TriggerCompleted>();
		base.EntityManager.GetComponentOrderVersion<TutorialShown>();
		bool flag = componentOrderVersion != m_TutorialActiveVersion;
		bool flag2 = componentOrderVersion2 != m_PhaseActiveVersion;
		bool flag3 = componentOrderVersion4 != m_TriggerActiveVersion;
		bool flag4 = componentOrderVersion5 != m_TriggerCompletedVersion;
		bool flag5 = componentOrderVersion3 != m_PhaseCompletedVersion;
		if (flag)
		{
			m_ActiveTutorialListBinding.Update();
		}
		if (m_TutorialsBinding != null && (flag || flag2 || flag3 || flag4 || flag5))
		{
			m_ActiveTutorialBinding.Update();
			m_ActiveTutorialPhaseBinding.Update();
			m_TutorialsBinding.Update(m_TutorialSystem.activeTutorial);
		}
		if (m_TutorialPendingBinding != null)
		{
			m_TutorialPendingBinding.Update();
		}
		m_TutorialActiveVersion = componentOrderVersion;
		m_PhaseActiveVersion = componentOrderVersion2;
		m_TriggerActiveVersion = componentOrderVersion4;
		m_TriggerCompletedVersion = componentOrderVersion5;
		m_PhaseCompletedVersion = componentOrderVersion3;
	}

	protected void BindTutorialGroup(IJsonWriter writer, Entity entity)
	{
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, buffer, Allocator.TempJob);
			writer.ArrayBegin(sortedObjects.Length);
			for (int i = 0; i < sortedObjects.Length; i++)
			{
				Entity entity2 = sortedObjects[i].entity;
				PrefabData prefabData = sortedObjects[i].prefabData;
				PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(prefabData);
				UIObject component = prefab.GetComponent<UIObject>();
				writer.TypeBegin(TypeNames.kAdvisorItem);
				writer.PropertyName("entity");
				writer.Write(entity2);
				writer.PropertyName("name");
				writer.Write(prefab.name);
				writer.PropertyName("icon");
				if (component.m_Icon == null)
				{
					writer.WriteNull();
				}
				else
				{
					writer.Write(component.m_Icon);
				}
				writer.PropertyName("type");
				writer.Write((!(prefab is TutorialPrefab)) ? 1 : 0);
				writer.PropertyName("shown");
				writer.Write(base.EntityManager.HasComponent<TutorialShown>(entity2));
				writer.PropertyName("force");
				writer.Write(base.EntityManager.HasComponent<ForceAdvisor>(entity2));
				writer.PropertyName("locked");
				writer.Write(base.EntityManager.HasEnabledComponent<Locked>(entity2));
				writer.PropertyName("children");
				BindTutorialGroup(writer, entity2);
				writer.TypeEnd();
			}
			writer.ArrayEnd();
			sortedObjects.Dispose();
		}
		else
		{
			writer.WriteEmptyArray();
		}
	}

	protected void BindActiveTutorialList(IJsonWriter writer)
	{
		Entity activeTutorialList = m_TutorialSystem.activeTutorialList;
		if (activeTutorialList != Entity.Null)
		{
			TutorialListPrefab prefab = m_PrefabSystem.GetPrefab<TutorialListPrefab>(activeTutorialList);
			NativeList<Entity> visibleListTutorials = GetVisibleListTutorials(activeTutorialList, Allocator.Temp);
			NativeList<Entity> listHintTutorials = GetListHintTutorials(activeTutorialList, Allocator.Temp);
			try
			{
				writer.TypeBegin(TypeNames.kTutorialList);
				writer.PropertyName("entity");
				writer.Write(activeTutorialList);
				writer.PropertyName("name");
				writer.Write(prefab.name);
				writer.PropertyName("tutorials");
				writer.ArrayBegin(visibleListTutorials.Length);
				foreach (Entity item in visibleListTutorials)
				{
					BindTutorial(writer, item);
				}
				writer.ArrayEnd();
				writer.PropertyName("hints");
				writer.ArrayBegin(listHintTutorials.Length);
				foreach (Entity item2 in listHintTutorials)
				{
					BindTutorial(writer, item2);
				}
				writer.ArrayEnd();
				writer.PropertyName("intro");
				writer.Write(m_TutorialSystem.showListReminder);
				writer.TypeEnd();
				return;
			}
			finally
			{
				visibleListTutorials.Dispose();
			}
		}
		writer.WriteNull();
	}

	private NativeList<Entity> GetVisibleListTutorials(Entity listEntity, Allocator allocator)
	{
		DynamicBuffer<TutorialRef> buffer = base.EntityManager.GetBuffer<TutorialRef>(listEntity, isReadOnly: true);
		ComponentLookup<TutorialActivationData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tutorials_TutorialActivationData_RW_ComponentLookup, ref base.CheckedStateRef);
		NativeList<Entity> result = new NativeList<Entity>(buffer.Length, allocator);
		foreach (TutorialRef item in buffer)
		{
			TutorialRef current = item;
			if (!componentLookup.HasComponent(current.m_Tutorial))
			{
				result.Add(in current.m_Tutorial);
			}
		}
		return result;
	}

	private NativeList<Entity> GetListHintTutorials(Entity listEntity, Allocator allocator)
	{
		DynamicBuffer<TutorialRef> buffer = base.EntityManager.GetBuffer<TutorialRef>(listEntity, isReadOnly: true);
		ComponentLookup<TutorialActivationData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tutorials_TutorialActivationData_RW_ComponentLookup, ref base.CheckedStateRef);
		NativeList<Entity> result = new NativeList<Entity>(buffer.Length, allocator);
		foreach (TutorialRef item in buffer)
		{
			TutorialRef current = item;
			if (componentLookup.HasComponent(current.m_Tutorial))
			{
				result.Add(in current.m_Tutorial);
			}
		}
		return result;
	}

	private bool AlternativeCompleted(Entity tutorial)
	{
		if (base.EntityManager.TryGetBuffer(tutorial, isReadOnly: true, out DynamicBuffer<Game.Tutorials.TutorialAlternative> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				if (base.EntityManager.HasComponent<TutorialCompleted>(buffer[i].m_Alternative))
				{
					return true;
				}
			}
		}
		return false;
	}

	protected void BindTutorial(IJsonWriter writer, Entity tutorialEntity)
	{
		if (base.EntityManager.HasComponent<TutorialData>(tutorialEntity))
		{
			TutorialPrefab prefab = m_PrefabSystem.GetPrefab<TutorialPrefab>(tutorialEntity);
			writer.TypeBegin(TypeNames.kTutorial);
			writer.PropertyName("entity");
			writer.Write(tutorialEntity);
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("icon");
			writer.Write(ImageSystem.GetIcon(prefab));
			writer.PropertyName("locked");
			writer.Write(base.EntityManager.HasEnabledComponent<Locked>(tutorialEntity));
			writer.PropertyName("priority");
			writer.Write(prefab.m_Priority);
			writer.PropertyName("active");
			writer.Write(base.EntityManager.HasComponent<TutorialActive>(tutorialEntity));
			writer.PropertyName("completed");
			writer.Write(base.EntityManager.HasComponent<TutorialCompleted>(tutorialEntity) || AlternativeCompleted(tutorialEntity));
			writer.PropertyName("shown");
			writer.Write(base.EntityManager.HasComponent<TutorialShown>(tutorialEntity));
			writer.PropertyName("force");
			writer.Write(base.EntityManager.HasComponent<ForceAdvisor>(tutorialEntity));
			writer.PropertyName("mandatory");
			writer.Write(prefab.m_Mandatory);
			writer.PropertyName("advisorActivation");
			writer.Write(base.EntityManager.HasComponent<AdvisorActivation>(m_TutorialSystem.activeTutorial));
			DynamicBuffer<TutorialPhaseRef> buffer = base.EntityManager.GetBuffer<TutorialPhaseRef>(tutorialEntity, isReadOnly: true);
			writer.PropertyName("phases");
			int num = 0;
			for (int i = 0; i < buffer.Length; i++)
			{
				if (TutorialSystem.IsValidControlScheme(buffer[i].m_Phase, m_PrefabSystem))
				{
					num++;
				}
			}
			writer.ArrayBegin(num);
			for (int j = 0; j < buffer.Length; j++)
			{
				Entity phase = buffer[j].m_Phase;
				if (TutorialSystem.IsValidControlScheme(phase, m_PrefabSystem))
				{
					BindTutorialPhase(writer, phase);
				}
			}
			writer.ArrayEnd();
			writer.PropertyName("filters");
			writer.Write(GetFilters(prefab));
			writer.PropertyName("alternatives");
			if (base.EntityManager.TryGetBuffer(tutorialEntity, isReadOnly: true, out DynamicBuffer<Game.Tutorials.TutorialAlternative> buffer2))
			{
				writer.ArrayBegin(buffer2.Length);
				for (int k = 0; k < buffer2.Length; k++)
				{
					writer.Write(buffer2[k].m_Alternative);
				}
				writer.ArrayEnd();
			}
			else
			{
				writer.WriteNull();
			}
			writer.TypeEnd();
		}
		else
		{
			writer.WriteNull();
		}
	}

	protected void BindTutorialPhase(IJsonWriter writer, Entity phaseEntity)
	{
		if (base.EntityManager.TryGetComponent<TutorialPhaseData>(phaseEntity, out var component))
		{
			TutorialPhasePrefab prefab = m_PrefabSystem.GetPrefab<TutorialPhasePrefab>(phaseEntity);
			TutorialBalloonPrefab tutorialBalloonPrefab = prefab as TutorialBalloonPrefab;
			writer.TypeBegin(TypeNames.kTutorialPhase);
			writer.PropertyName("entity");
			writer.Write(phaseEntity);
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("type");
			writer.Write((int)component.m_Type);
			writer.PropertyName("active");
			writer.Write(base.EntityManager.HasComponent<TutorialPhaseActive>(phaseEntity));
			writer.PropertyName("shown");
			writer.Write(base.EntityManager.HasComponent<TutorialPhaseShown>(phaseEntity));
			writer.PropertyName("force");
			writer.Write(base.EntityManager.HasComponent<ForceAdvisor>(phaseEntity));
			writer.PropertyName("completed");
			writer.Write(base.EntityManager.HasComponent<TutorialPhaseCompleted>(phaseEntity));
			writer.PropertyName("forcesCompletion");
			writer.Write(base.EntityManager.HasComponent<Game.Tutorials.ForceTutorialCompletion>(phaseEntity));
			writer.PropertyName("isBranch");
			writer.Write(base.EntityManager.HasComponent<TutorialPhaseBranch>(phaseEntity));
			writer.PropertyName("image");
			writer.Write((!string.IsNullOrWhiteSpace(prefab.m_Image)) ? prefab.m_Image : null);
			writer.PropertyName("overrideImagePS");
			writer.Write((!string.IsNullOrWhiteSpace(prefab.m_OverrideImagePS)) ? prefab.m_OverrideImagePS : null);
			writer.PropertyName("overrideImageXbox");
			writer.Write((!string.IsNullOrWhiteSpace(prefab.m_OverrideImageXBox)) ? prefab.m_OverrideImageXBox : null);
			writer.PropertyName("icon");
			writer.Write((!string.IsNullOrWhiteSpace(prefab.m_Icon)) ? prefab.m_Icon : null);
			writer.PropertyName("titleVisible");
			writer.Write(prefab.m_TitleVisible);
			writer.PropertyName("descriptionVisible");
			writer.Write(prefab.m_DescriptionVisible);
			writer.PropertyName("balloonTargets");
			if (tutorialBalloonPrefab == null)
			{
				writer.WriteEmptyArray();
			}
			else
			{
				writer.Write((IList<TutorialBalloonPrefab.BalloonUITarget>)tutorialBalloonPrefab.m_UITargets);
			}
			writer.PropertyName("controlScheme");
			writer.Write((int)prefab.m_ControlScheme);
			writer.PropertyName("trigger");
			if (base.EntityManager.TryGetComponent<TutorialTrigger>(phaseEntity, out var component2))
			{
				TutorialTriggerPrefabBase prefab2 = m_PrefabSystem.GetPrefab<TutorialTriggerPrefabBase>(component2.m_Trigger);
				writer.TypeBegin(TypeNames.kTutorialTrigger);
				writer.PropertyName("entity");
				writer.Write(component2.m_Trigger);
				writer.PropertyName("name");
				writer.Write(prefab2.name);
				Dictionary<int, List<string>> blinkTags = prefab2.GetBlinkTags();
				List<int> list = blinkTags.Keys.ToList();
				list.Sort();
				writer.PropertyName("blinkTags");
				writer.ArrayBegin(list.Count);
				foreach (int item in list)
				{
					List<string> value = blinkTags[item];
					writer.Write((IList<string>)value);
				}
				writer.ArrayEnd();
				writer.PropertyName("displayUI");
				writer.Write(prefab2.m_DisplayUI);
				writer.PropertyName("active");
				writer.Write(base.EntityManager.HasComponent<TriggerActive>(component2.m_Trigger));
				writer.PropertyName("completed");
				writer.Write(base.EntityManager.HasComponent<TriggerCompleted>(component2.m_Trigger));
				writer.PropertyName("preCompleted");
				writer.Write(base.EntityManager.HasComponent<TriggerPreCompleted>(component2.m_Trigger));
				writer.PropertyName("phaseBranching");
				writer.Write(prefab2.phaseBranching);
				writer.TypeEnd();
			}
			else
			{
				writer.WriteNull();
			}
			writer.TypeEnd();
		}
		else
		{
			writer.WriteNull();
		}
	}

	private void ActivateTutorial(Entity tutorial)
	{
		m_TutorialSystem.SetTutorial(tutorial, Entity.Null);
	}

	private void ActivateTutorialPhase(Entity tutorial, Entity phase)
	{
		m_TutorialSystem.SetTutorial(tutorial, phase);
	}

	private void ForceTutorial(Entity tutorial, Entity phase, bool advisorActivation)
	{
		m_TutorialSystem.ForceTutorial(tutorial, phase, advisorActivation);
	}

	protected virtual void CompleteActiveTutorialPhase()
	{
		if (GameManager.instance.gameMode.IsGame())
		{
			m_TutorialSystem.CompleteCurrentTutorialPhase();
		}
	}

	private void CompleteActiveTutorial()
	{
		m_TutorialSystem.CompleteTutorial(m_TutorialSystem.activeTutorial);
	}

	private void CompleteIntro(bool tutorialEnabled)
	{
		m_TutorialSystem.mode = TutorialMode.Default;
		m_TutorialSystem.tutorialEnabled = tutorialEnabled;
	}

	private void OnSetTutorialTagActive(string tag, bool active)
	{
		m_ActivationSystem.SetTag(tag, active);
		if (!active)
		{
			m_DeactivationSystem.DeactivateTag(tag);
		}
	}

	private void ActivateTutorialTrigger(string trigger)
	{
		m_TriggerSystem.ActivateTrigger(trigger);
	}

	private void DisactivateTutorialTrigger(string trigger)
	{
		m_TriggerSystem.DisactivateTrigger(trigger);
	}

	private static string[] GetFilters(TutorialPrefab prefab)
	{
		TutorialControlSchemeActivation component = prefab.GetComponent<TutorialControlSchemeActivation>();
		if (!(component != null))
		{
			return null;
		}
		return new string[1] { component.m_ControlScheme.ToString() };
	}

	protected override void OnGameLoadingComplete(Purpose purpose, GameMode gameMode)
	{
		base.OnGameLoadingComplete(purpose, gameMode);
		base.Enabled = gameMode.IsGameOrEditor();
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
	public TutorialsUISystem()
	{
	}
}
