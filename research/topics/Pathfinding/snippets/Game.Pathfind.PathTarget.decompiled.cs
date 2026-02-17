using Unity.Entities;

namespace Game.Pathfind;

public struct PathTarget
{
	public Entity m_Target;

	public Entity m_Entity;

	public float m_Delta;

	public float m_Cost;

	public EdgeFlags m_Flags;

	public PathTarget(Entity target, Entity entity, float delta, float cost)
	{
		m_Target = target;
		m_Entity = entity;
		m_Delta = delta;
		m_Cost = cost;
		m_Flags = EdgeFlags.DefaultMask;
	}

	public PathTarget(Entity target, Entity entity, float delta, float cost, EdgeFlags flags)
	{
		m_Target = target;
		m_Entity = entity;
		m_Delta = delta;
		m_Cost = cost;
		m_Flags = flags;
	}
}
