using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct LandValueCell : ILandValueCell, IStrideSerializable, ISerializable
{
	public float m_LandValue;

	public void Add(float amount)
	{
		m_LandValue += amount;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_LandValue);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_LandValue);
	}

	public int GetStride(Context context)
	{
		return 4;
	}
}
