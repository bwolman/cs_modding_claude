using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Companies;

public struct TradeCost : IBufferElementData, ISerializable
{
	public Resource m_Resource;

	public float m_BuyCost;

	public float m_SellCost;

	public long m_LastTransferRequestTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		float buyCost = m_BuyCost;
		writer.Write(buyCost);
		float sellCost = m_SellCost;
		writer.Write(sellCost);
		long lastTransferRequestTime = m_LastTransferRequestTime;
		writer.Write(lastTransferRequestTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		ref float buyCost = ref m_BuyCost;
		reader.Read(out buyCost);
		if (float.IsNaN(m_BuyCost))
		{
			m_BuyCost = 0f;
		}
		ref float sellCost = ref m_SellCost;
		reader.Read(out sellCost);
		if (float.IsNaN(m_SellCost))
		{
			m_SellCost = 0f;
		}
		ref long lastTransferRequestTime = ref m_LastTransferRequestTime;
		reader.Read(out lastTransferRequestTime);
		m_Resource = EconomyUtils.GetResource(value);
	}
}
