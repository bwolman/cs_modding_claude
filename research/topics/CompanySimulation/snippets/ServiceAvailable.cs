using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct ServiceAvailable : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_ServiceAvailable;

	public float m_MeanPriority;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int serviceAvailable = m_ServiceAvailable;
		writer.Write(serviceAvailable);
		float meanPriority = m_MeanPriority;
		writer.Write(meanPriority);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int serviceAvailable = ref m_ServiceAvailable;
		reader.Read(out serviceAvailable);
		ref float meanPriority = ref m_MeanPriority;
		reader.Read(out meanPriority);
	}
}
