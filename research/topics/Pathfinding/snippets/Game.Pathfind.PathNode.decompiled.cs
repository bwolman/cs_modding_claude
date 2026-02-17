using System;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

public struct PathNode : IEquatable<PathNode>, IStrideSerializable, ISerializable
{
	private const float FLOAT_TO_INT = 32767f;

	private const float INT_TO_FLOAT = 3.051851E-05f;

	private const ulong CURVEPOS_INCLUDE = 2147418112uL;

	private const ulong CURVEPOS_EXCLUDE = 18446744071562133503uL;

	private const ulong SECONDARY_NODE = 2147483648uL;

	private ulong m_SearchKey;

	public PathNode(Entity owner, byte laneIndex, byte segmentIndex)
	{
		m_SearchKey = (ulong)((long)owner.Index << 32) | ((ulong)segmentIndex << 8) | laneIndex;
	}

	public PathNode(Entity owner, byte laneIndex, byte segmentIndex, float curvePosition)
	{
		m_SearchKey = (ulong)((long)owner.Index << 32) | ((ulong)(curvePosition * 32767f) << 16) | ((ulong)segmentIndex << 8) | laneIndex;
	}

	public PathNode(Entity owner, ushort laneIndex, float curvePosition)
	{
		m_SearchKey = (ulong)((long)owner.Index << 32) | ((ulong)(curvePosition * 32767f) << 16) | laneIndex;
	}

	public PathNode(Entity owner, ushort laneIndex)
	{
		m_SearchKey = (ulong)(((long)owner.Index << 32) | laneIndex);
	}

	public PathNode(PathTarget pathTarget)
	{
		m_SearchKey = (ulong)((long)pathTarget.m_Entity.Index << 32) | ((ulong)(pathTarget.m_Delta * 32767f) << 16);
	}

	public PathNode(PathNode pathNode, float curvePosition)
	{
		m_SearchKey = (pathNode.m_SearchKey & 0xFFFFFFFF8000FFFFuL) | ((ulong)(curvePosition * 32767f) << 16);
	}

	public PathNode(PathNode pathNode, bool secondaryNode)
	{
		m_SearchKey = math.select(pathNode.m_SearchKey & 0xFFFFFFFF7FFFFFFFuL, pathNode.m_SearchKey | 0x80000000u, secondaryNode);
	}

	public bool IsSecondary()
	{
		return (m_SearchKey & 0x80000000u) != 0;
	}

	public bool Equals(PathNode other)
	{
		return m_SearchKey == other.m_SearchKey;
	}

	public bool EqualsIgnoreCurvePos(PathNode other)
	{
		return ((m_SearchKey ^ other.m_SearchKey) & 0xFFFFFFFF8000FFFFuL) == 0;
	}

	public bool OwnerEquals(PathNode other)
	{
		return (uint)(m_SearchKey >> 32) == (uint)(other.m_SearchKey >> 32);
	}

	public override int GetHashCode()
	{
		return m_SearchKey.GetHashCode();
	}

	public PathNode StripCurvePos()
	{
		return new PathNode
		{
			m_SearchKey = (m_SearchKey & 0xFFFFFFFF8000FFFFuL)
		};
	}

	public void ReplaceOwner(Entity oldOwner, Entity newOwner)
	{
		if ((int)(m_SearchKey >> 32) == oldOwner.Index)
		{
			m_SearchKey = (ulong)((long)newOwner.Index << 32) | (m_SearchKey & 0xFFFFFFFFu);
		}
	}

	public void SetOwner(Entity newOwner)
	{
		m_SearchKey = (ulong)((long)newOwner.Index << 32) | (m_SearchKey & 0xFFFFFFFFu);
	}

	public void SetSegmentIndex(byte segmentIndex)
	{
		m_SearchKey = ((ulong)segmentIndex << 8) | (m_SearchKey & 0xFFFFFFFFFFFF00FFuL);
	}

	public float GetCurvePos()
	{
		return (float)((m_SearchKey & 0x7FFF0000) >> 16) * 3.051851E-05f;
	}

	public int GetOwnerIndex()
	{
		return (int)(m_SearchKey >> 32);
	}

	public ushort GetLaneIndex()
	{
		return (ushort)(m_SearchKey & 0xFFFF);
	}

	public bool GetOrder(PathNode other)
	{
		return other.m_SearchKey < m_SearchKey;
	}

	public int GetCurvePosOrder(PathNode other)
	{
		return (int)(m_SearchKey & 0x7FFF0000) - (int)(other.m_SearchKey & 0x7FFF0000);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = new Entity
		{
			Index = (int)(m_SearchKey >> 32)
		};
		writer.Write(value, ignoreVersion: true);
		int value2 = (int)m_SearchKey;
		writer.Write((uint)value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out Entity value);
		reader.Read(out uint value2);
		m_SearchKey = (ulong)(((long)value.Index << 32) | value2);
	}

	public int GetStride(Context context)
	{
		return 8;
	}
}
