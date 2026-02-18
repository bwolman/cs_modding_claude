using Unity.Entities;

namespace Game.Prefabs;

public struct MilestoneData : IComponentData, IQueryTypeParameter
{
	public int m_Index;

	public int m_Reward;

	public int m_DevTreePoints;

	public int m_MapTiles;

	public int m_LoanLimit;

	public int m_XpRequried;

	public bool m_Major;

	public bool m_IsVictory;
}
