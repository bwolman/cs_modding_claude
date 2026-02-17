// Decompiled from Game.dll -> Game.Simulation.TaxSystem
// Decompiled with ilspycmd on 2026-02-16

using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Areas;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TaxSystem : GameSystemBase, ITaxSystem, IDefaultSerializable, ISerializable, IPostDeserialize
{
    [BurstCompile]
    private struct PayTaxJob : IJobChunk
    {
        [ReadOnly] public EntityTypeHandle m_EntityType;
        public ComponentTypeHandle<TaxPayer> m_TaxPayerType;
        [ReadOnly] public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;
        public BufferTypeHandle<Resources> m_ResourceType;
        [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
        [ReadOnly] public ComponentLookup<Worker> m_Workers;
        [ReadOnly] public ComponentLookup<PrefabRef> m_Prefabs;
        [ReadOnly] public ComponentLookup<IndustrialProcessData> m_ProcessDatas;
        [ReadOnly] public ComponentLookup<ResourceData> m_ResourceDatas;
        [ReadOnly] public ResourcePrefabs m_ResourcePrefabs;
        public uint m_UpdateFrameIndex;
        public IncomeSource m_Type;
        public float m_PaidMultiplier;
        public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

        private void PayTax(ref TaxPayer taxPayer, Entity entity, DynamicBuffer<Resources> resources, IncomeSource taxType, NativeQueue<StatisticsEvent>.ParallelWriter statisticsEventQueue)
        {
            int tax = GetTax(taxPayer);
            int num = (int)math.round(m_PaidMultiplier * (float)tax);
            EconomyUtils.AddResources(Resource.Money, -num, resources);
            // ... categorization logic ...
            taxPayer.m_UntaxedIncome = 0;
            taxPayer.m_AverageTaxPaid = tax * kUpdatesPerDay;
        }
    }

    public static readonly int kUpdatesPerDay = 32;
    private NativeArray<int> m_TaxRates; // 92 entries
    private float3 m_TaxPaidMultiplier;  // x=residential, y=commercial, z=industrial

    public int TaxRate
    {
        get => m_TaxRates[0];
        set
        {
            m_TaxRates[0] = math.min(m_TaxParameterData.m_TotalTaxLimits.y, math.max(m_TaxParameterData.m_TotalTaxLimits.x, value));
            EnsureAreaTaxRateLimits(TaxAreaType.Residential);
            EnsureAreaTaxRateLimits(TaxAreaType.Commercial);
            EnsureAreaTaxRateLimits(TaxAreaType.Industrial);
            EnsureAreaTaxRateLimits(TaxAreaType.Office);
        }
    }

    public override int GetUpdateInterval(SystemUpdatePhase phase) => 262144 / (kUpdatesPerDay * 16);

    public static int GetTax(TaxPayer payer) => (int)math.round(0.01f * (float)payer.m_AverageTaxRate * (float)payer.m_UntaxedIncome);

    public static int GetTaxRate(TaxAreaType areaType, NativeArray<int> taxRates) => taxRates[0] + taxRates[(int)areaType];

    public static int GetResidentialTaxRate(int jobLevel, NativeArray<int> taxRates) => GetTaxRate(TaxAreaType.Residential, taxRates) + taxRates[5 + jobLevel];

    public static int GetCommercialTaxRate(Resource resource, NativeArray<int> taxRates) => GetTaxRate(TaxAreaType.Commercial, taxRates) + taxRates[10 + EconomyUtils.GetResourceIndex(resource)];

    public static int GetIndustrialTaxRate(Resource resource, NativeArray<int> taxRates) => GetTaxRate(TaxAreaType.Industrial, taxRates) + taxRates[51 + EconomyUtils.GetResourceIndex(resource)];

    public static int GetOfficeTaxRate(Resource resource, NativeArray<int> taxRates) => GetTaxRate(TaxAreaType.Office, taxRates) + taxRates[51 + EconomyUtils.GetResourceIndex(resource)];
}
