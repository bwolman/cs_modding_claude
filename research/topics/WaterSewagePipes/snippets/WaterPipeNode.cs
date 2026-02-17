using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct WaterPipeNode : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Index;

	public float m_FreshPollution;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_FreshPollution);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_FreshPollution);
	}
}
