using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Zones;

[InternalBufferCapacity(60)]
public struct Cell : IBufferElementData, IStrideSerializable, ISerializable
{
	public CellFlags m_State;

	public ZoneType m_Zone;

	public short m_Height;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		CellFlags state = m_State;
		writer.Write((ushort)state);
		ZoneType zone = m_Zone;
		writer.Write(zone);
		short height = m_Height;
		writer.Write(height);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.cornerBuildings)
		{
			reader.Read(out ushort value);
			m_State = (CellFlags)value;
		}
		else
		{
			reader.Read(out byte value2);
			m_State = (CellFlags)value2;
		}
		ref ZoneType zone = ref m_Zone;
		reader.Read(out zone);
		if (reader.context.version >= Version.zoneHeightLimit)
		{
			ref short height = ref m_Height;
			reader.Read(out height);
		}
		else
		{
			m_Height = short.MaxValue;
		}
	}

	public int GetStride(Context context)
	{
		if (context.version >= Version.zoneHeightLimit)
		{
			return 4 + m_Zone.GetStride(context);
		}
		return 2 + m_Zone.GetStride(context);
	}
}
