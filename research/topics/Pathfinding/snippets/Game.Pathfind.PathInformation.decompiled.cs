using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Pathfind;

public struct PathInformation : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Origin;

	public Entity m_Destination;

	public float m_Distance;

	public float m_Duration;

	public float m_TotalCost;

	public PathMethod m_Methods;

	public PathFlags m_State;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity origin = m_Origin;
		writer.Write(origin);
		Entity destination = m_Destination;
		writer.Write(destination);
		float distance = m_Distance;
		writer.Write(distance);
		float duration = m_Duration;
		writer.Write(duration);
		float totalCost = m_TotalCost;
		writer.Write(totalCost);
		PathMethod methods = m_Methods;
		writer.Write((ushort)methods);
		PathFlags state = m_State;
		writer.Write((ushort)state);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity origin = ref m_Origin;
		reader.Read(out origin);
		ref Entity destination = ref m_Destination;
		reader.Read(out destination);
		ref float distance = ref m_Distance;
		reader.Read(out distance);
		ref float duration = ref m_Duration;
		reader.Read(out duration);
		if (reader.context.version >= Version.totalPathfindCost)
		{
			ref float totalCost = ref m_TotalCost;
			reader.Read(out totalCost);
		}
		if (reader.context.version >= Version.usedPathfindMethods)
		{
			reader.Read(out ushort value);
			m_Methods = (PathMethod)value;
		}
		if (reader.context.version >= Version.pathfindState)
		{
			reader.Read(out ushort value2);
			m_State = (PathFlags)value2;
		}
		if ((m_State & PathFlags.Pending) != 0)
		{
			m_State &= ~PathFlags.Pending;
			m_State |= PathFlags.Obsolete;
		}
	}
}
