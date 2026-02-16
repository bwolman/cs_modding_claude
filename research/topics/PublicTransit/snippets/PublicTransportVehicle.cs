using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct PublicTransport : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_TargetRequest;
    public PublicTransportFlags m_State;
    public uint m_DepartureFrame;
    public int m_RequestCount;
    public float m_PathElementTime;
    public float m_MaxBoardingDistance;
    public float m_MinWaitingDistance;
}

[Flags]
public enum PublicTransportFlags : uint
{
    Returning = 1u,
    EnRoute = 2u,
    Boarding = 4u,
    Arriving = 8u,
    Launched = 0x10u,
    Evacuating = 0x20u,
    PrisonerTransport = 0x40u,
    RequiresMaintenance = 0x80u,
    Refueling = 0x100u,
    AbandonRoute = 0x200u,
    RouteSource = 0x400u,
    Testing = 0x800u,
    RequireStop = 0x1000u,
    DummyTraffic = 0x2000u,
    StopLeft = 0x4000u,
    StopRight = 0x8000u,
    Disabled = 0x10000u,
    Full = 0x20000u
}
