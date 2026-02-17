using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Renter : IBufferElementData, IEmptySerializable
{
	public Entity m_Renter;

	public static implicit operator Entity(Renter renter)
	{
		return renter.m_Renter;
	}
}
