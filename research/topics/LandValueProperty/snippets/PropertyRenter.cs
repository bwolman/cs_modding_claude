using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct PropertyRenter : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Property;

	public int m_Rent;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity property = m_Property;
		writer.Write(property);
		int rent = m_Rent;
		writer.Write(rent);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity property = ref m_Property;
		reader.Read(out property);
		ref int rent = ref m_Rent;
		reader.Read(out rent);
		if (reader.context.version < Version.economyFix)
		{
			reader.Read(out int _);
		}
	}
}
