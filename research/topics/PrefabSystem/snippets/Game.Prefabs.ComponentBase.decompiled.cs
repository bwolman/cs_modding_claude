using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Logging;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[Serializable]
public abstract class ComponentBase : ScriptableObject, IComponentBase, IComparable
{
	protected static ILog baseLog;

	public bool active = true;

	public virtual bool ignoreUnlockDependencies => false;

	public PrefabBase prefab { get; set; }

	public virtual IEnumerable<string> modTags => Enumerable.Empty<string>();

	public virtual string GetDebugString()
	{
		return GetType().Name;
	}

	protected virtual void OnEnable()
	{
		baseLog = LogManager.GetLogger("SceneFlow");
	}

	protected virtual void OnDisable()
	{
	}

	public T GetComponent<T>() where T : ComponentBase
	{
		if (prefab == null)
		{
			throw new NullReferenceException($"GetComponent<{typeof(T)}>() -> prefab is null");
		}
		if (prefab.TryGet<T>(out var component))
		{
			return component;
		}
		return null;
	}

	public ComponentBase GetComponentExactly(Type type)
	{
		if (prefab == null)
		{
			throw new NullReferenceException($"GetComponentExactly<{type}>() -> prefab is null");
		}
		if (prefab.TryGetExactly(type, out var component))
		{
			return component;
		}
		return null;
	}

	public bool GetComponents<T>(List<T> list) where T : ComponentBase
	{
		Type typeFromHandle = typeof(T);
		if (prefab == null)
		{
			throw new NullReferenceException($"GetComponents<{typeFromHandle}>() -> prefab is null");
		}
		return prefab.TryGet(list);
	}

	public virtual void GetDependencies(List<PrefabBase> prefabs)
	{
	}

	public virtual void Initialize(EntityManager entityManager, Entity entity)
	{
	}

	public virtual void LateInitialize(EntityManager entityManager, Entity entity)
	{
	}

	public abstract void GetPrefabComponents(HashSet<ComponentType> components);

	public abstract void GetArchetypeComponents(HashSet<ComponentType> components);

	public int CompareTo(object obj)
	{
		if (obj == null)
		{
			return 1;
		}
		ComponentBase componentBase = obj as ComponentBase;
		if (componentBase != null)
		{
			return base.name.CompareTo(componentBase.name);
		}
		throw new ArgumentException("Object is not a ComponentBase");
	}

	public void CopyToClipboard()
	{
		ProxyObject proxyObject = JSON.Load(JsonUtility.ToJson(this)) as ProxyObject;
		if (proxyObject != null)
		{
			proxyObject.Remove("components");
			proxyObject.Remove("active");
			proxyObject.Remove("m_NameOverride");
		}
		GUIUtility.systemCopyBuffer = proxyObject.ToJSON();
	}

	public void PasteFromClipboard()
	{
		try
		{
			JsonUtility.FromJsonOverwrite(GUIUtility.systemCopyBuffer, this);
		}
		catch (Exception exception)
		{
			baseLog.Error(exception);
		}
	}
}
