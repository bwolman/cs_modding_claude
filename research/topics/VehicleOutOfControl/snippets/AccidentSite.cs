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
		Entity value = m_Event;
		writer.Write(value);
		Entity policeRequest = m_PoliceRequest;
		writer.Write(policeRequest);
		AccidentSiteFlags flags = m_Flags;
		writer.Write((uint)flags);
		uint creationFrame = m_CreationFrame;
		writer.Write(creationFrame);
		uint securedFrame = m_SecuredFrame;
		writer.Write(securedFrame);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref Entity policeRequest = ref m_PoliceRequest;
		reader.Read(out policeRequest);
		reader.Read(out uint value2);
		ref uint creationFrame = ref m_CreationFrame;
		reader.Read(out creationFrame);
		if (reader.context.version >= Version.policeImprovement)
		{
			ref uint securedFrame = ref m_SecuredFrame;
			reader.Read(out securedFrame);
		}
		m_Flags = (AccidentSiteFlags)value2;
	}
}
