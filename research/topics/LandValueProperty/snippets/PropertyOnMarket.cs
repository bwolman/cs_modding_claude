using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct PropertyOnMarket : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_AskingRent;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_AskingRent);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_AskingRent);
	}
}
