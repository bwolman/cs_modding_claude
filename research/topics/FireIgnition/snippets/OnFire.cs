using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct OnFire : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public Entity m_RescueRequest;

	public float m_Intensity;

	public uint m_RequestFrame;

	public OnFire(Entity _event, float intensity, uint requestFrame = 0u)
	{
		m_Event = _event;
		m_RescueRequest = Entity.Null;
		m_Intensity = intensity;
		m_RequestFrame = requestFrame;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		Entity rescueRequest = m_RescueRequest;
		writer.Write(rescueRequest);
		float intensity = m_Intensity;
		writer.Write(intensity);
		uint requestFrame = m_RequestFrame;
		writer.Write(requestFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref Entity rescueRequest = ref m_RescueRequest;
		reader.Read(out rescueRequest);
		ref float intensity = ref m_Intensity;
		reader.Read(out intensity);
		ref uint requestFrame = ref m_RequestFrame;
		reader.Read(out requestFrame);
	}
}
