using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Zones;

public struct Block : IComponentData, IQueryTypeParameter, IEquatable<Block>, ISerializable
{
	public float3 m_Position;

	public float2 m_Direction;

	public int2 m_Size;

	public bool Equals(Block other)
	{
		if (m_Position.Equals(other.m_Position) && m_Direction.Equals(other.m_Direction))
		{
			return m_Size.Equals(other.m_Size);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Position.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		float2 direction = m_Direction;
		writer.Write(direction);
		int2 size = m_Size;
		writer.Write(size);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		ref float2 direction = ref m_Direction;
		reader.Read(out direction);
		ref int2 size = ref m_Size;
		reader.Read(out size);
	}
}
