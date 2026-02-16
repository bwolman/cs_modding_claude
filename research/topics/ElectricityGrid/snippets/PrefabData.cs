using Unity.Entities;

namespace Game.Prefabs;

public struct PowerPlantData : IComponentData, IQueryTypeParameter, ICombineData<PowerPlantData>
{
	public int m_ElectricityProduction;

	public void Combine(PowerPlantData otherData)
	{
		m_ElectricityProduction += otherData.m_ElectricityProduction;
	}
}

public struct BatteryData : IComponentData, IQueryTypeParameter, ICombineData<BatteryData>
{
	public int m_Capacity;
	public int m_PowerOutput;

	/// <summary>Capacity in simulation ticks = 85 * m_Capacity (kUpdatesPerHour).</summary>
	public long capacityTicks => 85 * m_Capacity;

	public void Combine(BatteryData otherData)
	{
		m_Capacity += otherData.m_Capacity;
		m_PowerOutput += otherData.m_PowerOutput;
	}
}

public struct WindPoweredData : IComponentData, IQueryTypeParameter, ICombineData<WindPoweredData>
{
	public float m_MaximumWind;
	public int m_Production;
}

public struct SolarPoweredData : IComponentData, IQueryTypeParameter, ICombineData<SolarPoweredData>
{
	public int m_Production;
}

public struct ElectricityParameterData : IComponentData, IQueryTypeParameter
{
	public float m_InitialBatteryCharge;
	public AnimationCurve1 m_TemperatureConsumptionMultiplier;
	public float m_CloudinessSolarPenalty;
	public Entity m_ElectricityServicePrefab;
	public Entity m_ElectricityNotificationPrefab;
	public Entity m_LowVoltageNotConnectedPrefab;
	public Entity m_HighVoltageNotConnectedPrefab;
	public Entity m_BottleneckNotificationPrefab;
	public Entity m_BuildingBottleneckNotificationPrefab;
	public Entity m_NotEnoughProductionNotificationPrefab;
	public Entity m_TransformerNotificationPrefab;
	public Entity m_NotEnoughConnectedNotificationPrefab;
	public Entity m_BatteryEmptyNotificationPrefab;
}

public struct ElectricityConnectionData : IComponentData, IQueryTypeParameter
{
	public int m_Capacity;
	public FlowDirection m_Direction;
	public ElectricityConnection.Voltage m_Voltage;
	public CompositionFlags m_CompositionAll;
	public CompositionFlags m_CompositionAny;
	public CompositionFlags m_CompositionNone;
}
