// Extracted from: Game.dll -> Game.Events.InitializeSystem
// This is the CRITICAL missing link: InitializeSystem processes new crime event entities
// (created by CrimeCheckSystem) and creates AddCriminal command entities that
// AddCriminalSystem then uses to add the Criminal component to citizens.

// InitializeSystem.OnUpdate processes all entities with Created + Event tags:
//   if (base.EntityManager.HasComponent<Crime>(entity))
//   {
//       InitializeCrimeEvent(entity);
//   }

// m_CriminalEventArchetype is created in OnCreate:
//   m_CriminalEventArchetype = base.EntityManager.CreateArchetype(
//       ComponentType.ReadWrite<Game.Common.Event>(),
//       ComponentType.ReadWrite<AddCriminal>()
//   );

private void InitializeCrimeEvent(Entity eventEntity)
{
    PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(eventEntity);
    CrimeData componentData2 = base.EntityManager.GetComponentData<CrimeData>(componentData.m_Prefab);
    if (componentData2.m_RandomTargetType == EventTargetType.None)
    {
        return;
    }
    DynamicBuffer<TargetElement> buffer = base.EntityManager.GetBuffer<TargetElement>(eventEntity);
    if (buffer.Length == 0)
    {
        // If no targets specified, picks a random citizen from the world
        AddRandomTarget(buffer, componentData2.m_RandomTargetType, TransportType.None);
    }
    RandomSeed.Next().GetRandom(eventEntity.Index);
    EntityCommandBuffer commandBuffer = GetCommandBuffer();
    for (int i = 0; i < buffer.Length; i++)
    {
        Entity entity = buffer[i].m_Entity;
        // If target is a Creature/Resident, resolve to the underlying Citizen entity
        if (base.EntityManager.TryGetComponent<Game.Creatures.Resident>(entity, out var component))
        {
            entity = component.m_Citizen;
            buffer[i] = new TargetElement(entity);
        }
        // Only create AddCriminal for actual Citizen entities
        if (base.EntityManager.TryGetComponent<Citizen>(entity, out var _))
        {
            CriminalFlags criminalFlags = CriminalFlags.Planning;
            if (componentData2.m_CrimeType == CrimeType.Robbery)
            {
                criminalFlags |= CriminalFlags.Robber;
            }
            // Create AddCriminal command entity — processed by AddCriminalSystem
            Entity e = commandBuffer.CreateEntity(m_CriminalEventArchetype);
            commandBuffer.SetComponent(e, new AddCriminal
            {
                m_Event = eventEntity,
                m_Target = entity,
                m_Flags = criminalFlags
            });
        }
    }
}
