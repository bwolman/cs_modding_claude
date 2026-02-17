using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct TrafficAmbienceCell : IStrideSerializable, ISerializable
{
	public float m_Accumulator;

	public float m_Traffic;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float accumulator = m_Accumulator;
		writer.Write(accumulator);
		float traffic = m_Traffic;
		writer.Write(traffic);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float accumulator = ref m_Accumulator;
		reader.Read(out accumulator);
		ref float traffic = ref m_Traffic;
		reader.Read(out traffic);
	}

	public int GetStride(Context context)
	{
		return 8;
	}
}
