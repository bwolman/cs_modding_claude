using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct CrimeData : IComponentData, IQueryTypeParameter
{
	public EventTargetType m_RandomTargetType;

	public CrimeType m_CrimeType;

	public Bounds1 m_OccurenceProbability;

	public Bounds1 m_RecurrenceProbability;

	public Bounds1 m_AlarmDelay;

	public Bounds1 m_CrimeDuration;

	public Bounds1 m_CrimeIncomeAbsolute;

	public Bounds1 m_CrimeIncomeRelative;

	public Bounds1 m_JailTimeRange;

	public Bounds1 m_PrisonTimeRange;

	public float m_PrisonProbability;
}
