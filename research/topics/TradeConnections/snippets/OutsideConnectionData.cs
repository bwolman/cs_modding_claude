using Unity.Entities;

namespace Game.Prefabs;

public struct OutsideConnectionData : IComponentData, IQueryTypeParameter
{
	public OutsideConnectionTransferType m_Type;

	public float m_Remoteness;
}
