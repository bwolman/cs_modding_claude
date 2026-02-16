using Colossal.Serialization.Entities;
using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

public struct ZoneData : IComponentData, IQueryTypeParameter, ISerializable
{
	public ZoneType m_ZoneType;

	public AreaType m_AreaType;

	public ZoneFlags m_ZoneFlags;

	public ushort m_MinOddHeight;

	public ushort m_MinEvenHeight;

	public ushort m_MaxHeight;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ZoneType zoneType = m_ZoneType;
		writer.Write(zoneType);
		AreaType areaType = m_AreaType;
		writer.Write((byte)areaType);
		ZoneFlags zoneFlags = m_ZoneFlags;
		writer.Write((byte)zoneFlags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref ZoneType zoneType = ref m_ZoneType;
		reader.Read(out zoneType);
		reader.Read(out byte value);
		reader.Read(out byte value2);
		m_AreaType = (AreaType)value;
		m_ZoneFlags = (ZoneFlags)value2;
	}

	public bool IsOffice()
	{
		return (m_ZoneFlags & ZoneFlags.Office) != 0;
	}
}
