using System;
using System.Collections.Generic;
using Game.Buildings;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class Attraction : ComponentBase, IServiceUpgrade
{
	public int m_Attractiveness;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AttractionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null && m_Attractiveness > 0)
		{
			components.Add(ComponentType.ReadWrite<AttractivenessProvider>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		if (m_Attractiveness > 0)
		{
			components.Add(ComponentType.ReadWrite<AttractivenessProvider>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		AttractionData componentData = new AttractionData
		{
			m_Attractiveness = m_Attractiveness
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
