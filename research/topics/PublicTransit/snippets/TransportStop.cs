using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct TransportStop : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_AccessRestriction;
    public float m_ComfortFactor;
    public float m_LoadingFactor;
    public StopFlags m_Flags;
}

[Flags]
public enum StopFlags
{
    Active = 1,
    AllowEnter = 2
}
