// Decompiled from Game.dll -> Game.Simulation.PoliceEmergencyRequest
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Simulation;

public struct PoliceEmergencyRequest : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Site;
    public Entity m_Target;
    public float m_Priority;
    public PolicePurpose m_Purpose;

    public PoliceEmergencyRequest(Entity site, Entity target, float priority, PolicePurpose purpose)
    {
        m_Site = site;
        m_Target = target;
        m_Priority = priority;
        m_Purpose = purpose;
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Site);
        writer.Write(m_Target);
        writer.Write(m_Priority);
        writer.Write((int)m_Purpose);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Site);
        reader.Read(out m_Target);
        reader.Read(out m_Priority);
        if (reader.context.version >= Version.policeImprovement3)
        {
            reader.Read(out int value);
            m_Purpose = (PolicePurpose)value;
        }
        else
        {
            m_Purpose = PolicePurpose.Patrol | PolicePurpose.Emergency;
        }
    }
}
