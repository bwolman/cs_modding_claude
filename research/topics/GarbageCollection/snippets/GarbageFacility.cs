// Decompiled from Game.dll — Game.Buildings.GarbageFacility

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct GarbageFacility : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_GarbageDeliverRequest;
    public Entity m_GarbageReceiveRequest;
    public Entity m_TargetRequest;
    public GarbageFacilityFlags m_Flags;
    public float m_AcceptGarbagePriority;
    public float m_DeliverGarbagePriority;
    public int m_ProcessingRate;

    // Serialization omitted for brevity
}

[Flags]
public enum GarbageFacilityFlags : byte
{
    HasAvailableGarbageTrucks = 1,
    HasAvailableSpace = 2,
    IndustrialWasteOnly = 4,
    IsFull = 8,
    HasAvailableDeliveryTrucks = 0x10
}
