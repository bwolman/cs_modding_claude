// Decompiled from Game.dll — Game.Citizens.Household
// The core component for every household entity.

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct Household : IComponentData, IQueryTypeParameter, ISerializable
{
    public HouseholdFlags m_Flags;
    public int m_Resources;
    public short m_ConsumptionPerDay;
    public uint m_ShoppedValuePerDay;
    public uint m_ShoppedValueLastDay;
    public uint m_LastDayFrameIndex;
    public int m_SalaryLastDay;
    public int m_MoneySpendOnBuildingLevelingLastDay;
}

[Flags]
public enum HouseholdFlags : byte
{
    None = 0,
    Tourist = 1,
    Commuter = 2,
    MovedIn = 4
}
