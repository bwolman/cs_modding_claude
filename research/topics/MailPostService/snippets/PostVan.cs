using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct PostVan : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public PostVanFlags m_State;

	public int m_RequestCount;

	public float m_PathElementTime;

	public int m_DeliveringMail;

	public int m_CollectedMail;

	public int m_DeliveryEstimate;

	public int m_CollectEstimate;
}

[Flags]
public enum PostVanFlags : uint
{
	Returning = 1u,
	Delivering = 2u,
	Collecting = 4u,
	DeliveryEmpty = 8u,
	CollectFull = 0x10u,
	EstimatedEmpty = 0x20u,
	EstimatedFull = 0x40u,
	Disabled = 0x80u,
	ClearChecked = 0x100u
}
