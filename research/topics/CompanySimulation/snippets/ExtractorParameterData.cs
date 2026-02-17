using Unity.Entities;

namespace Game.Prefabs;

public struct ExtractorParameterData : IComponentData, IQueryTypeParameter
{
	public float m_FertilityConsumption;

	public float m_FishConsumption;

	public float m_OreConsumption;

	public float m_ForestConsumption;

	public float m_OilConsumption;

	public float m_FullFertility;

	public float m_FullFish;

	public float m_FullOre;

	public float m_FullOil;
}
