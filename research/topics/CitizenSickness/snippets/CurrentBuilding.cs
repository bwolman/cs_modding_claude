using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct CurrentBuilding : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_CurrentBuilding;

	public CurrentBuilding(Entity building)
	{
		m_CurrentBuilding = building;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_CurrentBuilding);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_CurrentBuilding);
	}
}
