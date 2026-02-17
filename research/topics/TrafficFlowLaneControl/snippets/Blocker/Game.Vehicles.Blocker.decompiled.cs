using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct Blocker : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Blocker;

	public BlockerType m_Type;

	public byte m_MaxSpeed;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity blocker = m_Blocker;
		writer.Write(blocker);
		BlockerType type = m_Type;
		writer.Write((byte)type);
		byte maxSpeed = m_MaxSpeed;
		writer.Write(maxSpeed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity blocker = ref m_Blocker;
		reader.Read(out blocker);
		if (reader.context.version >= Version.trafficBottlenecks)
		{
			reader.Read(out byte value);
			ref byte maxSpeed = ref m_MaxSpeed;
			reader.Read(out maxSpeed);
			m_Type = (BlockerType)value;
		}
		else
		{
			reader.Read(out float value2);
			m_MaxSpeed = (byte)math.clamp(value2 * 5f, 0f, 255f);
			m_Type = BlockerType.None;
		}
	}
}
