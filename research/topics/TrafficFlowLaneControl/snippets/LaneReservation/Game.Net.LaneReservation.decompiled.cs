using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct LaneReservation : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Blocker;

	public ReservationData m_Next;

	public ReservationData m_Prev;

	public float GetOffset()
	{
		return (float)math.max((int)m_Next.m_Offset, (int)m_Prev.m_Offset) * 0.003921569f;
	}

	public int GetPriority()
	{
		return math.max((int)m_Next.m_Priority, (int)m_Prev.m_Priority);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity blocker = m_Blocker;
		writer.Write(blocker);
		byte offset = m_Next.m_Offset;
		writer.Write(offset);
		byte priority = m_Next.m_Priority;
		writer.Write(priority);
		byte offset2 = m_Prev.m_Offset;
		writer.Write(offset2);
		byte priority2 = m_Prev.m_Priority;
		writer.Write(priority2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.stuckTrainFix)
		{
			ref Entity blocker = ref m_Blocker;
			reader.Read(out blocker);
		}
		ref byte offset = ref m_Next.m_Offset;
		reader.Read(out offset);
		ref byte priority = ref m_Next.m_Priority;
		reader.Read(out priority);
		ref byte offset2 = ref m_Prev.m_Offset;
		reader.Read(out offset2);
		ref byte priority2 = ref m_Prev.m_Priority;
		reader.Read(out priority2);
	}
}
