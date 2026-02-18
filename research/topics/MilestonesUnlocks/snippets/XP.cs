using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct XP : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_XP;

	public int m_MaximumPopulation;

	public int m_MaximumIncome;

	public XPRewardFlags m_XPRewardRecord;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int xP = m_XP;
		writer.Write(xP);
		int maximumPopulation = m_MaximumPopulation;
		writer.Write(maximumPopulation);
		int maximumIncome = m_MaximumIncome;
		writer.Write(maximumIncome);
		XPRewardFlags xPRewardRecord = m_XPRewardRecord;
		writer.Write((byte)xPRewardRecord);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int xP = ref m_XP;
		reader.Read(out xP);
		if (reader.context.version >= Version.xpMaximumStats)
		{
			ref int maximumPopulation = ref m_MaximumPopulation;
			reader.Read(out maximumPopulation);
			ref int maximumIncome = ref m_MaximumIncome;
			reader.Read(out maximumIncome);
		}
		if (reader.context.version >= Version.xpRewardRecord)
		{
			reader.Read(out byte value);
			m_XPRewardRecord = (XPRewardFlags)value;
		}
	}
}
