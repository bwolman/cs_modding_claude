using System;
using Colossal.Serialization.Entities;

namespace Game.Zones;

public struct ZoneType : IEquatable<ZoneType>, IStrideSerializable, ISerializable
{
	public ushort m_Index;

	public static ZoneType None => default(ZoneType);

	public bool Equals(ZoneType other)
	{
		return m_Index.Equals(other.m_Index);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Index);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.cornerBuildings)
		{
			ref ushort index = ref m_Index;
			reader.Read(out index);
		}
		else
		{
			reader.Read(out byte value);
			m_Index = value;
		}
	}

	public int GetStride(Context context)
	{
		return 2;
	}
}
