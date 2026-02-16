using Unity.Entities;

namespace Game.Events;

public struct AddAccidentSite : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public AccidentSiteFlags m_Flags;
}
