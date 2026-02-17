using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct ServiceImportBudget : IBufferElementData, ISerializable
{
	public PlayerResource m_Resource;

	public int m_MaximumBudget;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		PlayerResource resource = m_Resource;
		writer.Write((int)resource);
		int maximumBudget = m_MaximumBudget;
		writer.Write(maximumBudget);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_Resource = (PlayerResource)value;
		ref int maximumBudget = ref m_MaximumBudget;
		reader.Read(out maximumBudget);
	}
}
