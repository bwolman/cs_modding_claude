using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct WaterPipeEdge : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Index;

	public Entity m_Start;

	public Entity m_End;

	public int m_FreshFlow;

	public float m_FreshPollution;

	public int m_SewageFlow;

	public int m_FreshCapacity;

	public int m_SewageCapacity;

	public WaterPipeEdgeFlags m_Flags;

	public int2 flow => new int2(m_FreshFlow, m_SewageFlow);

	public int2 capacity => new int2(m_FreshCapacity, m_SewageCapacity);

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity start = m_Start;
		writer.Write(start);
		Entity end = m_End;
		writer.Write(end);
		int freshFlow = m_FreshFlow;
		writer.Write(freshFlow);
		float freshPollution = m_FreshPollution;
		writer.Write(freshPollution);
		int sewageFlow = m_SewageFlow;
		writer.Write(sewageFlow);
		int freshCapacity = m_FreshCapacity;
		writer.Write(freshCapacity);
		int sewageCapacity = m_SewageCapacity;
		writer.Write(sewageCapacity);
		WaterPipeEdgeFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity start = ref m_Start;
		reader.Read(out start);
		ref Entity end = ref m_End;
		reader.Read(out end);
		ref int freshFlow = ref m_FreshFlow;
		reader.Read(out freshFlow);
		if (reader.context.version >= Version.waterPipePollution)
		{
			ref float freshPollution = ref m_FreshPollution;
			reader.Read(out freshPollution);
		}
		else
		{
			reader.Read(out int _);
		}
		ref int sewageFlow = ref m_SewageFlow;
		reader.Read(out sewageFlow);
		ref int freshCapacity = ref m_FreshCapacity;
		reader.Read(out freshCapacity);
		ref int sewageCapacity = ref m_SewageCapacity;
		reader.Read(out sewageCapacity);
		if (reader.context.version >= Version.stormWater && reader.context.version < Version.waterPipeFlowSim)
		{
			reader.Read(out int _);
			reader.Read(out int _);
		}
		if (reader.context.version >= Version.waterPipeFlags)
		{
			reader.Read(out byte value4);
			m_Flags = (WaterPipeEdgeFlags)value4;
		}
	}
}
