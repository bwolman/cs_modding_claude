using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PrefabData : IComponentData, IQueryTypeParameter, IEnableableComponent, ISerializable, ISerializeAsEnabled, IEquatable<PrefabData>
{
	public int m_Index;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Index);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Index);
	}

	public bool Equals(PrefabData other)
	{
		return m_Index == other.m_Index;
	}

	public override int GetHashCode()
	{
		return m_Index;
	}
}
