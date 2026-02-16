using Colossal.Serialization.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct NoisePollution : IPollution, IStrideSerializable, ISerializable
{
	public short m_Pollution;

	public short m_PollutionTemp;

	public void Add(short amount)
	{
		m_PollutionTemp = (short)math.min(32767, m_PollutionTemp + amount);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		short pollution = m_Pollution;
		writer.Write(pollution);
		short pollutionTemp = m_PollutionTemp;
		writer.Write(pollutionTemp);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref short pollution = ref m_Pollution;
		reader.Read(out pollution);
		ref short pollutionTemp = ref m_PollutionTemp;
		reader.Read(out pollutionTemp);
	}

	public int GetStride(Context context)
	{
		return 4;
	}
}
