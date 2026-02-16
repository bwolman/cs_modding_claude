using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(0)]
public struct RouteWaypoint : IBufferElementData, IEmptySerializable
{
    public Entity m_Waypoint;

    public RouteWaypoint(Entity waypoint)
    {
        m_Waypoint = waypoint;
    }
}

public struct Waypoint : IComponentData, IQueryTypeParameter, ISerializable
{
    public int m_Index;
}

public struct Segment : IComponentData, IQueryTypeParameter, ISerializable
{
    public int m_Index;
}

[InternalBufferCapacity(0)]
public struct RouteSegment : IBufferElementData, IEmptySerializable
{
    public Entity m_Segment;
}

[InternalBufferCapacity(0)]
public struct RouteVehicle : IBufferElementData, IEquatable<RouteVehicle>, IEmptySerializable
{
    public Entity m_Vehicle;
}

public struct Connected : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Connected;
}

public struct CurrentRoute : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Route;
}

public struct RouteInfo : IComponentData, IQueryTypeParameter, ISerializable
{
    public float m_Duration;
    public float m_Distance;
    public RouteInfoFlags m_Flags;
}

public struct VehicleTiming : IComponentData, IQueryTypeParameter, ISerializable
{
    public uint m_LastDepartureFrame;
    public float m_AverageTravelTime;
}

public struct BoardingVehicle : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Vehicle;
    public Entity m_Testing;
}

public enum RouteModifierType
{
    TicketPrice,
    VehicleInterval
}
