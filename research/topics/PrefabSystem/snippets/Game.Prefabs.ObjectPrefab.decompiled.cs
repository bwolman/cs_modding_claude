using System;
using System.Collections.Generic;
using Game.Common;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { })]
public class ObjectPrefab : PrefabBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Game.Objects.Object>());
		components.Add(ComponentType.ReadWrite<Transform>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		RefreshArchetype(entityManager, entity);
	}

	protected virtual void RefreshArchetype(EntityManager entityManager, Entity entity)
	{
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		entityManager.SetComponentData(entity, new ObjectData
		{
			m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet))
		});
	}
}
