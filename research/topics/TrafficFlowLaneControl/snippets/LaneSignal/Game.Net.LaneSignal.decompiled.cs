using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct LaneSignal : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Petitioner;

	public Entity m_Blocker;

	public ushort m_GroupMask;

	public sbyte m_Priority;

	public sbyte m_Default;

	public LaneSignalType m_Signal;

	public LaneSignalFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity petitioner = m_Petitioner;
		writer.Write(petitioner);
		Entity blocker = m_Blocker;
		writer.Write(blocker);
		ushort groupMask = m_GroupMask;
		writer.Write(groupMask);
		sbyte priority = m_Priority;
		writer.Write(priority);
		sbyte value = m_Default;
		writer.Write(value);
		LaneSignalType signal = m_Signal;
		writer.Write((byte)signal);
		LaneSignalFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.trafficImprovements)
		{
			ref Entity petitioner = ref m_Petitioner;
			reader.Read(out petitioner);
			ref Entity blocker = ref m_Blocker;
			reader.Read(out blocker);
		}
		ref ushort groupMask = ref m_GroupMask;
		reader.Read(out groupMask);
		ref sbyte priority = ref m_Priority;
		reader.Read(out priority);
		if (reader.context.version >= Version.levelCrossing)
		{
			ref sbyte value = ref m_Default;
			reader.Read(out value);
		}
		reader.Read(out byte value2);
		m_Signal = (LaneSignalType)value2;
		if (reader.context.version >= Version.trafficFlowFixes)
		{
			reader.Read(out byte value3);
			m_Flags = (LaneSignalFlags)value3;
		}
	}
}
