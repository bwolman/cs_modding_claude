using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", true, new System.Type[] { })]
public class LandValuePrefab : PrefabBase
{
	public InfoviewPrefab m_LandValueInfoViewPrefab;

	[Tooltip("This is the baseline of land value, land value less or equal this won't be showed with gizmos")]
	public float m_LandValueBaseline = 10f;

	[Tooltip("This is the multiplier to the health service coverage bonus")]
	public float m_HealthCoverageBonusMultiplier = 5f;

	[Tooltip("This is the multiplier to the education service coverage bonus")]
	public float m_EducationCoverageBonusMultiplier = 5f;

	[Tooltip("This is the multiplier to the police service coverage bonus")]
	public float m_PoliceCoverageBonusMultiplier = 5f;

	[Tooltip("This is the multiplier to both terrain attractiveness and tourism building attractiveness bonus")]
	public float m_AttractivenessBonusMultiplier = 3f;

	[Tooltip("This is the multiplier to the telecom coverage bonus")]
	public float m_TelecomCoverageBonusMultiplier = 20f;

	[Tooltip("This is the multiplier to the commercial service bonus")]
	public float m_CommercialServiceBonusMultiplier = 10f;

	[Tooltip("This is the multiplier to the bus transportation bonus")]
	public float m_BusBonusMultiplier = 5f;

	[Tooltip("This is the multiplier to the tram or Subway transportation bonus")]
	public float m_TramSubwayBonusMultiplier = 50f;

	[Tooltip("This is the max bonus money a common factor can contribute")]
	public int m_CommonFactorMaxBonus = 100;

	[Tooltip("This is the multiplier to the ground pollution penalty")]
	public float m_GroundPollutionPenaltyMultiplier = 10f;

	[Tooltip("This is the multiplier to the air pollution penalty")]
	public float m_AirPollutionPenaltyMultiplier = 5f;

	[Tooltip("This is the multiplier to the noise pollution penalty")]
	public float m_NoisePollutionPenaltyMultiplier = 0.01f;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add((PrefabBase)m_LandValueInfoViewPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<LandValueParameterData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = ((EntityManager)(ref entityManager)).World.GetOrCreateSystemManaged<PrefabSystem>();
		LandValueParameterData landValueParameterData = new LandValueParameterData
		{
			m_LandValueInfoViewPrefab = orCreateSystemManaged.GetEntity(m_LandValueInfoViewPrefab),
			m_LandValueBaseline = m_LandValueBaseline,
			m_HealthCoverageBonusMultiplier = m_HealthCoverageBonusMultiplier,
			m_EducationCoverageBonusMultiplier = m_EducationCoverageBonusMultiplier,
			m_PoliceCoverageBonusMultiplier = m_PoliceCoverageBonusMultiplier,
			m_AttractivenessBonusMultiplier = m_AttractivenessBonusMultiplier,
			m_TelecomCoverageBonusMultiplier = m_TelecomCoverageBonusMultiplier,
			m_CommercialServiceBonusMultiplier = m_CommercialServiceBonusMultiplier,
			m_BusBonusMultiplier = m_BusBonusMultiplier,
			m_TramSubwayBonusMultiplier = m_TramSubwayBonusMultiplier,
			m_CommonFactorMaxBonus = m_CommonFactorMaxBonus,
			m_GroundPollutionPenaltyMultiplier = m_GroundPollutionPenaltyMultiplier,
			m_AirPollutionPenaltyMultiplier = m_AirPollutionPenaltyMultiplier,
			m_NoisePollutionPenaltyMultiplier = m_NoisePollutionPenaltyMultiplier
		};
		((EntityManager)(ref entityManager)).SetComponentData<LandValueParameterData>(entity, landValueParameterData);
	}
}
