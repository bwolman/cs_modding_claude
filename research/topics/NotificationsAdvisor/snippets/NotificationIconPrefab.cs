using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Common;
using Game.Notifications;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Notifications/", new Type[] { })]
public class NotificationIconPrefab : PrefabBase
{
	public Texture2D m_Icon;

	public string m_Description;

	public string m_TargetDescription;

	public Bounds1 m_DisplaySize = new Bounds1(3f, 3f);

	public Bounds1 m_PulsateAmplitude = new Bounds1(0.01f, 0.1f);

	public bool m_EnabledByDefault = true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<NotificationIconData>());
		components.Add(ComponentType.ReadWrite<NotificationIconDisplayData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (!m_EnabledByDefault)
		{
			entityManager.SetComponentEnabled<NotificationIconDisplayData>(entity, value: false);
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Icon>());
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
		entityManager.SetComponentData(entity, new NotificationIconData
		{
			m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet))
		});
	}
}
