// Decompiled from Game.dll — Game.Buildings.GarbageProducer

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct GarbageProducer : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_CollectionRequest;
    public int m_Garbage;
    public GarbageProducerFlags m_Flags;
    public byte m_DispatchIndex;

    // Serialization omitted for brevity
}

[Flags]
public enum GarbageProducerFlags : byte
{
    None = 0,
    GarbagePilingUpWarning = 1
}
