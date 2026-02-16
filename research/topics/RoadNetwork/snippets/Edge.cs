// Source: Game.dll -> Game.Net.Edge (decompiled with ilspycmd v9.1)
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct Edge : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Start;

    public Entity m_End;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        Entity start = m_Start;
        writer.Write(start);
        Entity end = m_End;
        writer.Write(end);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        ref Entity start = ref m_Start;
        reader.Read(out start);
        ref Entity end = ref m_End;
        reader.Read(out end);
    }
}
