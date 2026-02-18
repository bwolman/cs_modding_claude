using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(1)]
public struct ServiceUpgradeBuilding : IBufferElementData
{
	public Entity m_Building;

	public ServiceUpgradeBuilding(Entity building)
	{
		m_Building = building;
	}
}
