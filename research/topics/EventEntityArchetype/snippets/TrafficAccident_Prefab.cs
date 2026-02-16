// Extracted from Game.dll → Game.Prefabs.TrafficAccident
// ComponentBase that adds the TrafficAccident marker and TargetElement buffer to the archetype

namespace Game.Prefabs;

public class TrafficAccident : ComponentBase
{
    // --- GetArchetypeComponents ---
    // Adds Game.Events.TrafficAccident (empty marker) and TargetElement (DynamicBuffer)
    public override void GetArchetypeComponents(HashSet<ComponentType> components)
    {
        components.Add(ComponentType.ReadWrite<Game.Events.TrafficAccident>());
        components.Add(ComponentType.ReadWrite<TargetElement>());
    }
}

// --- The two different Event types ---

namespace Game.Common;
// Short-lived command marker. Used on Impact, AddAccidentSite, Ignite, Damage entities.
public struct Event : IComponentData { }

namespace Game.Events;
// Persistent event marker. Used on TrafficAccident event entities.
public struct Event : IComponentData { }

// --- The TrafficAccident marker ---
namespace Game.Events;
public struct TrafficAccident : IComponentData { }
