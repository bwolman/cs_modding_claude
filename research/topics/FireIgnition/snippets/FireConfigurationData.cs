using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct FireConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_FireNotificationPrefab;

	public Entity m_BurnedDownNotificationPrefab;

	public float m_DefaultStructuralIntegrity;

	public float m_BuildingStructuralIntegrity;

	public float m_StructuralIntegrityLevel1;

	public float m_StructuralIntegrityLevel2;

	public float m_StructuralIntegrityLevel3;

	public float m_StructuralIntegrityLevel4;

	public float m_StructuralIntegrityLevel5;

	public Bounds1 m_ResponseTimeRange;

	public float m_TelecomResponseTimeModifier;

	public float m_DarknessResponseTimeModifier;

	public float m_DeathRateOfFireAccident;
}
