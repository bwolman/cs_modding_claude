// Decompiled from Game.dll -> Game.Buildings.RescueTarget
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct RescueTarget : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Request;

    public RescueTarget(Entity request)
    {
        m_Request = request;
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Request);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Request);
    }
}
