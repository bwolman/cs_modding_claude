using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct CarCurrentLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Lane;

	public Entity m_ChangeLane;

	public float3 m_CurvePosition;

	public CarLaneFlags m_LaneFlags;

	public float m_ChangeProgress;

	public float m_Duration;

	public float m_Distance;

	public float m_LanePosition;

	public CarCurrentLane(ParkedCar parkedCar, CarLaneFlags flags)
	{
		m_Lane = parkedCar.m_Lane;
		m_ChangeLane = Entity.Null;
		m_CurvePosition = parkedCar.m_CurvePosition;
		m_LaneFlags = flags;
		m_ChangeProgress = 0f;
		m_Duration = 0f;
		m_Distance = 0f;
		m_LanePosition = 0f;
	}

	public CarCurrentLane(PathElement pathElement, CarLaneFlags flags)
	{
		m_Lane = pathElement.m_Target;
		m_ChangeLane = Entity.Null;
		m_CurvePosition = pathElement.m_TargetDelta.xxx;
		m_LaneFlags = flags;
		m_ChangeProgress = 0f;
		m_Duration = 0f;
		m_Distance = 0f;
		m_LanePosition = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lane = m_Lane;
		writer.Write(lane);
		Entity changeLane = m_ChangeLane;
		writer.Write(changeLane);
		float3 curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		CarLaneFlags laneFlags = m_LaneFlags;
		writer.Write((uint)laneFlags);
		float changeProgress = m_ChangeProgress;
		writer.Write(changeProgress);
		float duration = m_Duration;
		writer.Write(duration);
		float distance = m_Distance;
		writer.Write(distance);
		float lanePosition = m_LanePosition;
		writer.Write(lanePosition);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lane = ref m_Lane;
		reader.Read(out lane);
		ref Entity changeLane = ref m_ChangeLane;
		reader.Read(out changeLane);
		ref float3 curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		reader.Read(out uint value);
		ref float changeProgress = ref m_ChangeProgress;
		reader.Read(out changeProgress);
		ref float duration = ref m_Duration;
		reader.Read(out duration);
		ref float distance = ref m_Distance;
		reader.Read(out distance);
		if (reader.context.version >= Version.lanePosition)
		{
			ref float lanePosition = ref m_LanePosition;
			reader.Read(out lanePosition);
		}
		m_LaneFlags = (CarLaneFlags)value;
	}
}
