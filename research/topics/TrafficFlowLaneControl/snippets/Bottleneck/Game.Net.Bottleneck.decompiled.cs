using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct Bottleneck : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_Position;

	public byte m_MinPos;

	public byte m_MaxPos;

	public byte m_Timer;

	public Bottleneck(byte minPos, byte maxPos, byte timer)
	{
		m_Position = (byte)(minPos + maxPos + 1 >> 1);
		m_MinPos = minPos;
		m_MaxPos = maxPos;
		m_Timer = timer;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte position = m_Position;
		writer.Write(position);
		byte minPos = m_MinPos;
		writer.Write(minPos);
		byte maxPos = m_MaxPos;
		writer.Write(maxPos);
		byte timer = m_Timer;
		writer.Write(timer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref byte position = ref m_Position;
		reader.Read(out position);
		if (reader.context.version >= Version.trafficBottleneckPosition)
		{
			ref byte minPos = ref m_MinPos;
			reader.Read(out minPos);
			ref byte maxPos = ref m_MaxPos;
			reader.Read(out maxPos);
		}
		else
		{
			m_MinPos = m_Position;
			m_MaxPos = m_Position;
		}
		ref byte timer = ref m_Timer;
		reader.Read(out timer);
	}
}
