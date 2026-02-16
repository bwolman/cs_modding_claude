// Decompiled from Game.dll -> Game.Simulation.ServiceDispatch
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

[InternalBufferCapacity(0)]
public struct ServiceDispatch : IBufferElementData, ISerializable
{
    public Entity m_Request;

    public ServiceDispatch(Entity request)
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
