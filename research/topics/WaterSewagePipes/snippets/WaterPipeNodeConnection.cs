using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct WaterPipeNodeConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_WaterPipeNode;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_WaterPipeNode);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_WaterPipeNode);
	}
}
