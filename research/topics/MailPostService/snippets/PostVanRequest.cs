using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct PostVanRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public PostVanRequestFlags m_Flags;

	public byte m_DispatchIndex;

	public ushort m_Priority;
}

[Flags]
public enum PostVanRequestFlags : byte
{
	Deliver = 1,
	Collect = 2,
	BuildingTarget = 4,
	MailBoxTarget = 8
}
