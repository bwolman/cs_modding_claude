using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct HealthEventData : IComponentData, IQueryTypeParameter
{
	public EventTargetType m_RandomTargetType;

	public HealthEventType m_HealthEventType;

	public Bounds1 m_OccurenceProbability;

	public Bounds1 m_TransportProbability;

	public bool m_RequireTracking;
}
