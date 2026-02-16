using Unity.Entities;

namespace Game.Zones;

public struct ProcessEstimate : IBufferElementData
{
	public float m_ProductionPerCell;

	public float m_BaseProfitabilityPerCell;

	public float m_WorkerProductionPerCell;

	public float m_LowEducationWeight;

	public Entity m_ProcessEntity;
}
