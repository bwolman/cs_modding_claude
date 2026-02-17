using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

public struct PathfindParameters
{
	public Entity m_ParkingTarget;

	public Entity m_Authorization1;

	public Entity m_Authorization2;

	public PathfindWeights m_Weights;

	public float2 m_MaxSpeed;

	public float2 m_WalkSpeed;

	public float2 m_ParkingSize;

	public float m_ParkingDelta;

	public float m_MaxCost;

	public int m_MaxResultCount;

	public PathMethod m_Methods;

	public PathfindFlags m_PathfindFlags;

	public RuleFlags m_IgnoredRules;

	public RuleFlags m_TaxiIgnoredRules;
}
