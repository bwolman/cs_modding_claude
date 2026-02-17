using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct AttractionData : IComponentData, IQueryTypeParameter, ICombineData<AttractionData>, ISerializable
{
	public int m_Attractiveness;

	public void Combine(AttractionData otherData)
	{
		m_Attractiveness += otherData.m_Attractiveness;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Attractiveness);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Attractiveness);
	}
}
