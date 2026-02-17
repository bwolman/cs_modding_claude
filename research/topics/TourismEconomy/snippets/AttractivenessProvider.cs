using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct AttractivenessProvider : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Attractiveness;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Attractiveness);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Attractiveness);
	}
}
