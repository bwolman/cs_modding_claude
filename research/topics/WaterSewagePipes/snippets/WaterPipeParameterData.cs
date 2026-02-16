using Unity.Entities;

namespace Game.Prefabs;

public struct WaterPipeParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_WaterService;

	public Entity m_WaterNotification;

	public Entity m_DirtyWaterNotification;

	public Entity m_SewageNotification;

	public Entity m_WaterPipeNotConnectedNotification;

	public Entity m_SewagePipeNotConnectedNotification;

	public Entity m_NotEnoughWaterCapacityNotification;

	public Entity m_NotEnoughSewageCapacityNotification;

	public Entity m_NotEnoughGroundwaterNotification;

	public Entity m_NotEnoughSurfaceWaterNotification;

	public Entity m_DirtyWaterPumpNotification;

	public float m_GroundwaterReplenish;

	public int m_GroundwaterPurification;

	public float m_GroundwaterUsageMultiplier;

	public float m_GroundwaterPumpEffectiveAmount;

	public float m_SurfaceWaterUsageMultiplier;

	public float m_SurfaceWaterPumpEffectiveDepth;

	public float m_MaxToleratedPollution;

	public int m_WaterPipePollutionSpreadInterval;

	public float m_StaleWaterPipePurification;
}
