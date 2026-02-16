// Decompiled from Game.dll — Game.Simulation.GarbageCollectionRequest

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct GarbageCollectionRequest : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Target;
    public int m_Priority;
    public GarbageCollectionRequestFlags m_Flags;
    public byte m_DispatchIndex;

    public GarbageCollectionRequest(Entity target, int priority, GarbageCollectionRequestFlags flags)
    {
        m_Target = target;
        m_Priority = priority;
        m_Flags = flags;
        m_DispatchIndex = 0;
    }

    // Serialization omitted for brevity
}

[Flags]
public enum GarbageCollectionRequestFlags : byte
{
    IndustrialWaste = 1
}
