using System.Reflection;
using Game.Economy;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[DefaultMember("Item")]
public struct ResourcePrefabs
{
	private NativeArray<Entity> m_ResourcePrefabs;

	public Entity this[Resource resource]
	{
		get
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			int resourceIndex = EconomyUtils.GetResourceIndex(resource);
			if (resourceIndex >= 0 && resourceIndex < m_ResourcePrefabs.Length)
			{
				return m_ResourcePrefabs[resourceIndex];
			}
			return Entity.Null;
		}
	}

	public ResourcePrefabs(NativeArray<Entity> resourcePrefabs)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		m_ResourcePrefabs = resourcePrefabs;
	}
}
