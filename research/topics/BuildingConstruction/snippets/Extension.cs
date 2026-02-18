using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Extension : IComponentData, IQueryTypeParameter, ISerializable
{
	public ExtensionFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_Flags = (ExtensionFlags)value;
	}
}
