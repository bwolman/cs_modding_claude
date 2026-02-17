// Decompiled from Game.dll — Game.Prefabs.PoliceCarData
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PoliceCarData : IComponentData, IQueryTypeParameter, ISerializable
{
    public int m_CriminalCapacity;
    public float m_CrimeReductionRate;
    public uint m_ShiftDuration;
    public PolicePurpose m_PurposeMask;

    public PoliceCarData(int criminalCapacity, float crimeReductionRate, uint shiftDuration, PolicePurpose purposeMask)
    {
        m_CriminalCapacity = criminalCapacity;
        m_CrimeReductionRate = crimeReductionRate;
        m_ShiftDuration = shiftDuration;
        m_PurposeMask = purposeMask;
    }
}
