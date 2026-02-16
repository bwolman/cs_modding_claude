using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct Criminal : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public ushort m_JailTime;

	public CriminalFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Event);
		writer.Write(m_JailTime);
		writer.Write((ushort)m_Flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Event);
		reader.Read(out m_JailTime);
		reader.Read(out ushort value);
		m_Flags = (CriminalFlags)value;
	}
}
