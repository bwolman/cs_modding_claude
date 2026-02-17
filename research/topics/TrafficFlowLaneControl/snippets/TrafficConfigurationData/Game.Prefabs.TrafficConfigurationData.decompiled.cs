using Unity.Entities;

namespace Game.Prefabs;

public struct TrafficConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_BottleneckNotification;

	public Entity m_DeadEndNotification;

	public Entity m_RoadConnectionNotification;

	public Entity m_TrackConnectionNotification;

	public Entity m_CarConnectionNotification;

	public Entity m_ShipConnectionNotification;

	public Entity m_TrainConnectionNotification;

	public Entity m_PedestrianConnectionNotification;

	public Entity m_BicycleConnectionNotification;
}
