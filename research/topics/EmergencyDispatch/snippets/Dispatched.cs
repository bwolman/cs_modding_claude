// Decompiled from Game.dll -> Game.Simulation.Dispatched
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct Dispatched : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Handler;

    public Dispatched(Entity handler)
    {
        m_Handler = handler;
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Handler);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Handler);
    }
}
