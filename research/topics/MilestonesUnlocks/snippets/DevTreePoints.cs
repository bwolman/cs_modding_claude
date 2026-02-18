using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct DevTreePoints : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Points;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Points);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Points);
	}
}
