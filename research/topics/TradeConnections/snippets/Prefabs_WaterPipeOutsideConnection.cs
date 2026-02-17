using System;
using System.Collections.Generic;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class WaterPipeOutsideConnection : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.WaterPipeOutsideConnection>());
		components.Add(ComponentType.ReadWrite<Game.Objects.OutsideConnection>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
	}
}
