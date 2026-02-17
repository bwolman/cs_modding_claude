using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct AttractivenessParameterData : IComponentData, IQueryTypeParameter
{
	public float m_ForestEffect;

	public float m_ForestDistance;

	public float m_ShoreEffect;

	public float m_ShoreDistance;

	public float3 m_HeightBonus;

	public float2 m_AttractiveTemperature;

	public float2 m_ExtremeTemperature;

	public float2 m_TemperatureAffect;

	public float2 m_RainEffectRange;

	public float2 m_SnowEffectRange;

	public float3 m_SnowRainExtremeAffect;
}
