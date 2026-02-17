using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct CarLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_AccessRestriction;

	public CarLaneFlags m_Flags;

	public float m_DefaultSpeedLimit;

	public float m_SpeedLimit;

	public float m_Curviness;

	public ushort m_CarriagewayGroup;

	public byte m_BlockageStart;

	public byte m_BlockageEnd;

	public byte m_CautionStart;

	public byte m_CautionEnd;

	public byte m_FlowOffset;

	public byte m_LaneCrossCount;

	public Bounds1 blockageBounds => new Bounds1((float)(int)m_BlockageStart * 0.003921569f, (float)(int)m_BlockageEnd * 0.003921569f);

	public Bounds1 cautionBounds => new Bounds1((float)(int)m_CautionStart * 0.003921569f, (float)(int)m_CautionEnd * 0.003921569f);

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity accessRestriction = m_AccessRestriction;
		writer.Write(accessRestriction);
		CarLaneFlags flags = m_Flags;
		writer.Write((uint)flags);
		float defaultSpeedLimit = m_DefaultSpeedLimit;
		writer.Write(defaultSpeedLimit);
		float speedLimit = m_SpeedLimit;
		writer.Write(speedLimit);
		float curviness = m_Curviness;
		writer.Write(curviness);
		ushort carriagewayGroup = m_CarriagewayGroup;
		writer.Write(carriagewayGroup);
		byte blockageStart = m_BlockageStart;
		writer.Write(blockageStart);
		byte blockageEnd = m_BlockageEnd;
		writer.Write(blockageEnd);
		byte cautionStart = m_CautionStart;
		writer.Write(cautionStart);
		byte cautionEnd = m_CautionEnd;
		writer.Write(cautionEnd);
		byte flowOffset = m_FlowOffset;
		writer.Write(flowOffset);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.pathfindAccessRestriction)
		{
			ref Entity accessRestriction = ref m_AccessRestriction;
			reader.Read(out accessRestriction);
		}
		reader.Read(out uint value);
		ref float defaultSpeedLimit = ref m_DefaultSpeedLimit;
		reader.Read(out defaultSpeedLimit);
		if (reader.context.version >= Version.modifiedSpeedLimit)
		{
			ref float speedLimit = ref m_SpeedLimit;
			reader.Read(out speedLimit);
		}
		else
		{
			m_SpeedLimit = m_DefaultSpeedLimit;
		}
		ref float curviness = ref m_Curviness;
		reader.Read(out curviness);
		ref ushort carriagewayGroup = ref m_CarriagewayGroup;
		reader.Read(out carriagewayGroup);
		if (reader.context.version >= Version.carLaneBlockage)
		{
			ref byte blockageStart = ref m_BlockageStart;
			reader.Read(out blockageStart);
			ref byte blockageEnd = ref m_BlockageEnd;
			reader.Read(out blockageEnd);
		}
		else
		{
			m_BlockageStart = byte.MaxValue;
			m_BlockageEnd = 0;
		}
		if (reader.context.version >= Version.trafficImprovements)
		{
			ref byte cautionStart = ref m_CautionStart;
			reader.Read(out cautionStart);
			ref byte cautionEnd = ref m_CautionEnd;
			reader.Read(out cautionEnd);
		}
		else
		{
			m_CautionStart = byte.MaxValue;
			m_CautionEnd = 0;
		}
		if (reader.context.version >= Version.pathfindImprovement)
		{
			ref byte flowOffset = ref m_FlowOffset;
			reader.Read(out flowOffset);
		}
		m_Flags = (CarLaneFlags)value;
		if (reader.context.version < Version.roadSideConnectionImprovements)
		{
			if ((m_Flags & CarLaneFlags.Unsafe) != 0)
			{
				m_Flags |= CarLaneFlags.SideConnection;
			}
			else
			{
				m_Flags &= ~CarLaneFlags.SideConnection;
			}
		}
	}
}
