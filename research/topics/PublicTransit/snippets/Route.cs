using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct Route : IComponentData, IQueryTypeParameter, ISerializable
{
    public RouteFlags m_Flags;
    public uint m_OptionMask;
}

[Flags]
public enum RouteFlags
{
    Complete = 1
}

public enum RouteOption
{
    Day,
    Night,
    Inactive,
    PaidTicket
}

public enum RouteType
{
    None = -1,
    TransportLine,
    WorkRoute,
    Count
}
