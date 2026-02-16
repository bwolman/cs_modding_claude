using Unity.Entities;

namespace Game.Prefabs;

public struct PoliceConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_PoliceServicePrefab;

	public Entity m_TrafficAccidentNotificationPrefab;

	public Entity m_CrimeSceneNotificationPrefab;

	public float m_MaxCrimeAccumulation;

	public float m_CrimeAccumulationTolerance;

	public int m_HomeCrimeEffect;

	public int m_WorkplaceCrimeEffect;

	public float m_WelfareCrimeRecurrenceFactor;

	public float m_CrimePoliceCoverageFactor;

	public float m_CrimePopulationReduction;
}
