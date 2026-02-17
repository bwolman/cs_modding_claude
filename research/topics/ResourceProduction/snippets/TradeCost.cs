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
		sbyte num = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		((IWriter)writer/*cast due to .constrained prefix*/).Write(num);
		float buyCost = m_BuyCost;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(buyCost);
		float sellCost = m_SellCost;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(sellCost);
		long lastTransferRequestTime = m_LastTransferRequestTime;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(lastTransferRequestTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		sbyte index = default(sbyte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref index);
		ref float buyCost = ref m_BuyCost;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref buyCost);
		if (float.IsNaN(m_BuyCost))
		{
			m_BuyCost = 0f;
		}
		ref float sellCost = ref m_SellCost;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref sellCost);
		if (float.IsNaN(m_SellCost))
		{
			m_SellCost = 0f;
		}
		ref long lastTransferRequestTime = ref m_LastTransferRequestTime;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref lastTransferRequestTime);
		m_Resource = EconomyUtils.GetResource(index);
	}
}
