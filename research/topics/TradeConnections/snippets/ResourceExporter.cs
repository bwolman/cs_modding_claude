using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Companies;

public struct ResourceExporter : IComponentData, IQueryTypeParameter, ISerializable
{
	public Resource m_Resource;

	public int m_Amount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		int amount = m_Amount;
		writer.Write(amount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		m_Resource = EconomyUtils.GetResource(value);
	}
}
