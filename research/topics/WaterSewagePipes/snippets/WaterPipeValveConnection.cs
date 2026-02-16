using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct WaterPipeValveConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ValveNode;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_ValveNode);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_ValveNode);
	}
}
