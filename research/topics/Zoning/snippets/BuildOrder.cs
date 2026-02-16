using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Zones;

public struct BuildOrder : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_Order;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Order);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Order);
	}
}
