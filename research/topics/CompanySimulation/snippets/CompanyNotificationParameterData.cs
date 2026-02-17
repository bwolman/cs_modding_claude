using Unity.Entities;

namespace Game.Prefabs;

public struct CompanyNotificationParameterData : IComponentData, IQueryTypeParameter
{
	public Entity m_NoInputsNotificationPrefab;

	public Entity m_NoCustomersNotificationPrefab;

	public float m_NoInputCostLimit;

	public float m_NoCustomersServiceLimit;

	public float m_NoCustomersHotelLimit;
}
