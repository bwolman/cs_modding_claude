using Unity.Entities;

namespace Game.Prefabs;

public struct WorkProviderParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_UneducatedNotificationPrefab;

	public Entity m_EducatedNotificationPrefab;

	public short m_UneducatedNotificationDelay;

	public short m_EducatedNotificationDelay;

	public float m_UneducatedNotificationLimit;

	public float m_EducatedNotificationLimit;

	public int m_SeniorEmployeeLevel;
}
