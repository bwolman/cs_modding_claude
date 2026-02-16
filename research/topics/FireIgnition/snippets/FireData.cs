using Unity.Entities;

namespace Game.Prefabs;

public struct FireData : IComponentData, IQueryTypeParameter
{
	public EventTargetType m_RandomTargetType;

	public float m_StartProbability;

	public float m_StartIntensity;

	public float m_EscalationRate;

	public float m_SpreadProbability;

	public float m_SpreadRange;
}
