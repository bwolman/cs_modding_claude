using Unity.Entities;
using Unity.Mathematics;

namespace Game.Events;

public struct Impact : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public float3 m_VelocityDelta;

	public float3 m_AngularVelocityDelta;

	public float m_Severity;

	public bool m_CheckStoppedEvent;
}
