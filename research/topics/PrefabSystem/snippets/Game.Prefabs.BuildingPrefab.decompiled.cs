using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Effects;
using Game.Net;
using Game.Objects;
using Game.Policies;
using Game.Routes;
using Game.Simulation;
using Game.UI.Editor;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { })]
public class BuildingPrefab : StaticObjectPrefab
{
	public BuildingAccessType m_AccessType;

	[CustomField(typeof(BuildingLotWidthField))]
	public int m_LotWidth = 4;

	[CustomField(typeof(BuildingLotDepthField))]
	public int m_LotDepth = 4;

	public int lotSize => m_LotWidth * m_LotDepth;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<BuildingData>());
		components.Add(ComponentType.ReadWrite<PlaceableObjectData>());
		components.Add(ComponentType.ReadWrite<BuildingTerraformData>());
		components.Add(ComponentType.ReadWrite<Effect>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Building>());
		components.Add(ComponentType.ReadWrite<CitizenPresence>());
		components.Add(ComponentType.ReadWrite<SpawnLocationElement>());
		components.Add(ComponentType.ReadWrite<CurrentDistrict>());
		components.Add(ComponentType.ReadWrite<UpdateFrame>());
		components.Add(ComponentType.ReadWrite<Game.Objects.Color>());
		components.Add(ComponentType.ReadWrite<Game.Objects.Surface>());
		components.Add(ComponentType.ReadWrite<BuildingModifier>());
		components.Add(ComponentType.ReadWrite<Policy>());
		components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
		components.Add(ComponentType.ReadWrite<Game.Objects.SubObject>());
		components.Add(ComponentType.ReadWrite<Game.Buildings.Lot>());
		components.Add(ComponentType.ReadWrite<EnabledEffect>());
	}

	protected override void RefreshArchetype(EntityManager entityManager, Entity entity)
	{
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		if (entityManager.HasComponent<BuildingUpgradeElement>(entity))
		{
			hashSet.Add(ComponentType.ReadWrite<InstalledUpgrade>());
			hashSet.Add(ComponentType.ReadWrite<Game.Net.SubNet>());
			hashSet.Add(ComponentType.ReadWrite<SubRoute>());
		}
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

	public void AddUpgrade(EntityManager entityManager, ServiceUpgrade upgrade)
	{
		if (entityManager.World.GetExistingSystemManaged<PrefabSystem>().TryGetEntity(this, out var entity))
		{
			if (!entityManager.HasComponent<BuildingUpgradeElement>(entity))
			{
				entityManager.AddBuffer<BuildingUpgradeElement>(entity);
				if (entityManager.GetComponentData<ObjectData>(entity).m_Archetype.Valid)
				{
					RefreshArchetype(entityManager, entity);
				}
			}
			return;
		}
		throw new Exception("Building prefab entity not found for upgrade!");
	}
}
