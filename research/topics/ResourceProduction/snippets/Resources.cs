using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Economy;

public struct Resources : IBufferElementData, ISerializable
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
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		sbyte index = default(sbyte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref index);
		ref int amount = ref m_Amount;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref amount);
		m_Resource = EconomyUtils.GetResource(index);
		Context context = ((IReader)reader).context;
		if (((Context)(ref context)).version < Version.resetNegativeResource && m_Resource != Resource.Money)
		{
			if (m_Amount > 1000000)
			{
				m_Amount = 1000000;
			}
			else if (m_Amount < 0)
			{
				m_Amount = 0;
			}
		}
	}
}
