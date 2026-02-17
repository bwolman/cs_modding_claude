using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct ResourceData : IComponentData, IQueryTypeParameter
{
	public float2 m_Price;

	public bool m_IsProduceable;

	public bool m_IsTradable;

	public bool m_IsMaterial;

	public bool m_IsLeisure;

	public float m_Weight;

	public float m_WealthModifier;

	public float m_BaseConsumption;

	public int m_ChildWeight;

	public int m_TeenWeight;

	public int m_AdultWeight;

	public int m_ElderlyWeight;

	public int m_CarConsumption;

	public bool m_RequireTemperature;

	public float m_RequiredTemperature;

	public bool m_RequireNaturalResource;

	public int2 m_NeededWorkPerUnit;
}
