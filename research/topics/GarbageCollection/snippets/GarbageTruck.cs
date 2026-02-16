// Decompiled from Game.dll — Game.Vehicles.GarbageTruck

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct GarbageTruck : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_TargetRequest;
    public GarbageTruckFlags m_State;
    public int m_RequestCount;
    public int m_Garbage;
    public int m_EstimatedGarbage;
    public float m_PathElementTime;

    public GarbageTruck(GarbageTruckFlags flags, int requestCount)
    {
        m_TargetRequest = Entity.Null;
        m_State = flags;
        m_RequestCount = requestCount;
        m_Garbage = 0;
        m_EstimatedGarbage = 0;
        m_PathElementTime = 0f;
    }

    // Serialization omitted for brevity
}

[Flags]
public enum GarbageTruckFlags : uint
{
    Returning = 1u,
    IndustrialWasteOnly = 2u,
    Unloading = 4u,
    Disabled = 8u,
    EstimatedFull = 0x10u,
    ClearChecked = 0x20u
}
