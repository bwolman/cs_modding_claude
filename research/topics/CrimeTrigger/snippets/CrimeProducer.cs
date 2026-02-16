using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct CrimeProducer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_PatrolRequest;

	public float m_Crime;

	public byte m_DispatchIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_PatrolRequest);
		writer.Write(m_Crime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_PatrolRequest);
		reader.Read(out m_Crime);
	}
}
