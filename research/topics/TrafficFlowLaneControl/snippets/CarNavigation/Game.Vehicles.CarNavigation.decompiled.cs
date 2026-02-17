using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct CarNavigation : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_TargetPosition;

	public quaternion m_TargetRotation;

	public float m_MaxSpeed;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 targetPosition = m_TargetPosition;
		writer.Write(targetPosition);
		quaternion targetRotation = m_TargetRotation;
		writer.Write(targetRotation);
		float maxSpeed = m_MaxSpeed;
		writer.Write(maxSpeed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 targetPosition = ref m_TargetPosition;
		reader.Read(out targetPosition);
		if (reader.context.version >= Version.parkingRotation)
		{
			ref quaternion targetRotation = ref m_TargetRotation;
			reader.Read(out targetRotation);
		}
		else
		{
			reader.Read(out float3 value);
			if (!value.Equals(default(float3)))
			{
				m_TargetRotation = quaternion.LookRotationSafe(value, math.up());
			}
		}
		ref float maxSpeed = ref m_MaxSpeed;
		reader.Read(out maxSpeed);
	}
}
