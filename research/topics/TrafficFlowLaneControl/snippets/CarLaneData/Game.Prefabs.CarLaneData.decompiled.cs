using Colossal.Serialization.Entities;
using Game.Net;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct CarLaneData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_NotTrackLanePrefab;

	public Entity m_NotBusLanePrefab;

	public RoadTypes m_RoadTypes;

	public SizeClass m_MaxSize;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		RoadTypes roadTypes = m_RoadTypes;
		writer.Write((byte)roadTypes);
		SizeClass maxSize = m_MaxSize;
		writer.Write((byte)maxSize);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out byte value2);
		m_RoadTypes = (RoadTypes)value;
		m_MaxSize = (SizeClass)value2;
	}
}
