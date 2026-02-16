// Decompiled from Game.dll — Game.Simulation.WeatherPhenomenonSystem
// Simulates active weather phenomena: moves hotspot with wind, applies damage, triggers lightning.
// Runs every 16 frames.

namespace Game.Simulation;

public class WeatherPhenomenonSystem : GameSystemBase
{
    public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

    // WeatherPhenomenonJob.Execute: per active phenomenon entity:
    //   1. Intensity ramps: +0.2/sec after start, -0.2/sec after end
    //   2. Sample wind at phenomenon position, move it: position.xz += wind * 20 * dt
    //   3. Move hotspot within phenomenon radius using wind + instability
    //   4. If m_DamageSeverity != 0: find affected buildings via quad tree, create FaceWeather events
    //   5. If m_LightningTimer > 0: decrement, on zero:
    //      - Find tallest building/tree within hotspot radius (LightningTargetIterator)
    //      - Enqueue LightningStrike for rendering
    //      - If FireData.m_StartProbability > 0.01: roll ignition (Ignite event)
    //   6. If TrafficAccidentData on prefab: find cars in hotspot, cause LoseControl impacts
    //   7. If m_DangerFlags != 0: find endangered buildings for evacuation warnings
    //   8. After end frame + fade: mark entity Deleted

    // LightningTargetIterator: searches static objects quad tree
    //   - Prefers tallest objects within hotspot radius
    //   - Score = distance * 0.5 - height (lower = better target)
    //   - Must be a tree (BoundsMask.IsTree) or building

    // AffectedNetIterator: searches road network for moving cars
    //   - probability = sqrt(TrafficAccidentData.m_OccurenceProbability * 0.01)
    //   - Creates Impact events that cause vehicles to lose control
}
