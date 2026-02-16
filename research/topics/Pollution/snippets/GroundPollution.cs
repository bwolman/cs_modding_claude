using Colossal.Serialization.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct GroundPollution : IPollution, IStrideSerializable, ISerializable
{
	public short m_Pollution;

	public void Add(short amount)
	{
		m_Pollution = (short)math.min(32767, m_Pollution + amount);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Pollution);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref short pollution = ref m_Pollution;
		reader.Read(out pollution);
		if (reader.context.version >= Version.groundPollutionDelta && reader.context.version < Version.removeGroundPollutionDelta)
		{
			reader.Read(out short _);
		}
	}

	public int GetStride(Context context)
	{
		if (context.version < Version.removeGroundPollutionDelta)
		{
			return 4;
		}
		return 2;
	}
}
