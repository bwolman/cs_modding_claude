using Unity.Entities;

namespace Game.Buildings;

public struct RentersUpdated : IComponentData, IQueryTypeParameter
{
	public Entity m_Property;

	public RentersUpdated(Entity property)
	{
		m_Property = property;
	}
}
