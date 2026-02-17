using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct LaneOverlap : IBufferElementData, IEmptySerializable, IComparable<LaneOverlap>
{
	public Entity m_Other;

	public OverlapFlags m_Flags;

	public byte m_ThisStart;

	public byte m_ThisEnd;

	public byte m_OtherStart;

	public byte m_OtherEnd;

	public byte m_Parallelism;

	public sbyte m_PriorityDelta;

	public LaneOverlap(Entity other, float4 overlap, OverlapFlags flags, float parallelism, int priorityDelta)
	{
		int4 @int = math.clamp((int4)math.round(overlap * 255f), 0, 255);
		m_Other = other;
		m_ThisStart = (byte)@int.x;
		m_ThisEnd = (byte)@int.y;
		m_OtherStart = (byte)@int.z;
		m_OtherEnd = (byte)@int.w;
		m_Flags = flags;
		m_Parallelism = (byte)math.clamp((int)math.round(parallelism * 128f), 0, 255);
		m_PriorityDelta = (sbyte)priorityDelta;
	}

	public int CompareTo(LaneOverlap other)
	{
		return ((m_ThisStart << 8) | m_ThisEnd) - ((other.m_ThisStart << 8) | other.m_ThisEnd);
	}
}
