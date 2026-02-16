using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct ZonePropertiesData : IComponentData, IQueryTypeParameter
{
	public bool m_ScaleResidentials;

	public float m_ResidentialProperties;

	public float m_SpaceMultiplier;

	public Resource m_AllowedSold;

	public Resource m_AllowedManufactured;

	public Resource m_AllowedStored;

	public float m_FireHazardMultiplier;

	public bool m_IgnoreLandValue;
}
