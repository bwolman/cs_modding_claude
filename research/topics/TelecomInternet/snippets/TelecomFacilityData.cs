// Decompiled from Game.dll — Game.Prefabs.TelecomFacilityData

public struct TelecomFacilityData : IComponentData, ICombineData<TelecomFacilityData>
{
    public float m_Range;              // Coverage radius in meters (default: 1000)
    public float m_NetworkCapacity;    // Bandwidth capacity (default: 10000)
    public bool m_PenetrateTerrain;    // If true, ignores terrain obstruction

    // Combine() for upgrades: range += other.range, capacity += other.capacity
    // m_PenetrateTerrain |= other.m_PenetrateTerrain
}

// TelecomFacility prefab (Game.Prefabs.TelecomFacility):
//   Default m_Range = 1000f
//   Default m_NetworkCapacity = 10000f
//   Default m_PenetrateTerrain = false
//   Sets UpdateFrameData(13)

// TelecomFacilityMode (Game.Prefabs.Modes):
//   Applies m_RangeMultiplier and m_NetworkCapacityMultiplier
//   Used for game mode variations (e.g., difficulty settings)
