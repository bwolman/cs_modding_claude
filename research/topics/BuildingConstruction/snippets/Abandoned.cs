using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Abandoned : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_AbandonmentTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_AbandonmentTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_AbandonmentTime);
	}
}
