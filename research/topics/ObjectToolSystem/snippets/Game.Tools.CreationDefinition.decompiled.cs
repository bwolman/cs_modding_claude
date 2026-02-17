using Unity.Entities;

namespace Game.Tools;

public struct CreationDefinition : IComponentData, IQueryTypeParameter
{
	public Entity m_Prefab;

	public Entity m_SubPrefab;

	public Entity m_Original;

	public Entity m_Owner;

	public Entity m_Attached;

	public CreationFlags m_Flags;

	public int m_RandomSeed;
}
