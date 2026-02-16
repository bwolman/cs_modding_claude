// Extracted from Game.dll → Game.Prefabs.TrafficAccident (ComponentBase)
// This is the PREFAB component that adds archetype components for TrafficAccident events.
// NOT to be confused with Game.Events.TrafficAccident (the ECS marker component).

namespace Game.Prefabs;

public class TrafficAccident : ComponentBase
{
    // --- GetArchetypeComponents ---
    // Adds two things to the event entity archetype:
    //   1. Game.Events.TrafficAccident — empty marker component for type identification
    //   2. TargetElement — DynamicBuffer that tracks involved entities (vehicles, creatures, sites)
    public override void GetArchetypeComponents(HashSet<ComponentType> components)
    {
        components.Add(ComponentType.ReadWrite<Game.Events.TrafficAccident>());
        components.Add(ComponentType.ReadWrite<TargetElement>());
    }
}

// --- Game.Events.TrafficAccident (ECS marker) ---
// Empty struct, just a type tag on the event entity
namespace Game.Events;

public struct TrafficAccident : IComponentData
{
    // empty — StructLayout(LayoutKind.Sequential, Size = 1)
}
