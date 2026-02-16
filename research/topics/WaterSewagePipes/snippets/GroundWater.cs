using Colossal.Serialization.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct GroundWater : IStrideSerializable, ISerializable
{
	public short m_Amount;

	public short m_Polluted;

	public short m_Max;

	public void Consume(int amount)
	{
		if (m_Amount > 0)
		{
			float num = (float)m_Polluted / (float)m_Amount;
			m_Amount -= (short)math.clamp(amount, 0, m_Amount);
			m_Polluted = (short)math.clamp(math.round(num * (float)m_Amount), 0f, m_Amount);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		short amount = m_Amount;
		writer.Write(amount);
		short polluted = m_Polluted;
		writer.Write(polluted);
		short max = m_Max;
		writer.Write(max);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref short amount = ref m_Amount;
		reader.Read(out amount);
		ref short polluted = ref m_Polluted;
		reader.Read(out polluted);
		ref short max = ref m_Max;
		reader.Read(out max);
		if (reader.context.version < Version.groundWaterPollutionFix)
		{
			m_Amount = (short)math.clamp(m_Amount, 0, m_Max);
			m_Polluted = (short)math.clamp(m_Polluted, 0, m_Amount);
		}
	}

	public int GetStride(Context context)
	{
		return 6;
	}
}
