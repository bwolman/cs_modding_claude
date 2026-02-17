using System;
using System.Collections.Generic;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new System.Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class ElectricityOutsideConnection : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		components.Add(ComponentType.ReadWrite<Game.Objects.ElectricityOutsideConnection>());
		components.Add(ComponentType.ReadWrite<Game.Objects.OutsideConnection>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		base.Initialize(entityManager, entity);
	}
}
