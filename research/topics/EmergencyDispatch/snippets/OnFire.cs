// Decompiled from Game.dll -> Game.Events.OnFire
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct OnFire : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Event;
    public Entity m_RescueRequest;
    public float m_Intensity;
    public uint m_RequestFrame;

    public OnFire(Entity _event, float intensity, uint requestFrame = 0u)
    {
        m_Event = _event;
        m_RescueRequest = Entity.Null;
        m_Intensity = intensity;
        m_RequestFrame = requestFrame;
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Event);
        writer.Write(m_RescueRequest);
        writer.Write(m_Intensity);
        writer.Write(m_RequestFrame);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Event);
        reader.Read(out m_RescueRequest);
        reader.Read(out m_Intensity);
        reader.Read(out m_RequestFrame);
    }
}
