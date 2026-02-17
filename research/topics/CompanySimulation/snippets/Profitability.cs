using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct Profitability : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_Profitability;

	public int m_LastTotalWorth;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte profitability = m_Profitability;
		writer.Write(profitability);
		int lastTotalWorth = m_LastTotalWorth;
		writer.Write(lastTotalWorth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref byte profitability = ref m_Profitability;
		reader.Read(out profitability);
		if (reader.context.format.Has(FormatTags.CompanyAndCargoFix))
		{
			ref int lastTotalWorth = ref m_LastTotalWorth;
			reader.Read(out lastTotalWorth);
		}
	}
}
