using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct StorageCompanyData : IComponentData, IQueryTypeParameter, ISerializable, ICombineData<StorageCompanyData>
{
	public Resource m_StoredResources;

	public int2 m_TransportInterval;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Resource storedResources = m_StoredResources;
		writer.Write((ulong)storedResources);
		int2 transportInterval = m_TransportInterval;
		writer.Write(transportInterval);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out ulong value);
		m_StoredResources = (Resource)value;
		if (reader.context.version > Version.transportInterval)
		{
			ref int2 transportInterval = ref m_TransportInterval;
			reader.Read(out transportInterval);
		}
	}

	public void Combine(StorageCompanyData otherData)
	{
		m_StoredResources |= otherData.m_StoredResources;
	}
}
