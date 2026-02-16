using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct HealthProblem : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public Entity m_HealthcareRequest;

	public HealthProblemFlags m_Flags;

	public byte m_Timer;

	public HealthProblem(Entity _event, HealthProblemFlags flags)
	{
		m_Event = _event;
		m_HealthcareRequest = Entity.Null;
		m_Flags = flags;
		m_Timer = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		Entity healthcareRequest = m_HealthcareRequest;
		writer.Write(healthcareRequest);
		HealthProblemFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte timer = m_Timer;
		writer.Write(timer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref Entity healthcareRequest = ref m_HealthcareRequest;
		reader.Read(out healthcareRequest);
		reader.Read(out byte value2);
		if (reader.context.version >= Version.healthcareNotifications)
		{
			ref byte timer = ref m_Timer;
			reader.Read(out timer);
		}
		m_Flags = (HealthProblemFlags)value2;
	}
}
