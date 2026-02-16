// Decompiled from Game.dll -> Game.Simulation.CollapsedBuildingSystem
// This system manages destroyed/collapsed buildings and is the CREATOR and CLEANER of RescueTarget.

// --- CollapsedBuildingJob.Execute ---
// Runs on all entities with Destroyed + (Building | Extension), excluding Deleted/Temp.
// Update interval: 64 frames.
//
// Two branches based on whether the entity already has RescueTarget:
//
// 1. Entity HAS RescueTarget:
//    - If Destroyed.m_Cleared < 1.0: call RequestRescueIfNeeded (keeps fire engines coming)
//    - If Destroyed.m_Cleared >= 1.0: REMOVE RescueTarget component
//
// 2. Entity does NOT have RescueTarget:
//    - Manages the collapse animation (m_Cleared starts negative, increments toward 0)
//    - Once m_Cleared >= 0 and building has RequireRoad flag (road-connected building):
//      ADD RescueTarget component and call RequestRescueIfNeeded
//    - If building does NOT have RequireRoad: set m_Cleared = 1.0 (skip rescue entirely)

public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
{
    NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
    NativeArray<Destroyed> nativeArray2 = chunk.GetNativeArray(ref m_DestroyedType);
    NativeArray<RescueTarget> nativeArray3 = chunk.GetNativeArray(ref m_RescueTargetType);
    // ...

    if (nativeArray3.Length != 0)
    {
        // Branch 1: Entity already has RescueTarget
        for (int i = 0; i < nativeArray2.Length; i++)
        {
            Destroyed destroyed = nativeArray2[i];
            Entity entity = nativeArray[i];
            if (destroyed.m_Cleared < 1f)
            {
                RescueTarget rescueTarget = nativeArray3[i];
                RequestRescueIfNeeded(unfilteredChunkIndex, entity, rescueTarget);
            }
            else
            {
                // *** THIS IS THE CLEANUP: RescueTarget is removed when clearing is complete ***
                m_CommandBuffer.RemoveComponent<RescueTarget>(unfilteredChunkIndex, entity);
            }
        }
    }
    else
    {
        // Branch 2: Entity does NOT have RescueTarget yet
        for (int j = 0; j < nativeArray2.Length; j++)
        {
            ref Destroyed reference = ref nativeArray2.ElementAt(j);
            PrefabRef prefabRef = nativeArray5[j];
            bool flag4 = false;
            if (m_PrefabBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
            {
                flag4 = (componentData.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0;
                // ... also checks owner's building flags ...
            }

            if (reference.m_Cleared < 0f)
            {
                // Collapse animation still playing
                Entity e = nativeArray[j];
                reference.m_Cleared += 1.0666667f;
                if (reference.m_Cleared >= 0f)
                {
                    reference.m_Cleared = math.select(1f, 0f, flag4);
                    // ... remove interpolation, add Updated ...
                }
            }
            else if (reference.m_Cleared < 1f && !flag3)
            {
                if (flag4)
                {
                    // *** THIS IS THE CREATION: RescueTarget added to road-connected buildings ***
                    Entity entity2 = nativeArray[j];
                    RescueTarget rescueTarget2 = default(RescueTarget);
                    RequestRescueIfNeeded(unfilteredChunkIndex, entity2, rescueTarget2);
                    m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, rescueTarget2);
                }
                else
                {
                    // Non-road buildings skip rescue entirely
                    reference.m_Cleared = 1f;
                }
            }
        }
    }
    // ... cleanup/deletion of fully cleared buildings ...
}

// --- RequestRescueIfNeeded ---
// Creates a FireRescueRequest (Disaster type) if no active request exists.
// Priority is hardcoded at 10f (higher than normal fire requests which use intensity).
private void RequestRescueIfNeeded(int jobIndex, Entity entity, RescueTarget rescueTarget)
{
    if (!m_FireRescueRequestData.HasComponent(rescueTarget.m_Request))
    {
        Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_RescueRequestArchetype);
        m_CommandBuffer.SetComponent(jobIndex, e, new FireRescueRequest(entity, 10f, FireRescueRequestType.Disaster));
        m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
    }
}
