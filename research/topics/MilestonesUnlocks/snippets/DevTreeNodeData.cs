using Unity.Entities;

namespace Game.Prefabs;

public struct DevTreeNodeData : IComponentData, IQueryTypeParameter
{
	public int m_Cost;

	public Entity m_Service;
}
