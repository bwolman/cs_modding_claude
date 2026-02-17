using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPrefab) })]
public class WaterPipeConnection : ComponentBase
{
	public int m_FreshCapacity = 1073741823;

	public int m_SewageCapacity = 1073741823;

	public int m_StormCapacity;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WaterPipeConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Net.WaterPipeConnection>());
		}
	}
}
