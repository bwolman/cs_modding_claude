using Game.Citizens;
using Unity.Entities;

namespace Game.Events;

public struct AddCriminal : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public CriminalFlags m_Flags;
}
