using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct InvolvedInAccident : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public float m_Severity;

	public uint m_InvolvedFrame;

	public InvolvedInAccident(Entity _event, float severity, uint simulationFrame)
	{
		m_Event = _event;
		m_Severity = severity;
		m_InvolvedFrame = simulationFrame;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		float severity = m_Severity;
		writer.Write(severity);
		uint involvedFrame = m_InvolvedFrame;
		writer.Write(involvedFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref float severity = ref m_Severity;
		reader.Read(out severity);
		if (reader.context.version >= Version.accidentInvolvedFrame)
		{
			ref uint involvedFrame = ref m_InvolvedFrame;
			reader.Read(out involvedFrame);
		}
	}
}
