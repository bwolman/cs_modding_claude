using System;
using System.Collections.Generic;
using Game.Buildings;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class SewageOutlet : ComponentBase, IServiceUpgrade
{
	public int m_Capacity = 75;

	public float m_Purification;

	public bool m_AllowSubmerged;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SewageOutletData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Game.Buildings.SewageOutlet>());
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.SewageOutlet>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new SewageOutletData
		{
			m_Capacity = m_Capacity,
			m_Purification = m_Purification
		});
	}
}
