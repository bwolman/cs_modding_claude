// Extracted from Game.dll → Game.Prefabs.EventPrefab
// Shows how the TrafficAccident event entity archetype is assembled at prefab load time

namespace Game.Prefabs;

public class EventPrefab : PrefabBase
{
    public int m_ConcurrentLimit;

    // --- GetArchetypeComponents ---
    // Adds Game.Events.Event (the PERSISTENT event marker, NOT Game.Common.Event)
    public override void GetArchetypeComponents(HashSet<ComponentType> components)
    {
        base.GetArchetypeComponents(components);  // PrefabBase adds PrefabRef
        components.Add(ComponentType.ReadWrite<Game.Events.Event>());
    }

    // --- RefreshArchetype ---
    // Collects components from all ComponentBase children, adds Created/Updated,
    // then stores the resulting EntityArchetype in EventData.m_Archetype
    protected virtual void RefreshArchetype(EntityManager entityManager, Entity entity)
    {
        List<ComponentBase> list = new List<ComponentBase>();
        GetComponents(list);
        HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
        for (int i = 0; i < list.Count; i++)
        {
            list[i].GetArchetypeComponents(hashSet);
        }
        hashSet.Add(ComponentType.ReadWrite<Created>());    // unconditional
        hashSet.Add(ComponentType.ReadWrite<Updated>());    // unconditional
        entityManager.SetComponentData(entity, new EventData
        {
            m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet)),
            m_ConcurrentLimit = m_ConcurrentLimit
        });
    }
}
