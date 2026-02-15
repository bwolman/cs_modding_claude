using Unity.Entities;

namespace Game.Prefabs;

public struct TrafficAccidentData : IComponentData, IQueryTypeParameter
{
	public EventTargetType m_RandomSiteType;

	public EventTargetType m_SubjectType;

	public TrafficAccidentType m_AccidentType;

	public float m_OccurenceProbability;
}

public enum TrafficAccidentType
{
	LoseControl
}
