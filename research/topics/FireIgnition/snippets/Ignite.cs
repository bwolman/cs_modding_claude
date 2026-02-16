using Unity.Entities;

namespace Game.Events;

public struct Ignite : IComponentData, IQueryTypeParameter
{
	public Entity m_Event;

	public Entity m_Target;

	public float m_Intensity;

	public uint m_RequestFrame;
}
