using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct LodgingProvider : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_FreeRooms;

	public int m_Price;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int freeRooms = m_FreeRooms;
		writer.Write(freeRooms);
		int price = m_Price;
		writer.Write(price);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int freeRooms = ref m_FreeRooms;
		reader.Read(out freeRooms);
		ref int price = ref m_Price;
		reader.Read(out price);
	}
}
