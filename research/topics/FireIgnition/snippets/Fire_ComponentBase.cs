using System;
using System.Collections.Generic;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class Fire : ComponentBase
{
	public EventTargetType m_RandomTargetType;

	public float m_StartProbability = 0.01f;

	public float m_StartIntensity = 1f;

	public float m_EscalationRate = 1f / 60f;

	public float m_SpreadProbability = 1f;

	public float m_SpreadRange = 20f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<FireData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Events.Fire>());
		components.Add(ComponentType.ReadWrite<TargetElement>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		FireData componentData = default(FireData);
		componentData.m_RandomTargetType = m_RandomTargetType;
		componentData.m_StartProbability = m_StartProbability;
		componentData.m_StartIntensity = m_StartIntensity;
		componentData.m_EscalationRate = m_EscalationRate;
		componentData.m_SpreadProbability = m_SpreadProbability;
		componentData.m_SpreadRange = m_SpreadRange;
		entityManager.SetComponentData(entity, componentData);
	}
}
