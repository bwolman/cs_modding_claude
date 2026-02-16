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
}
