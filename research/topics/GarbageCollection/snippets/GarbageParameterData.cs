// Decompiled from Game.dll — Game.Prefabs.GarbageParameterData

using Unity.Entities;

namespace Game.Prefabs;

public struct GarbageParameterData : IComponentData, IQueryTypeParameter
{
    public Entity m_GarbageServicePrefab;
    public Entity m_GarbageNotificationPrefab;
    public Entity m_FacilityFullNotificationPrefab;
    public int m_HomelessGarbageProduce;
    public int m_CollectionGarbageLimit;
    public int m_RequestGarbageLimit;
    public int m_WarningGarbageLimit;
    public int m_MaxGarbageAccumulation;
    public float m_BuildingLevelBalance;
    public float m_EducationBalance;
    public int m_HappinessEffectBaseline;
    public int m_HappinessEffectStep;
}
