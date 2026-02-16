using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Buildings;

public struct PollutionEmitModifier : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_GroundPollutionModifier;

	public float m_AirPollutionModifier;

	public float m_NoisePollutionModifier;

	public void UpdatePollutionData(ref PollutionData pollutionData)
	{
		pollutionData.m_GroundPollution += m_GroundPollutionModifier * pollutionData.m_GroundPollution;
		pollutionData.m_AirPollution += m_AirPollutionModifier * pollutionData.m_AirPollution;
		pollutionData.m_NoisePollution += m_NoisePollutionModifier * pollutionData.m_NoisePollution;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float groundPollutionModifier = m_GroundPollutionModifier;
		writer.Write(groundPollutionModifier);
		float airPollutionModifier = m_AirPollutionModifier;
		writer.Write(airPollutionModifier);
		float noisePollutionModifier = m_NoisePollutionModifier;
		writer.Write(noisePollutionModifier);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float groundPollutionModifier = ref m_GroundPollutionModifier;
		reader.Read(out groundPollutionModifier);
		ref float airPollutionModifier = ref m_AirPollutionModifier;
		reader.Read(out airPollutionModifier);
		ref float noisePollutionModifier = ref m_NoisePollutionModifier;
		reader.Read(out noisePollutionModifier);
	}
}
