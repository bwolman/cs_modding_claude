// Source: Game.dll -> Game.Simulation.NaturalResourceCell
// Decompiled with ILSpy

using Colossal.Serialization.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct NaturalResourceCell : IStrideSerializable, ISerializable
{
	public NaturalResourceAmount m_Fertility;

	public NaturalResourceAmount m_Ore;

	public NaturalResourceAmount m_Oil;

	public NaturalResourceAmount m_Fish;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		NaturalResourceAmount fertility = m_Fertility;
		writer.Write(fertility);
		NaturalResourceAmount ore = m_Ore;
		writer.Write(ore);
		NaturalResourceAmount oil = m_Oil;
		writer.Write(oil);
		NaturalResourceAmount fish = m_Fish;
		writer.Write(fish);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref NaturalResourceAmount fertility = ref m_Fertility;
		reader.Read(out fertility);
		ref NaturalResourceAmount ore = ref m_Ore;
		reader.Read(out ore);
		ref NaturalResourceAmount oil = ref m_Oil;
		reader.Read(out oil);
		if (reader.context.format.Has(FormatTags.FishResource))
		{
			ref NaturalResourceAmount fish = ref m_Fish;
			reader.Read(out fish);
		}
	}

	public int GetStride(Context context)
	{
		return m_Fertility.GetStride(context) + m_Ore.GetStride(context) + m_Oil.GetStride(context);
	}

	public float4 GetBaseResources()
	{
		return new float4((int)m_Fertility.m_Base, (int)m_Ore.m_Base, (int)m_Oil.m_Base, (int)m_Fish.m_Base);
	}

	public float4 GetUsedResources()
	{
		return new float4((int)m_Fertility.m_Used, (int)m_Ore.m_Used, (int)m_Oil.m_Used, (int)m_Oil.m_Used);
	}
}
