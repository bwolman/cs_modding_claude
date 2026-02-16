using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Routes;

public struct TransportLine : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_VehicleRequest;
    public float m_VehicleInterval;
    public float m_UnbunchingFactor;
    public TransportLineFlags m_Flags;
    public ushort m_TicketPrice;

    public TransportLine(TransportLineData transportLineData)
    {
        m_VehicleRequest = Entity.Null;
        m_VehicleInterval = transportLineData.m_DefaultVehicleInterval;
        m_UnbunchingFactor = transportLineData.m_DefaultUnbunchingFactor;
        m_Flags = (TransportLineFlags)0;
        m_TicketPrice = 0;
    }
}

[Flags]
public enum TransportLineFlags : ushort
{
    RequireVehicles = 1,
    NotEnoughVehicles = 2
}
