using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PlaceholderBuildingData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ZonePrefab;

	public BuildingType m_Type;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Entity zonePrefab = m_ZonePrefab;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(zonePrefab);
		BuildingType type = m_Type;
		((IWriter)writer/*cast due to .constrained prefix*/).Write((int)type);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity zonePrefab = ref m_ZonePrefab;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref zonePrefab);
		int type = default(int);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref type);
		m_Type = (BuildingType)type;
	}
}
