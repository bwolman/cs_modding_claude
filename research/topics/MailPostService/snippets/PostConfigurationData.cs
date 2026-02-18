using Unity.Entities;

namespace Game.Prefabs;

public struct PostConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_PostServicePrefab;

	public int m_MaxMailAccumulation;

	public int m_MailAccumulationTolerance;

	public int m_OutgoingMailPercentage;
}
