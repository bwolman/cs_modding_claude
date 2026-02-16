using System;
using System.Collections.Generic;
using Game.Buildings;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[RequireComponent(typeof(PowerPlant))]
[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class GroundWaterPowered : ComponentBase, IServiceUpgrade
{
	public int m_Production;

	public int m_MaximumGroundWater;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<GroundWaterPoweredData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Efficiency>());
			components.Add(ComponentType.ReadWrite<RenewableElectricityProduction>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Efficiency>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new GroundWaterPoweredData
		{
			m_Production = m_Production,
			m_MaximumGroundWater = m_MaximumGroundWater
		});
	}
}
