using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct LaneFlow : IComponentData, IQueryTypeParameter, ISerializable
{
	public float4 m_Duration;

	public float4 m_Distance;

	public float2 m_Next;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float4 duration = m_Duration;
		writer.Write(duration);
		float4 distance = m_Distance;
		writer.Write(distance);
		float2 next = m_Next;
		writer.Write(next);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float4 duration = ref m_Duration;
		reader.Read(out duration);
		ref float4 distance = ref m_Distance;
		reader.Read(out distance);
		ref float2 next = ref m_Next;
		reader.Read(out next);
		if (reader.context.version < Version.trafficFlowFixes)
		{
			float4 @float = m_Duration + 1f;
			m_Distance *= 0.01f;
			m_Duration = math.select(m_Distance / @float, 0f, @float <= 0f);
			float num = m_Next.x + m_Next.y;
			m_Next.x = math.select(m_Next.y / num, 0f, num <= 0f);
			m_Next.y *= 0.01f;
			m_Next.x *= m_Next.y;
		}
	}
}
