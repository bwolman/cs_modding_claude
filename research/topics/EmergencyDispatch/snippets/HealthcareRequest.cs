// Decompiled from Game.dll -> Game.Simulation.HealthcareRequest
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct HealthcareRequest : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Citizen;
    public HealthcareRequestType m_Type;

    public HealthcareRequest(Entity citizen, HealthcareRequestType type)
    {
        m_Citizen = citizen;
        m_Type = type;
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Citizen);
        writer.Write((byte)m_Type);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Citizen);
        reader.Read(out byte value);
        m_Type = (HealthcareRequestType)value;
    }
}
