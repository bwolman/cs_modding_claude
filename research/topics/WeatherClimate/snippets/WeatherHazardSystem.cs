// Decompiled from Game.dll — Game.Simulation.WeatherHazardSystem
// Periodically checks all weather phenomenon prefabs and rolls probability to spawn events.
// Runs every 2048 frames. Uses temperature, rain, and cloudiness to modulate occurrence.

namespace Game.Simulation;

public class WeatherHazardSystem : GameSystemBase
{
    private const int UPDATES_PER_DAY = 128;

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 2048;

    // WeatherHazardJob: for each WeatherPhenomenonData prefab, computes probability:
    //   tempFactor = max(0, 1 - ((temp - center) / halfExtent)^2)
    //   rainFactor = saturate based on m_OccurenceRain bounds
    //   cloudFactor = saturate based on m_OccurenceCloudiness bounds
    //   probability = m_OccurenceProbability * tempFactor * rainFactor * cloudFactor * timeDelta
    //   If random(100) < probability: create weather event entity
    //
    // Damaging phenomena (m_DamageSeverity != 0) only spawn if naturalDisasters is enabled.
    // timeDelta = 34.133335f (scaled to 128 updates per day)
    //
    // OnUpdate passes current temperature, precipitation, cloudiness from ClimateSystem.
}
