// Extracted from Game.dll → Game.Prefabs.EventPrefab
// Shows how the event entity archetype is assembled at prefab load time

namespace Game.Prefabs;

public class EventPrefab : PrefabBase
{
    public int m_ConcurrentLimit;

    // --- GetArchetypeComponents ---
    // Adds Game.Events.Event (persistent event marker) to the archetype
    public override void GetArchetypeComponents(HashSet<ComponentType> components)
    {
        base.GetArchetypeComponents(components);  // PrefabBase adds PrefabRef
        components.Add(ComponentType.ReadWrite<Game.Events.Event>());
    }

    // --- RefreshArchetype ---
    // Called during prefab initialization. Collects all ComponentBase children,
    // calls GetArchetypeComponents on each, then unconditionally adds Created + Updated.
    // Stores the resulting EntityArchetype in EventData.m_Archetype on the prefab entity.
    protected virtual void RefreshArchetype(EntityManager entityManager, Entity entity)
    {
        List<ComponentBase> list = new List<ComponentBase>();
        GetComponents(list);
        HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
        for (int i = 0; i < list.Count; i++)
        {
            list[i].GetArchetypeComponents(hashSet);
        }
        hashSet.Add(ComponentType.ReadWrite<Created>());
        hashSet.Add(ComponentType.ReadWrite<Updated>());
        entityManager.SetComponentData(entity, new EventData
        {
            m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet)),
            m_ConcurrentLimit = m_ConcurrentLimit
        });
    }
}
