// Decompiled from Game.dll — Game.Citizens.HouseholdMember, HouseholdCitizen, Worker
// Link components connecting citizens to households and workplaces.

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

// Attached to a citizen entity — points to the household it belongs to
public struct HouseholdMember : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Household;
}

// Buffer element on the household entity — lists all citizens in the household
[InternalBufferCapacity(5)]
public struct HouseholdCitizen : IBufferElementData, IEquatable<HouseholdCitizen>, IEmptySerializable
{
    public Entity m_Citizen;

    public HouseholdCitizen(Entity citizen)
    {
        m_Citizen = citizen;
    }
}

// Attached to a citizen who has a job
public struct Worker : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Workplace;
    public float m_LastCommuteTime;
    public byte m_Level;
    public Workshift m_Shift;
}
