using Game.Areas;
using Game.Buildings;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public static class EventHelpers
{
	public struct FireHazardData
	{
		public LocalEffectSystem.ReadData m_LocalEffectData;

		public float m_ForestFireHazardFactor;

		public ComponentLookup<DestructibleObjectData> m_PrefabDestructibleObjectData;

		public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		public ComponentLookup<ZonePropertiesData> m_ZonePropertiesData;

		public FireHazardData(SystemBase system)
		{
			m_LocalEffectData = default(LocalEffectSystem.ReadData);
			m_ForestFireHazardFactor = 0f;
			m_PrefabDestructibleObjectData = system.GetComponentLookup<DestructibleObjectData>(isReadOnly: true);
			m_PrefabSpawnableBuildingData = system.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			m_ServiceCoverages = system.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			m_DistrictModifiers = system.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			m_ZonePropertiesData = system.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
		}

		public void Update(SystemBase system, LocalEffectSystem.ReadData localEffectData, FireConfigurationPrefab fireConfigurationPrefab, float temperature, float noRainDays)
		{
			m_LocalEffectData = localEffectData;
			m_ForestFireHazardFactor = fireConfigurationPrefab.m_TemperatureForestFireHazard.Evaluate(temperature);
			m_ForestFireHazardFactor *= fireConfigurationPrefab.m_NoRainForestFireHazard.Evaluate(noRainDays);
			m_PrefabDestructibleObjectData.Update(system);
			m_PrefabSpawnableBuildingData.Update(system);
			m_ServiceCoverages.Update(system);
			m_DistrictModifiers.Update(system);
			m_ZonePropertiesData.Update(system);
		}

		// BUILDING PATH: zone properties, service coverage, district modifiers
		public bool GetFireHazard(PrefabRef prefabRef, Building building, CurrentDistrict currentDistrict, Damaged damaged, UnderConstruction underConstruction, out float fireHazard, out float riskFactor)
		{
			fireHazard = GetFireHazard(prefabRef);
			fireHazard = math.select(fireHazard, 0f, underConstruction.m_NewPrefab == Entity.Null && underConstruction.m_Progress < byte.MaxValue);
			riskFactor = 0f;
			if (fireHazard == 0f)
			{
				return false;
			}
			if (m_PrefabSpawnableBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				fireHazard *= 1f - (float)(componentData.m_Level - 1) * 0.03f;
				if (m_ZonePropertiesData.TryGetComponent(componentData.m_ZonePrefab, out var componentData2))
				{
					fireHazard *= componentData2.m_FireHazardMultiplier;
				}
			}
			float num = 0f;
			if (m_ServiceCoverages.TryGetBuffer(building.m_RoadEdge, out var bufferData))
			{
				num = NetUtils.GetServiceCoverage(bufferData, CoverageService.FireRescue, building.m_CurvePosition);
				fireHazard *= math.max(0.01f, 1f - num * 0.01f);
			}
			if (m_DistrictModifiers.TryGetBuffer(currentDistrict.m_District, out var bufferData2))
			{
				AreaUtils.ApplyModifier(ref fireHazard, bufferData2, DistrictModifierType.BuildingFireHazard);
			}
			riskFactor = fireHazard / (1f + num * 0.5f);
			fireHazard *= GetFireHazardFactor(damaged);
			return true;
		}

		// TREE PATH: temperature x rain curves, local effect modifiers
		public bool GetFireHazard(PrefabRef prefabRef, Tree tree, Transform transform, Damaged damaged, out float fireHazard, out float riskFactor)
		{
			fireHazard = GetFireHazard(prefabRef);
			riskFactor = 0f;
			if (fireHazard == 0f)
			{
				return false;
			}
			fireHazard *= m_ForestFireHazardFactor;
			m_LocalEffectData.ApplyModifier(ref fireHazard, transform.m_Position, LocalModifierType.ForestFireHazard);
			riskFactor = fireHazard;
			m_LocalEffectData.ApplyModifier(ref riskFactor, transform.m_Position, LocalModifierType.ForestFireResponseTime);
			fireHazard *= GetFireHazardFactor(damaged);
			return true;
		}

		// Base hazard from prefab: DestructibleObjectData.m_FireHazard, or 100f default
		private float GetFireHazard(PrefabRef prefabRef)
		{
			if (m_PrefabDestructibleObjectData.HasComponent(prefabRef.m_Prefab))
			{
				return m_PrefabDestructibleObjectData[prefabRef.m_Prefab].m_FireHazard;
			}
			return 100f;
		}

		// Damage reduces fire hazard: (1 - sum(damage.yz))^4
		// Fully damaged entities (structural + weather = 1.0) have zero fire hazard
		private float GetFireHazardFactor(Damaged damaged)
		{
			float num = math.max(0f, 1f - math.csum(damaged.m_Damage.yz));
			float num2 = num * num;
			return num2 * num2;
		}
	}

	public struct StructuralIntegrityData
	{
		public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

		public ComponentLookup<DestructibleObjectData> m_PrefabDestructibleObjectData;

		public FireConfigurationData m_FireConfigurationData;

		public StructuralIntegrityData(SystemBase system)
		{
			m_PrefabSpawnableBuildingData = system.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			m_PrefabDestructibleObjectData = system.GetComponentLookup<DestructibleObjectData>(isReadOnly: true);
			m_FireConfigurationData = default(FireConfigurationData);
		}

		public void Update(SystemBase system, FireConfigurationData fireConfigurationData)
		{
			m_PrefabSpawnableBuildingData.Update(system);
			m_PrefabDestructibleObjectData.Update(system);
			m_FireConfigurationData = fireConfigurationData;
		}

		// Returns structural integrity for a prefab:
		// 1. DestructibleObjectData.m_StructuralIntegrity if present
		// 2. For buildings: level-based from FireConfigurationData (12000-16000)
		// 3. For non-buildings: m_DefaultStructuralIntegrity (3000)
		public float GetStructuralIntegrity(Entity prefab, bool isBuilding)
		{
			if (m_PrefabDestructibleObjectData.HasComponent(prefab))
			{
				return m_PrefabDestructibleObjectData[prefab].m_StructuralIntegrity;
			}
			if (isBuilding)
			{
				if (m_PrefabSpawnableBuildingData.HasComponent(prefab))
				{
					return m_PrefabSpawnableBuildingData[prefab].m_Level switch
					{
						1 => m_FireConfigurationData.m_StructuralIntegrityLevel1,
						2 => m_FireConfigurationData.m_StructuralIntegrityLevel2,
						3 => m_FireConfigurationData.m_StructuralIntegrityLevel3,
						4 => m_FireConfigurationData.m_StructuralIntegrityLevel4,
						5 => m_FireConfigurationData.m_StructuralIntegrityLevel5,
						_ => m_FireConfigurationData.m_BuildingStructuralIntegrity,
					};
				}
				return m_FireConfigurationData.m_BuildingStructuralIntegrity;
			}
			return m_FireConfigurationData.m_DefaultStructuralIntegrity;
		}
	}
}
