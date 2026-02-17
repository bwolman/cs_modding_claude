using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct Employee : IBufferElementData, ISerializable
{
	public Entity m_Worker;

	public byte m_Level;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity worker = m_Worker;
		writer.Write(worker);
		byte level = m_Level;
		writer.Write(level);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity worker = ref m_Worker;
		reader.Read(out worker);
		ref byte level = ref m_Level;
		reader.Read(out level);
	}
}
