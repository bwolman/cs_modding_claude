// Decompiled from Game.dll -> Game.Simulation.FireRescueRequest
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct FireRescueRequest : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Target;
    public float m_Priority;
    public FireRescueRequestType m_Type;

    public FireRescueRequest(Entity target, float priority, FireRescueRequestType type)
    {
        m_Target = target;
        m_Priority = priority;
        m_Type = type;
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Target);
        writer.Write(m_Priority);
        writer.Write((byte)m_Type);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Target);
        reader.Read(out m_Priority);
        if (reader.context.version >= Version.disasterResponse)
        {
            reader.Read(out byte value);
            m_Type = (FireRescueRequestType)value;
        }
    }
}
