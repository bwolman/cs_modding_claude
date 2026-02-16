// Decompiled from Game.dll -> Game.Events.InvolvedInAccident
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
        writer.Write(m_Event);
        writer.Write(m_Severity);
        writer.Write(m_InvolvedFrame);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Event);
        reader.Read(out m_Severity);
        if (reader.context.version >= Version.accidentInvolvedFrame)
        {
            reader.Read(out m_InvolvedFrame);
        }
    }
}
