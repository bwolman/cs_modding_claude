using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct MailBox : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_CollectRequest;

	public int m_MailAmount;
}
