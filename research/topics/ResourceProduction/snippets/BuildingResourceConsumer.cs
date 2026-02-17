using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ResourceConsumer : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_ResourceAvailability;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		((IWriter)writer/*cast due to .constrained prefix*/).Write(m_ResourceAvailability);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref m_ResourceAvailability);
	}
}
