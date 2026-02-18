using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct BuildingData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int2 m_LotSize;

	public BuildingFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int2 lotSize = m_LotSize;
		writer.Write(lotSize);
		BuildingFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int2 lotSize = ref m_LotSize;
		reader.Read(out lotSize);
		reader.Read(out uint value);
		m_Flags = (BuildingFlags)value;
	}
}
