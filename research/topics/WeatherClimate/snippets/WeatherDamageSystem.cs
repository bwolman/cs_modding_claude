// Decompiled from Game.dll — Game.Simulation.WeatherDamageSystem
// Applies incremental damage to buildings facing active weather phenomena.
// Runs every 64 frames.

namespace Game.Simulation;

public class WeatherDamageSystem : GameSystemBase
{
    public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

    // WeatherDamageJob: for each entity with FacingWeather component:
    //   1. Recalculate severity from EventUtils.GetSeverity(position, phenomenon, data)
    //   2. Get structural integrity from prefab (higher = more resistant)
    //   3. Compute damage rate = severity / integrity * timeDelta
    //      - Capped at 0.5 per tick
    //      - Buildings apply CityModifierType.DisasterDamageRate modifier
    //      - Entities with integrity >= 100M are immune
    //   4. Add damage to Damaged.m_Damage.x component
    //   5. If total damage reaches 1.0: create Destroy event, add notification icon
    //   6. If severity drops to 0: remove FacingWeather component
    //
    // Notification icons:
    //   - WeatherDamageNotificationPrefab at Problem/MajorProblem priority (severity >= 30)
    //   - WeatherDestroyedNotificationPrefab at FatalProblem when destroyed
}
