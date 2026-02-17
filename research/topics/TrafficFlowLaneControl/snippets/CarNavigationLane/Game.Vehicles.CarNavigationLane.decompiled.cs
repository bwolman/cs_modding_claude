using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

[InternalBufferCapacity(8)]
public struct CarNavigationLane : IBufferElementData, ISerializable
{
	public Entity m_Lane;

	public float2 m_CurvePosition;

	public CarLaneFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		float2 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		CarLaneFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref float2 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		reader.Read(out uint value);
		m_Flags = (CarLaneFlags)value;
	}
}
