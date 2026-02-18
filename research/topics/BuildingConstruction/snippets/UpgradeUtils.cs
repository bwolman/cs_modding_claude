using Colossal.Entities;
using Game.Buildings;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public static class UpgradeUtils
{
	public static void CombineStats<T>(ref T result, BufferAccessor<InstalledUpgrade> accessor, int i, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<T> combineDatas) where T : unmanaged, IComponentData, ICombineData<T>
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		if (accessor.Length != 0)
		{
			CombineStats(ref result, accessor[i], ref prefabs, ref combineDatas);
		}
	}

	public static bool CombineStats<T>(ref T data, DynamicBuffer<InstalledUpgrade> upgrades, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<T> combineDatas) where T : unmanaged, IComponentData, ICombineData<T>
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		T other = default(T);
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && combineDatas.TryGetComponent(prefabs[installedUpgrade.m_Upgrade].m_Prefab, ref other))
			{
				data.Combine(other);
				result = true;
			}
		}
		return result;
	}

	public static bool CombinePollutionStats(ref PollutionData data, DynamicBuffer<InstalledUpgrade> upgrades, ref ComponentLookup<PrefabRef> prefabs, ref ComponentLookup<PollutionData> combineDatas, ref ComponentLookup<PollutionEmitModifier> modifierDatas)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		PollutionData pollutionData = default(PollutionData);
		PollutionEmitModifier pollutionEmitModifier = default(PollutionEmitModifier);
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && combineDatas.TryGetComponent(prefabs[installedUpgrade.m_Upgrade].m_Prefab, ref pollutionData))
			{
				if (modifierDatas.TryGetComponent((Entity)installedUpgrade, ref pollutionEmitModifier))
				{
					pollutionEmitModifier.UpdatePollutionData(ref pollutionData);
				}
				data.Combine(pollutionData);
				result = true;
			}
		}
		return result;
	}

	public static bool CombineStats<T>(EntityManager entityManager, ref T data, DynamicBuffer<InstalledUpgrade> upgrades) where T : unmanaged, IComponentData, ICombineData<T>
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		PrefabRef prefabRef = default(PrefabRef);
		T other = default(T);
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive) && EntitiesExtensions.TryGetComponent<PrefabRef>(entityManager, installedUpgrade.m_Upgrade, ref prefabRef) && EntitiesExtensions.TryGetComponent<T>(entityManager, prefabRef.m_Prefab, ref other))
			{
				data.Combine(other);
				result = true;
			}
		}
		return result;
	}

	public static void CombineStats<T>(NativeList<T> result, DynamicBuffer<InstalledUpgrade> upgrades, ref ComponentLookup<PrefabRef> prefabs, ref BufferLookup<T> combineDatas) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < upgrades.Length; i++)
		{
			InstalledUpgrade installedUpgrade = upgrades[i];
			if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
			{
				CombineStats(result, prefabs[installedUpgrade.m_Upgrade].m_Prefab, ref combineDatas);
			}
		}
	}

	public static void CombineStats<T>(NativeList<T> result, Entity prefab, ref BufferLookup<T> combineDatas) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		DynamicBuffer<T> combineData = default(DynamicBuffer<T>);
		if (combineDatas.TryGetBuffer(prefab, ref combineData))
		{
			CombineStats<T>(result, combineData);
		}
	}

	public static void CombineStats<T>(NativeList<T> result, DynamicBuffer<T> combineData) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < combineData.Length; i++)
		{
			combineData[i].Combine(result);
		}
	}

	public static void CombineStats<T>(NativeList<T> result, T combineData) where T : unmanaged, IBufferElementData, ICombineBuffer<T>
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		combineData.Combine(result);
	}

	public static bool TryGetCombinedComponent<T>(EntityManager entityManager, Entity entity, Entity prefab, out T data) where T : unmanaged, IComponentData, ICombineData<T>
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		bool flag = EntitiesExtensions.TryGetComponent<T>(entityManager, prefab, ref data);
		return TryCombineData(entityManager, entity, ref data) || flag;
	}

	public static bool TryCombineData<T>(EntityManager entityManager, Entity entity, ref T data) where T : unmanaged, IComponentData, ICombineData<T>
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		DynamicBuffer<InstalledUpgrade> upgrades = default(DynamicBuffer<InstalledUpgrade>);
		if (EntitiesExtensions.TryGetBuffer<InstalledUpgrade>(entityManager, entity, true, ref upgrades))
		{
			return CombineStats(entityManager, ref data, upgrades);
		}
		return false;
	}

	public static bool TryGetCombinedComponent<T>(Entity entity, out T data, ref ComponentLookup<PrefabRef> prefabRefLookup, ref ComponentLookup<T> combineDataLookup, ref BufferLookup<InstalledUpgrade> installedUpgradeLookup) where T : unmanaged, IComponentData, ICombineData<T>
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		data = default(T);
		PrefabRef prefabRef = default(PrefabRef);
		bool flag = prefabRefLookup.TryGetComponent(entity, ref prefabRef) && combineDataLookup.TryGetComponent(prefabRef.m_Prefab, ref data);
		DynamicBuffer<InstalledUpgrade> upgrades = default(DynamicBuffer<InstalledUpgrade>);
		if (installedUpgradeLookup.TryGetBuffer(entity, ref upgrades))
		{
			flag |= CombineStats(ref data, upgrades, ref prefabRefLookup, ref combineDataLookup);
		}
		return flag;
	}
}
