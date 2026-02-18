// Full source: Game.dll -> Game.Prefabs.EventData
// Stored on event prefab entities. Contains the archetype used to create event instances.

using Unity.Entities;

namespace Game.Prefabs;

public struct EventData : IComponentData, IQueryTypeParameter
{
    // The archetype for event instance entities.
    // For crime events, this archetype includes:
    //   - Game.Events.Crime (tag)
    //   - TargetElement (buffer)
    // Defined by Crime.GetArchetypeComponents()
    public EntityArchetype m_Archetype;

    // Maximum concurrent instances of this event type (0 = unlimited)
    public int m_ConcurrentLimit;
}
