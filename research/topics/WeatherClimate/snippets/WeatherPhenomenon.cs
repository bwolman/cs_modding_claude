// Decompiled from Game.dll — Game.Events.WeatherPhenomenon (ECS component)
// Represents an active weather phenomenon event (tornado, thunderstorm, etc.)
// Tracks position, hotspot, intensity, and lightning timing.

using Unity.Entities;
using Unity.Mathematics;

namespace Game.Events;

public struct WeatherPhenomenon : IComponentData, IQueryTypeParameter, ISerializable
{
    public float3 m_PhenomenonPosition;   // Center of the weather cell
    public float3 m_HotspotPosition;      // Center of the damage hotspot within the cell
    public float3 m_HotspotVelocity;      // Movement velocity of the hotspot
    public float m_PhenomenonRadius;       // Outer radius of the weather cell
    public float m_HotspotRadius;          // Inner radius of the damage hotspot
    public float m_Intensity;              // 0..1, ramps up/down at 0.2/sec
    public float m_LightningTimer;         // Countdown to next lightning strike
}
