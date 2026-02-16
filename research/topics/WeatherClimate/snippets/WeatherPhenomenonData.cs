// Decompiled from Game.dll — Game.Prefabs.WeatherPhenomenonData
// Prefab configuration for a weather phenomenon type (tornado, thunderstorm, etc.)
// Defines occurrence probability, damage severity, radii, and conditions.

using Colossal.Mathematics;
using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

public struct WeatherPhenomenonData : IComponentData, IQueryTypeParameter
{
    public float m_OccurenceProbability;       // Base probability per tick
    public float m_HotspotInstability;         // How much the hotspot wanders
    public float m_DamageSeverity;             // Damage multiplier (0 = no damage, e.g. fog)
    public float m_DangerLevel;                // Danger level for warnings
    public Bounds1 m_PhenomenonRadius;         // Min/max outer radius
    public Bounds1 m_HotspotRadius;            // Min/max hotspot radius
    public Bounds1 m_LightningInterval;        // Min/max seconds between lightning strikes
    public Bounds1 m_Duration;                 // Min/max duration in seconds
    public Bounds1 m_OccurenceTemperature;     // Temperature range for occurrence
    public Bounds1 m_OccurenceRain;            // Rain range for occurrence
    public Bounds1 m_OccurenceCloudiness;      // Cloudiness range for occurrence
    public DangerFlags m_DangerFlags;          // Evacuate, StayIndoors, etc.
}
