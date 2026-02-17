using Unity.Entities;

namespace Game.Buildings;

public struct RentAction
{
	public Entity m_Property;

	public Entity m_Renter;

	public RentActionFlags m_Flags;
}
