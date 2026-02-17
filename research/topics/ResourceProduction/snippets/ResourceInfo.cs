using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Economy;

public struct ResourceInfo : IComponentData, IQueryTypeParameter, ISerializable
{
	public Resource m_Resource;

	public float m_Price;

	public float m_TradeDistance;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte num = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		((IWriter)writer/*cast due to .constrained prefix*/).Write(num);
		float price = m_Price;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(price);
		float tradeDistance = m_TradeDistance;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(tradeDistance);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		sbyte index = default(sbyte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref index);
		ref float price = ref m_Price;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref price);
		ref float tradeDistance = ref m_TradeDistance;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref tradeDistance);
		m_Resource = EconomyUtils.GetResource(index);
	}
}
