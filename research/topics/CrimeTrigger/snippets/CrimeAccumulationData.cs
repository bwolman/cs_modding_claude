using Unity.Entities;

namespace Game.Prefabs;

public struct CrimeAccumulationData : IComponentData, IQueryTypeParameter
{
	public float m_CrimeRate;
}
