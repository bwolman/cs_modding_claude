using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Attached : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Parent;

	public Entity m_OldParent;

	public float m_CurvePosition;

	public Attached(Entity parent, Entity oldParent, float curvePosition)
	{
		m_Parent = parent;
		m_OldParent = oldParent;
		m_CurvePosition = curvePosition;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity parent = m_Parent;
		writer.Write(parent);
		float curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity parent = ref m_Parent;
		reader.Read(out parent);
		ref float curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
	}
}
