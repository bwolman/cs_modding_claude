using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct TouristHousehold : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Hotel;

	public uint m_LeavingTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity hotel = m_Hotel;
		writer.Write(hotel);
		uint leavingTime = m_LeavingTime;
		writer.Write(leavingTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity hotel = ref m_Hotel;
		reader.Read(out hotel);
		ref uint leavingTime = ref m_LeavingTime;
		reader.Read(out leavingTime);
	}
}
