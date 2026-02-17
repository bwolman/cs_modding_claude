using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct TrafficLights : IComponentData, IQueryTypeParameter, ISerializable
{
	public TrafficLightState m_State;

	public TrafficLightFlags m_Flags;

	public byte m_SignalGroupCount;

	public byte m_CurrentSignalGroup;

	public byte m_NextSignalGroup;

	public byte m_Timer;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		TrafficLightState state = m_State;
		writer.Write((byte)state);
		TrafficLightFlags flags = m_Flags;
		writer.Write((byte)flags);
		byte signalGroupCount = m_SignalGroupCount;
		writer.Write(signalGroupCount);
		byte currentSignalGroup = m_CurrentSignalGroup;
		writer.Write(currentSignalGroup);
		byte nextSignalGroup = m_NextSignalGroup;
		writer.Write(nextSignalGroup);
		byte timer = m_Timer;
		writer.Write(timer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out byte value2);
		ref byte signalGroupCount = ref m_SignalGroupCount;
		reader.Read(out signalGroupCount);
		ref byte currentSignalGroup = ref m_CurrentSignalGroup;
		reader.Read(out currentSignalGroup);
		if (reader.context.version >= Version.nextLaneSignal)
		{
			ref byte nextSignalGroup = ref m_NextSignalGroup;
			reader.Read(out nextSignalGroup);
		}
		ref byte timer = ref m_Timer;
		reader.Read(out timer);
		m_State = (TrafficLightState)value;
		m_Flags = (TrafficLightFlags)value2;
	}
}
