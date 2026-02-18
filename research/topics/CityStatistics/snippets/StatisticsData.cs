// Decompiled from Game.dll - Game.Prefabs.StatisticsData
using Game.City;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct StatisticsData : IComponentData, IQueryTypeParameter
{
    public Entity m_Category;               // UIStatisticsCategoryPrefab entity (e.g., "Population" tab)
    public Entity m_Group;                   // UIStatisticsGroupPrefab entity (grouping within tab)
    public StatisticType m_StatisticType;    // Which StatisticType this tracks
    public StatisticCollectionType m_CollectionType; // Point, Cumulative, or Daily
    public StatisticUnitType m_UnitType;     // None, Money, Percent, Weight
    public Color m_Color;                    // Line/bar color in UI chart
    public bool m_Stacked;                   // Whether to stack in grouped charts
}
