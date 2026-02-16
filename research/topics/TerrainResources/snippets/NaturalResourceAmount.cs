// Source: Game.dll -> Game.Simulation.NaturalResourceAmount
// Decompiled with ILSpy

using Colossal.Serialization.Entities;

namespace Game.Simulation;

public struct NaturalResourceAmount : IStrideSerializable, ISerializable
{
	public ushort m_Base;

	public ushort m_Used;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ushort value = m_Base;
		writer.Write(value);
		ushort used = m_Used;
		writer.Write(used);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref ushort value = ref m_Base;
		reader.Read(out value);
		ref ushort used = ref m_Used;
		reader.Read(out used);
	}

	public int GetStride(Context context)
	{
		return 4;
	}
}
