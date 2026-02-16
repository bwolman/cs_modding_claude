using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct Student : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_School;

	public float m_LastCommuteTime;

	public byte m_Level;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity school = m_School;
		writer.Write(school);
		float lastCommuteTime = m_LastCommuteTime;
		writer.Write(lastCommuteTime);
		byte level = m_Level;
		writer.Write(level);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity school = ref m_School;
		reader.Read(out school);
		ref float lastCommuteTime = ref m_LastCommuteTime;
		reader.Read(out lastCommuteTime);
		if (reader.context.version >= Version.educationTrading)
		{
			ref byte level = ref m_Level;
			reader.Read(out level);
		}
		else
		{
			m_Level = byte.MaxValue;
		}
	}
}
