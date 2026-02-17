using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct OutsideConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Delay;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Delay);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Delay);
	}
}
