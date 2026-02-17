using Colossal.Serialization.Entities;
using Game.Economy;
using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

public struct BuildingPropertyData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_ResidentialProperties;

	public Resource m_AllowedSold;

	public Resource m_AllowedInput;

	public Resource m_AllowedManufactured;

	public Resource m_AllowedStored;

	public float m_SpaceMultiplier;

	public int CountProperties(AreaType areaType)
	{
		switch (areaType)
		{
		case AreaType.Residential:
			return m_ResidentialProperties;
		case AreaType.Commercial:
			if (m_AllowedSold == Resource.NoResource)
			{
				return 0;
			}
			return 1;
		case AreaType.Industrial:
			if (m_AllowedStored != Resource.NoResource)
			{
				return 1;
			}
			if (m_AllowedManufactured == Resource.NoResource)
			{
				return 0;
			}
			return 1;
		default:
			return 0;
		}
	}

	public int CountProperties()
	{
		return CountProperties(AreaType.Residential) + CountProperties(AreaType.Commercial) + CountProperties(AreaType.Industrial);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int residentialProperties = m_ResidentialProperties;
		writer.Write(residentialProperties);
		float spaceMultiplier = m_SpaceMultiplier;
		writer.Write(spaceMultiplier);
		Resource allowedSold = m_AllowedSold;
		writer.Write((ulong)allowedSold);
		Resource allowedManufactured = m_AllowedManufactured;
		writer.Write((ulong)allowedManufactured);
		Resource allowedStored = m_AllowedStored;
		writer.Write((ulong)allowedStored);
		Resource allowedInput = m_AllowedInput;
		writer.Write((ulong)allowedInput);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int residentialProperties = ref m_ResidentialProperties;
		reader.Read(out residentialProperties);
		ref float spaceMultiplier = ref m_SpaceMultiplier;
		reader.Read(out spaceMultiplier);
		reader.Read(out ulong value);
		reader.Read(out ulong value2);
		reader.Read(out ulong value3);
		m_AllowedSold = (Resource)value;
		m_AllowedManufactured = (Resource)value2;
		m_AllowedStored = (Resource)value3;
		if (reader.context.format.Has(FormatTags.BpPrefabData))
		{
			reader.Read(out ulong value4);
			m_AllowedInput = (Resource)value4;
		}
	}
}
