// Decompiled from Game.dll — Game.Buildings.PoliceStation
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Buildings;

public struct PoliceStation : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_PrisonerTransportRequest;
    public Entity m_TargetRequest;
    public PoliceStationFlags m_Flags;
    public PolicePurpose m_PurposeMask;
}
