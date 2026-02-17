#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TradeSystem : GameSystemBase, ITradeSystem, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct TradeJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> m_GarbageFacilityDatas;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageDatas;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_Limits;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> m_OutsideConnectionDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityEffects;

		public Entity m_City;

		public NativeArray<int> m_TradeBalances;

		public NativeArray<float> m_CachedCosts;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		public NativeArray<int> m_ImportExportsAccumulator;

		public OutsideTradeParameterData m_TradeParameters;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		private TradeCost GetBestCachedTradeCostAmongTypes(Resource resource, OutsideConnectionTransferType types, NativeArray<float> cache)
		{
			float num = float.MaxValue;
			float num2 = float.MaxValue;
			OutsideConnectionTransferType outsideConnectionTransferType = OutsideConnectionTransferType.Road;
			while (outsideConnectionTransferType != OutsideConnectionTransferType.Last)
			{
				if ((outsideConnectionTransferType & OutsideConnectionTransferType.All) == 0 || (outsideConnectionTransferType & types) == 0)
				{
					outsideConnectionTransferType = (OutsideConnectionTransferType)((int)outsideConnectionTransferType << 1);
					continue;
				}
				num = math.min(num, cache[GetCacheIndex(resource, outsideConnectionTransferType, import: true)]);
				num2 = math.min(num2, cache[GetCacheIndex(resource, outsideConnectionTransferType, import: false)]);
				outsideConnectionTransferType = (OutsideConnectionTransferType)((int)outsideConnectionTransferType << 1);
			}
			return new TradeCost
			{
				m_Resource = resource,
				m_BuyCost = num,
				m_SellCost = num2
			};
		}

		public void Execute()
		{
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
				int value = Mathf.RoundToInt((1f - kRefreshRate) * (float)m_TradeBalances[resourceIndex]);
				m_TradeBalances[resourceIndex] = value;
				float weight = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]].m_Weight;
				OutsideConnectionTransferType outsideConnectionTransferType = OutsideConnectionTransferType.Road;
				DynamicBuffer<CityModifier> cityEffects = m_CityEffects[m_City];
				while (outsideConnectionTransferType != OutsideConnectionTransferType.Last)
				{
					if ((outsideConnectionTransferType & OutsideConnectionTransferType.All) == 0)
					{
						outsideConnectionTransferType = (OutsideConnectionTransferType)((int)outsideConnectionTransferType << 1);
						continue;
					}
					TradeCost tradeCost = CalculateTradeCost(iterator.resource, m_TradeBalances[resourceIndex], outsideConnectionTransferType, weight, ref m_TradeParameters, cityEffects);
					Assert.IsTrue(!float.IsNaN(tradeCost.m_SellCost));
					Assert.IsTrue(!float.IsNaN(tradeCost.m_BuyCost));
					m_CachedCosts[GetCacheIndex(iterator.resource, outsideConnectionTransferType, import: false)] = tradeCost.m_SellCost;
					m_CachedCosts[GetCacheIndex(iterator.resource, outsideConnectionTransferType, import: true)] = tradeCost.m_BuyCost;
					outsideConnectionTransferType = (OutsideConnectionTransferType)((int)outsideConnectionTransferType << 1);
				}
			}
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabType);
				BufferAccessor<Game.Economy.Resources> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ResourceType);
				BufferAccessor<TradeCost> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_TradeCostType);
				BufferAccessor<InstalledUpgrade> bufferAccessor3 = archetypeChunk.GetBufferAccessor(ref m_InstalledUpgradeType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					DynamicBuffer<Game.Economy.Resources> resources = bufferAccessor[j];
					Entity prefab = nativeArray2[j].m_Prefab;
					StorageCompanyData storageCompanyData = m_StorageDatas[prefab];
					DynamicBuffer<TradeCost> costs = bufferAccessor2[j];
					if (!m_Limits.HasComponent(prefab))
					{
						continue;
					}
					StorageLimitData data = m_Limits[prefab];
					if (bufferAccessor3.Length != 0)
					{
						UpgradeUtils.CombineStats(ref data, bufferAccessor3[j], ref m_PrefabRefData, ref m_Limits);
					}
					iterator = ResourceIterator.GetIterator();
					int num = EconomyUtils.CountResources(storageCompanyData.m_StoredResources);
					while (iterator.Next())
					{
						bool flag = EconomyUtils.IsOfficeResource(iterator.resource);
						if (!((storageCompanyData.m_StoredResources & iterator.resource) != 0 || flag))
						{
							continue;
						}
						if (iterator.resource == Resource.OutgoingMail)
						{
							EconomyUtils.SetResources(Resource.OutgoingMail, resources, 0);
							continue;
						}
						int resources2 = EconomyUtils.GetResources(iterator.resource, resources);
						int num2 = ((!flag) ? (data.m_Limit / num) : 0);
						if (iterator.resource == Resource.Garbage && m_GarbageFacilityDatas.HasComponent(prefab))
						{
							num2 = m_GarbageFacilityDatas[prefab].m_GarbageCapacity;
						}
						int num3 = (int)math.clamp((long)num2 / 2L - resources2, -2147483648L, 2147483647L);
						float num4 = math.abs((float)num3 / ((float)num2 / 2f));
						int num5 = ((!(num4 > 1f)) ? ((int)((float)num3 * num4 / (float)kUpdatesPerDay) * 8) : num3);
						EconomyUtils.AddResources(iterator.resource, num5, resources);
						int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);
						m_TradeBalances[resourceIndex2] -= num5;
						OutsideConnectionTransferType type = m_OutsideConnectionDatas[prefab].m_Type;
						EconomyUtils.SetTradeCost(iterator.resource, GetBestCachedTradeCostAmongTypes(iterator.resource, type, m_CachedCosts), costs, keepLastTime: false);
						if (num5 != 0 && (iterator.resource & (Resource)28672uL) == Resource.NoResource)
						{
							int resourceIndex3 = EconomyUtils.GetResourceIndex(iterator.resource);
							m_StatisticsEventQueue.Enqueue(new StatisticsEvent
							{
								m_Statistic = StatisticType.Trade,
								m_Change = num5,
								m_Parameter = resourceIndex3
							});
							m_ImportExportsAccumulator[resourceIndex3] += -num5;
						}
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public BufferTypeHandle<TradeCost> __Game_Companies_TradeCost_RW_BufferTypeHandle;

		public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnectionData> __Game_Prefabs_OutsideConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> __Game_Prefabs_GarbageFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Companies_TradeCost_RW_BufferTypeHandle = state.GetBufferTypeHandle<TradeCost>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>();
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup = state.GetComponentLookup<OutsideConnectionData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup = state.GetComponentLookup<GarbageFacilityData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private static readonly float kRefreshRate = 0.01f;

	public static readonly int kUpdatesPerDay = 128;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_StorageGroup;

	private EntityQuery m_TradeParameterQuery;

	private EntityQuery m_CityQuery;

	private ResourceSystem m_ResourceSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	[DebugWatchDeps]
	private JobHandle m_DebugTradeBalanceDeps;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_TradeBalances;

	private NativeArray<float> m_CachedCosts;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public float GetBestTradePriceAmongTypes(Resource resource, OutsideConnectionTransferType types, bool import, DynamicBuffer<CityModifier> cityEffects)
	{
		OutsideConnectionTransferType outsideConnectionTransferType = OutsideConnectionTransferType.Road;
		float value = float.MaxValue;
		while (outsideConnectionTransferType != OutsideConnectionTransferType.Last)
		{
			if ((outsideConnectionTransferType & OutsideConnectionTransferType.All) == 0 || (outsideConnectionTransferType & types) == 0)
			{
				outsideConnectionTransferType = (OutsideConnectionTransferType)((int)outsideConnectionTransferType << 1);
				continue;
			}
			value = math.min(value, m_CachedCosts[GetCacheIndex(resource, outsideConnectionTransferType, import)]);
			outsideConnectionTransferType = (OutsideConnectionTransferType)((int)outsideConnectionTransferType << 1);
		}
		if (import)
		{
			CityUtils.ApplyModifier(ref value, cityEffects, CityModifierType.ImportCost);
		}
		else
		{
			CityUtils.ApplyModifier(ref value, cityEffects, CityModifierType.ExportCost);
		}
		return value;
	}

	private static int GetCacheIndex(Resource resource, OutsideConnectionTransferType type, bool import)
	{
		Assert.IsTrue(math.countbits((int)type) == 1, "Invalid OutsideConnectionTransferType passed, this function only accept single OCTransferType");
		return Mathf.RoundToInt(math.log2((float)type) * 2f * (float)EconomyUtils.ResourceCount + (float)(2 * EconomyUtils.GetResourceIndex(resource)) + (float)(import ? 1 : 0));
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_TradeBalances = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
		m_CachedCosts = new NativeArray<float>(2 * EconomyUtils.ResourceCount * Mathf.RoundToInt(math.log2(32f)), Allocator.Persistent);
		m_StorageGroup = GetEntityQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<Game.Objects.OutsideConnection>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Economy.Resources>(), ComponentType.ReadOnly<TradeCost>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_TradeParameterQuery = GetEntityQuery(ComponentType.ReadOnly<OutsideTradeParameterData>());
		m_CityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>());
		RequireForUpdate(m_StorageGroup);
		RequireForUpdate(m_TradeParameterQuery);
		RequireForUpdate(m_CityQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_CachedCosts.Dispose();
		m_TradeBalances.Dispose();
		base.OnDestroy();
	}

	private static TradeCost CalculateTradeCost(Resource resource, int tradeBalance, OutsideConnectionTransferType type, float weight, ref OutsideTradeParameterData tradeParameters, DynamicBuffer<CityModifier> cityEffects)
	{
		Assert.IsTrue(math.countbits((int)type) == 1, "Invalid OutsideConnectionTransferType passed, this function only accept single OCTransferType");
		float value = tradeParameters.GetWeightCostSingle(type) * weight;
		if ((float)tradeBalance < 0f)
		{
			value *= 1f + tradeParameters.GetDistanceCostSingle(type) * math.max(50f, math.sqrt(-tradeBalance));
		}
		CityUtils.ApplyModifier(ref value, cityEffects, CityModifierType.ImportCost);
		float value2 = tradeParameters.GetWeightCostSingle(type) * weight;
		if ((float)tradeBalance > 0f)
		{
			value2 *= 1f + tradeParameters.GetDistanceCostSingle(type) * math.max(50f, math.sqrt(tradeBalance));
		}
		CityUtils.ApplyModifier(ref value2, cityEffects, CityModifierType.ExportCost);
		return new TradeCost
		{
			m_Resource = resource,
			m_BuyCost = value,
			m_SellCost = value2
		};
	}

	protected override void OnGameLoaded(Context context)
	{
		if (context.purpose != Purpose.NewGame)
		{
			return;
		}
		NativeArray<Entity> nativeArray = m_StorageGroup.ToEntityArray(Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity entity = nativeArray[i];
			if (!base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component) || !base.EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<Game.Economy.Resources> buffer) || !base.EntityManager.TryGetComponent<StorageCompanyData>(component, out var component2) || !base.EntityManager.TryGetComponent<StorageLimitData>(component, out var component3))
			{
				continue;
			}
			ResourceIterator iterator = ResourceIterator.GetIterator();
			int num = EconomyUtils.CountResources(component2.m_StoredResources);
			while (iterator.Next())
			{
				if ((component2.m_StoredResources & iterator.resource) != Resource.NoResource)
				{
					if (iterator.resource == Resource.OutgoingMail)
					{
						EconomyUtils.SetResources(Resource.OutgoingMail, buffer, 0);
						continue;
					}
					int resources = EconomyUtils.GetResources(iterator.resource, buffer);
					int amount = component3.m_Limit / num / 2 - resources;
					EconomyUtils.AddResources(iterator.resource, amount, buffer);
				}
			}
		}
		nativeArray.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		JobHandle deps2;
		JobHandle jobHandle = IJobExtensions.Schedule(new TradeJob
		{
			m_Chunks = m_StorageGroup.ToArchetypeChunkArray(Allocator.TempJob),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TradeCostType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Limits = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OutsideConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageFacilityDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_City = m_CityQuery.GetSingletonEntity(),
			m_TradeBalances = m_TradeBalances,
			m_CachedCosts = m_CachedCosts,
			m_TradeParameters = m_TradeParameterQuery.GetSingleton<OutsideTradeParameterData>(),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
			m_ImportExportsAccumulator = m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.ImportExport, out deps2)
		}, JobHandle.CombineDependencies(base.Dependency, deps, deps2));
		m_CityStatisticsSystem.AddWriter(jobHandle);
		m_CityProductionStatisticSystem.AddCityUsageAccumulatorWriter(CityProductionStatisticSystem.CityResourceUsage.Consumer.ImportExport, jobHandle);
		m_ResourceSystem.AddPrefabsReader(jobHandle);
		m_DebugTradeBalanceDeps = jobHandle;
		base.Dependency = jobHandle;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_TradeBalances);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.tradeBalance)
		{
			if (reader.context.format.Has(FormatTags.FishResource))
			{
				NativeArray<int> value = m_TradeBalances;
				reader.Read(value);
			}
			else
			{
				NativeArray<int> subArray = m_TradeBalances.GetSubArray(0, 40);
				reader.Read(subArray);
				m_TradeBalances[40] = 0;
			}
		}
	}

	public void SetDefaults()
	{
		for (int i = 0; i < m_TradeBalances.Length; i++)
		{
			m_TradeBalances[i] = 0;
		}
	}

	public void SetDefaults(Context context)
	{
		SetDefaults();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public TradeSystem()
	{
	}
}
