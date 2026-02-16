// Decompiled from Game.dll -> Game.Citizens.HealthProblem
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
        writer.Write(m_Event);
        writer.Write(m_HealthcareRequest);
        writer.Write((byte)m_Flags);
        writer.Write(m_Timer);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Event);
        reader.Read(out m_HealthcareRequest);
        reader.Read(out byte value2);
        if (reader.context.version >= Version.healthcareNotifications)
        {
            reader.Read(out m_Timer);
        }
        m_Flags = (HealthProblemFlags)value2;
    }
}
