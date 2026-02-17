using Game.Agents;
using Unity.Entities;

namespace Game.Prefabs;

public struct LeisureParametersData : IComponentData, IQueryTypeParameter
{
	public Entity m_TravelingPrefab;

	public Entity m_AttractionPrefab;

	public Entity m_SightseeingPrefab;

	public int m_LeisureRandomFactor;

	public int m_ChanceCitizenDecreaseLeisureCounter;

	public int m_ChanceTouristDecreaseLeisureCounter;

	public int m_AmountLeisureCounterDecrease;

	public int m_TouristLodgingConsumePerDay;

	public int m_TouristServiceConsumePerDay;

	public Entity GetPrefab(LeisureType type)
	{
		return type switch
		{
			LeisureType.Travel => m_TravelingPrefab, 
			LeisureType.Attractions => m_AttractionPrefab, 
			LeisureType.Sightseeing => m_SightseeingPrefab, 
			_ => default(Entity), 
		};
	}
}
