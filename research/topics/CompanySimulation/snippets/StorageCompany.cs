using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct StorageCompany : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_LastTradePartner;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_LastTradePartner);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_LastTradePartner);
	}
}
