// Decompiled from Game.dll — Game.Events.FacingWeather
// Attached to buildings that are within the hotspot of a weather phenomenon.
// Used by WeatherDamageSystem to apply incremental damage.

using Unity.Entities;

namespace Game.Events;

public struct FacingWeather : IComponentData, IQueryTypeParameter, ISerializable
{
    public Entity m_Event;     // The weather phenomenon event entity
    public float m_Severity;   // Current damage severity (distance-based)
}
