// Decompiled from Game.dll -> Game.Simulation.ServiceRequest
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ServiceRequest : IComponentData, IQueryTypeParameter, ISerializable
{
    public byte m_FailCount;
    public byte m_Cooldown;
    public ServiceRequestFlags m_Flags;

    public ServiceRequest(bool reversed)
    {
        m_FailCount = 0;
        m_Cooldown = 0;
        m_Flags = (reversed ? ServiceRequestFlags.Reversed : ((ServiceRequestFlags)0));
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_FailCount);
        writer.Write(m_Cooldown);
        writer.Write((byte)m_Flags);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_FailCount);
        reader.Read(out m_Cooldown);
        if (reader.context.version >= Version.reverseServiceRequests)
        {
            reader.Read(out byte value);
            m_Flags = (ServiceRequestFlags)value;
        }
    }
}
