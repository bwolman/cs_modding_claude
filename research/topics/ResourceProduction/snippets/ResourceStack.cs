using Colossal.Serialization.Entities;
using Game.Economy;

namespace Game.Prefabs;

public struct ResourceStack : ISerializable
{
	public Resource m_Resource;

	public int m_Amount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte num = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		((IWriter)writer/*cast due to .constrained prefix*/).Write(num);
		int amount = m_Amount;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(amount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		sbyte index = default(sbyte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref index);
		ref int amount = ref m_Amount;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref amount);
		m_Resource = EconomyUtils.GetResource(index);
	}
}
