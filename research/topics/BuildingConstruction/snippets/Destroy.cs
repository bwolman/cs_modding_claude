using Unity.Entities;

namespace Game.Objects;

public struct Destroy : IComponentData, IQueryTypeParameter
{
	public Entity m_Object;

	public Entity m_Event;

	public Destroy(Entity _object, Entity _event)
	{
		m_Object = _object;
		m_Event = _event;
	}
}
