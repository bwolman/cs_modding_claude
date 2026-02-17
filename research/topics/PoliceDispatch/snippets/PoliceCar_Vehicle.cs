// Decompiled from Game.dll — Game.Vehicles.PoliceCar
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Vehicles;

public struct PoliceCar : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_TargetRequest;
    public PoliceCarFlags m_State;
    public int m_RequestCount;
    public float m_PathElementTime;
    public uint m_ShiftTime;
    public uint m_EstimatedShift;
    public PolicePurpose m_PurposeMask;

    public PoliceCar(PoliceCarFlags flags, int requestCount, PolicePurpose purposeMask)
    {
        m_TargetRequest = Entity.Null;
        m_State = flags;
        m_RequestCount = requestCount;
        m_PathElementTime = 0f;
        m_ShiftTime = 0u;
        m_EstimatedShift = 0u;
        m_PurposeMask = purposeMask;
    }
}
