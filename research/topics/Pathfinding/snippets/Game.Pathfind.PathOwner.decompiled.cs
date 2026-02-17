using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Pathfind;

public struct PathOwner : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_ElementIndex;

	public PathFlags m_State;

	public PathOwner(PathFlags state)
	{
		m_ElementIndex = 0;
		m_State = state;
	}

	public PathOwner(int elementIndex, PathFlags state)
	{
		m_ElementIndex = elementIndex;
		m_State = state;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int elementIndex = m_ElementIndex;
		writer.Write(elementIndex);
		PathFlags state = m_State;
		writer.Write((ushort)state);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int elementIndex = ref m_ElementIndex;
		reader.Read(out elementIndex);
		reader.Read(out ushort value);
		m_State = (PathFlags)value;
		if ((m_State & PathFlags.Pending) != 0)
		{
			m_State &= ~PathFlags.Pending;
			m_State |= PathFlags.Obsolete;
		}
	}
}
