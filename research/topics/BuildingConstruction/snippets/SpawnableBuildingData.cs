using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct SpawnableBuildingData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ZonePrefab;

	public byte m_Level;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Entity zonePrefab = m_ZonePrefab;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(zonePrefab);
		byte level = m_Level;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(level);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity zonePrefab = ref m_ZonePrefab;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref zonePrefab);
		ref byte level = ref m_Level;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref level);
	}
}
