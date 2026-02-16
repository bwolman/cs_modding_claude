// Decompiled from Game.dll -> Game.Events.AccidentSite
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct AccidentSite : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Event;
    public Entity m_PoliceRequest;
    public AccidentSiteFlags m_Flags;
    public uint m_CreationFrame;
    public uint m_SecuredFrame;

    public AccidentSite(Entity _event, AccidentSiteFlags flags, uint currentFrame)
    {
        m_Event = _event;
        m_PoliceRequest = Entity.Null;
        m_Flags = flags;
        m_CreationFrame = currentFrame;
        m_SecuredFrame = 0u;
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Event);
        writer.Write(m_PoliceRequest);
        writer.Write((uint)m_Flags);
        writer.Write(m_CreationFrame);
        writer.Write(m_SecuredFrame);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Event);
        reader.Read(out m_PoliceRequest);
        reader.Read(out uint value2);
        reader.Read(out m_CreationFrame);
        if (reader.context.version >= Version.policeImprovement)
        {
            reader.Read(out m_SecuredFrame);
        }
        m_Flags = (AccidentSiteFlags)value2;
    }
}
