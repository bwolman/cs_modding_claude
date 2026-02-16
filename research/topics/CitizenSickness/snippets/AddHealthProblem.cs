using Game.Citizens;
using Unity.Entities;

namespace Game.Events;

public struct AddHealthProblem : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public HealthProblemFlags m_Flags;
}
