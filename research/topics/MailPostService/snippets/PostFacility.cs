using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct PostFacility : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_MailDeliverRequest;

	public Entity m_MailReceiveRequest;

	public Entity m_TargetRequest;

	public float m_AcceptMailPriority;

	public float m_DeliverMailPriority;

	public PostFacilityFlags m_Flags;

	public byte m_ProcessingFactor;
}

[Flags]
public enum PostFacilityFlags : byte
{
	CanDeliverMailWithVan = 1,
	CanCollectMailWithVan = 2,
	HasAvailableTrucks = 4,
	AcceptsUnsortedMail = 8,
	DeliversLocalMail = 0x10,
	AcceptsLocalMail = 0x20,
	DeliversUnsortedMail = 0x40
}
