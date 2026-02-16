using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PollutionData : IComponentData, IQueryTypeParameter, ICombineData<PollutionData>, ISerializable
{
	public float m_GroundPollution;

	public float m_AirPollution;

	public float m_NoisePollution;

	public bool m_ScaleWithRenters;

	public float GetValue(BuildingStatusType statusType)
	{
		return statusType switch
		{
			BuildingStatusType.GroundPollutionSource => m_GroundPollution,
			BuildingStatusType.AirPollutionSource => m_AirPollution,
			BuildingStatusType.NoisePollutionSource => m_NoisePollution,
			_ => 0f,
		};
	}

	public void AddArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public void Combine(PollutionData otherData)
	{
		m_GroundPollution += otherData.m_GroundPollution;
		m_AirPollution += otherData.m_AirPollution;
		m_NoisePollution += otherData.m_NoisePollution;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float groundPollution = m_GroundPollution;
		writer.Write(groundPollution);
		float airPollution = m_AirPollution;
		writer.Write(airPollution);
		float noisePollution = m_NoisePollution;
		writer.Write(noisePollution);
		bool scaleWithRenters = m_ScaleWithRenters;
		writer.Write(scaleWithRenters);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version > Version.pollutionMultiplierChange)
		{
			ref bool scaleWithRenters = ref m_ScaleWithRenters;
			reader.Read(out scaleWithRenters);
		}
		else
		{
			m_ScaleWithRenters = true;
		}
		if (reader.context.version < Version.pollutionFloatFix)
		{
			reader.Read(out int value);
			m_GroundPollution = value;
			reader.Read(out int value2);
			m_AirPollution = value2;
			reader.Read(out int value3);
			m_NoisePollution = value3;
		}
		else
		{
			ref float groundPollution = ref m_GroundPollution;
			reader.Read(out groundPollution);
			ref float airPollution = ref m_AirPollution;
			reader.Read(out airPollution);
			ref float noisePollution = ref m_NoisePollution;
			reader.Read(out noisePollution);
		}
	}
}
