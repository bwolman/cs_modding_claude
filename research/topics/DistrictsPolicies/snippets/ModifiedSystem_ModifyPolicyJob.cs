// Source: Game.Policies.ModifiedSystem.ModifyPolicyJob.Execute (simplified)
// Decompiled from Game.dll

public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
{
    NativeArray<Modify> nativeArray = chunk.GetNativeArray(ref m_ModifyType);
    DynamicBuffer<Policy> policies = default;
    for (int i = 0; i < nativeArray.Length; i++)
    {
        Modify modify = nativeArray[i];
        if (m_Policies.TryGetBuffer(modify.m_Entity, ref policies))
        {
            m_CommandBuffer.AddComponent<Updated>(modify.m_Entity);
            int num = 0;
            while (true)
            {
                if (num < policies.Length)
                {
                    Policy policy = policies[num];
                    if (policy.m_Policy == modify.m_Policy)
                    {
                        if ((modify.m_Flags & PolicyFlags.Active) == 0)
                        {
                            // Deactivating: remove from buffer (or keep if slider with non-default)
                            policies.RemoveAt(num);
                            RefreshEffects(modify.m_Entity, modify.m_Policy, policies);
                            break;
                        }
                        // Updating: change flags and adjustment
                        policy.m_Flags = modify.m_Flags;
                        policy.m_Adjustment = modify.m_Adjustment;
                        policies[num] = policy;
                        RefreshEffects(modify.m_Entity, modify.m_Policy, policies);
                        break;
                    }
                    num++;
                    continue;
                }
                // Not found -- activating new policy
                if ((modify.m_Flags & PolicyFlags.Active) != 0)
                {
                    policies.Add(new Policy(modify.m_Policy, modify.m_Flags, modify.m_Adjustment));
                    RefreshEffects(modify.m_Entity, modify.m_Policy, policies);
                    m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.PolicyActivated, modify.m_Policy, Entity.Null, Entity.Null));
                }
                break;
            }
        }
    }
}
