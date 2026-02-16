using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

[InternalBufferCapacity(5)]
public struct HouseholdCitizen : IBufferElementData, IEquatable<HouseholdCitizen>, IEmptySerializable
{
	public Entity m_Citizen;

	public HouseholdCitizen(Entity citizen)
	{
		m_Citizen = citizen;
	}

	public bool Equals(HouseholdCitizen other)
	{
		return m_Citizen.Equals(other.m_Citizen);
	}

	public override int GetHashCode()
	{
		return m_Citizen.GetHashCode();
	}

	public static implicit operator Entity(HouseholdCitizen citizen)
	{
		return citizen.m_Citizen;
	}
}
