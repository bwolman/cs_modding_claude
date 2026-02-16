// Decompiled from Game.dll — Game.Prefabs.GarbageFacilityData

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct GarbageFacilityData : IComponentData, IQueryTypeParameter, ICombineData<GarbageFacilityData>, ISerializable
{
    public int m_GarbageCapacity;
    public int m_VehicleCapacity;
    public int m_TransportCapacity;
    public int m_ProcessingSpeed;
    public bool m_IndustrialWasteOnly;
    public bool m_LongTermStorage;

    public void Combine(GarbageFacilityData otherData)
    {
        m_GarbageCapacity += otherData.m_GarbageCapacity;
        m_VehicleCapacity += otherData.m_VehicleCapacity;
        m_TransportCapacity += otherData.m_TransportCapacity;
        m_ProcessingSpeed += otherData.m_ProcessingSpeed;
        m_IndustrialWasteOnly |= otherData.m_IndustrialWasteOnly;
        m_LongTermStorage |= otherData.m_LongTermStorage;
    }

    // Serialization omitted for brevity
}
