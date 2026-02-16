using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Zones;

[InternalBufferCapacity(1)]
public struct VacantLot : IBufferElementData, IEquatable<VacantLot>, ISerializable
{
	public int4 m_Area;

	public ZoneType m_Type;

	public short m_Height;

	public LotFlags m_Flags;

	public VacantLot(int2 min, int2 max, ZoneType type, int height, LotFlags flags)
	{
		m_Area = new int4(min.x, max.x, min.y, max.y);
		m_Type = type;
		m_Height = (short)height;
		m_Flags = flags;
	}

	public bool Equals(VacantLot other)
	{
		return m_Area.Equals(other.m_Area);
	}

	public override int GetHashCode()
	{
		return (17 * 31 + m_Area.GetHashCode()) * 31 + m_Type.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int4 area = m_Area;
		writer.Write(area);
		ZoneType type = m_Type;
		writer.Write(type);
		short height = m_Height;
		writer.Write(height);
		LotFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int4 area = ref m_Area;
		reader.Read(out area);
		ref ZoneType type = ref m_Type;
		reader.Read(out type);
		if (reader.context.version >= Version.zoneHeightLimit)
		{
			ref short height = ref m_Height;
			reader.Read(out height);
		}
		else
		{
			m_Height = short.MaxValue;
		}
		if (reader.context.version >= Version.cornerBuildings)
		{
			reader.Read(out byte value);
			m_Flags = (LotFlags)value;
		}
	}
}
