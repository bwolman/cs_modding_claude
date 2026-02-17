using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct ResourceAvailability : IBufferElementData, ISerializable
{
	public float2 m_Availability;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		((IWriter)writer/*cast due to .constrained prefix*/).Write(m_Availability);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref m_Availability);
	}
}
