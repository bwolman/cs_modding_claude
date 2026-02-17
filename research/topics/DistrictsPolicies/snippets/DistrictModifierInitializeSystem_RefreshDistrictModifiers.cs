// Source: Game.Policies.DistrictModifierInitializeSystem.DistrictModifierRefreshData
// Decompiled from Game.dll

public void RefreshDistrictModifiers(DynamicBuffer<DistrictModifier> modifiers, DynamicBuffer<Policy> policies)
{
    modifiers.Clear();
    for (int i = 0; i < policies.Length; i++)
    {
        Policy policy = policies[i];
        if ((policy.m_Flags & PolicyFlags.Active) == 0 || !m_DistrictModifierData.HasBuffer(policy.m_Policy))
        {
            continue;
        }
        DynamicBuffer<DistrictModifierData> val = m_DistrictModifierData[policy.m_Policy];
        for (int j = 0; j < val.Length; j++)
        {
            DistrictModifierData modifierData = val[j];
            float delta;
            if (m_PolicySliderData.HasComponent(policy.m_Policy))
            {
                PolicySliderData policySliderData = m_PolicySliderData[policy.m_Policy];
                float num = (policy.m_Adjustment - policySliderData.m_Range.min) / (policySliderData.m_Range.max - policySliderData.m_Range.min);
                num = math.select(num, 0f, policySliderData.m_Range.min == policySliderData.m_Range.max);
                num = math.saturate(num);
                delta = math.lerp(modifierData.m_Range.min, modifierData.m_Range.max, num);
            }
            else
            {
                delta = modifierData.m_Range.min;
            }
            AddModifier(modifiers, modifierData, delta);
        }
    }
}

private static void AddModifier(DynamicBuffer<DistrictModifier> modifiers, DistrictModifierData modifierData, float delta)
{
    while (modifiers.Length <= (int)modifierData.m_Type)
    {
        modifiers.Add(default(DistrictModifier));
    }
    DistrictModifier districtModifier = modifiers[(int)modifierData.m_Type];
    switch (modifierData.m_Mode)
    {
    case ModifierValueMode.Relative:
        districtModifier.m_Delta.y = districtModifier.m_Delta.y * (1f + delta) + delta;
        break;
    case ModifierValueMode.Absolute:
        districtModifier.m_Delta.x += delta;
        break;
    case ModifierValueMode.InverseRelative:
        delta = 1f / math.max(0.001f, 1f + delta) - 1f;
        districtModifier.m_Delta.y = districtModifier.m_Delta.y * (1f + delta) + delta;
        break;
    }
    modifiers[(int)modifierData.m_Type] = districtModifier;
}
