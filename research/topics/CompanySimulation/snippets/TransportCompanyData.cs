using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Companies;

public struct TransportCompanyData : IComponentData, IQueryTypeParameter, ICombineData<TransportCompanyData>, ISerializable
{
	public int m_MaxTransports;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_MaxTransports);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_MaxTransports);
	}

	public void Combine(TransportCompanyData other)
	{
		m_MaxTransports += other.m_MaxTransports;
	}
}
