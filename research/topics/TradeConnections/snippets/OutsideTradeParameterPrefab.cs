using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", true, new Type[] { })]
public class OutsideTradeParameterPrefab : PrefabBase
{
	[Header("Electricity")]
	[Tooltip("Expense for importing 0.1 kW of electricity for 24h")]
	public float m_ElectricityImportPrice;

	[Tooltip("Revenue for exporting 0.1 kW of electricity for 24h")]
	public float m_ElectricityExportPrice;

	[Header("Water & Sewage")]
	[Tooltip("Expense for importing 1m^3 of water for 24h")]
	public float m_WaterImportPrice;

	[Tooltip("Revenue for exporting 1m^3 of water for 24h")]
	public float m_WaterExportPrice;

	[Tooltip("Percentage of pollution when the water export revenue becomes zero")]
	[Range(0f, 1f)]
	public float m_WaterExportPollutionTolerance = 0.1f;

	[Tooltip("Expense for importing 1m^3 of sewage for 24h")]
	public float m_SewageExportPrice;

	[Header("Resource Trade")]
	public float m_AirWeightMultiplier;

	public float m_RoadWeightMultiplier;

	public float m_TrainWeightMultiplier;

	public float m_ShipWeightMultiplier;

	public float m_AirDistanceMultiplier;

	public float m_RoadDistanceMultiplier;

	public float m_TrainDistanceMultiplier;

	public float m_ShipDistanceMultiplier;

	[Tooltip("Service fees for ambulance import service, multiply by population")]
	public float m_AmbulanceImportServiceFee = 1f;

	[Tooltip("Service fees for Hearse import service, multiply by population")]
	public float m_HearseImportServiceFee = 1f;

	[Tooltip("Service fees for FireEngine import service, multiply by population")]
	public float m_FireEngineImportServiceFee = 1f;

	[Tooltip("Service fees for Garbage import service, multiply by population")]
	public float m_GarbageImportServiceFee = 1f;

	[Tooltip("Service fees for Police import service, multiply by population")]
	public float m_PoliceImportServiceFee = 1f;

	[Tooltip("Service fees from outside will change based on this population range,0 - 1000 => service fee * 1000, 1000 - 2000 => service fee * 2000")]
	public int m_OCServiceTradePopulationRange = 1000;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<OutsideTradeParameterData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new OutsideTradeParameterData
		{
			m_ElectricityImportPrice = m_ElectricityImportPrice,
			m_ElectricityExportPrice = m_ElectricityExportPrice,
			m_WaterImportPrice = m_WaterImportPrice,
			m_WaterExportPrice = m_WaterExportPrice,
			m_WaterExportPollutionTolerance = m_WaterExportPollutionTolerance,
			m_SewageExportPrice = m_SewageExportPrice,
			m_AirDistanceMultiplier = m_AirDistanceMultiplier,
			m_RoadDistanceMultiplier = m_RoadDistanceMultiplier,
			m_TrainDistanceMultiplier = m_TrainDistanceMultiplier,
			m_ShipDistanceMultiplier = m_ShipDistanceMultiplier,
			m_AirWeightMultiplier = m_AirWeightMultiplier,
			m_RoadWeightMultiplier = m_RoadWeightMultiplier,
			m_TrainWeightMultiplier = m_TrainWeightMultiplier,
			m_ShipWeightMultiplier = m_ShipWeightMultiplier,
			m_AmbulanceImportServiceFee = m_AmbulanceImportServiceFee,
			m_HearseImportServiceFee = m_HearseImportServiceFee,
			m_FireEngineImportServiceFee = m_FireEngineImportServiceFee,
			m_GarbageImportServiceFee = m_GarbageImportServiceFee,
			m_PoliceImportServiceFee = m_PoliceImportServiceFee,
			m_OCServiceTradePopulationRange = m_OCServiceTradePopulationRange
		});
	}
}
