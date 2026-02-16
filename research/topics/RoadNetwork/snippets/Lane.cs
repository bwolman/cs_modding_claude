// Source: Game.dll -> Game.Net.Lane (decompiled with ilspycmd v9.1)
using System;
using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;

namespace Game.Net;

public struct Lane : IComponentData, IQueryTypeParameter, IEquatable<Lane>, IStrideSerializable, ISerializable
{
    public PathNode m_StartNode;

    public PathNode m_MiddleNode;

    public PathNode m_EndNode;

    public bool Equals(Lane other)
    {
        if (m_StartNode.Equals(other.m_StartNode) && m_MiddleNode.Equals(other.m_MiddleNode))
        {
            return m_EndNode.Equals(other.m_EndNode);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return m_MiddleNode.GetHashCode();
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        PathNode startNode = m_StartNode;
        writer.Write(startNode);
        PathNode middleNode = m_MiddleNode;
        writer.Write(middleNode);
        PathNode endNode = m_EndNode;
        writer.Write(endNode);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        ref PathNode startNode = ref m_StartNode;
        reader.Read(out startNode);
        ref PathNode middleNode = ref m_MiddleNode;
        reader.Read(out middleNode);
        ref PathNode endNode = ref m_EndNode;
        reader.Read(out endNode);
    }

    public int GetStride(Context context)
    {
        return m_StartNode.GetStride(context) * 3;
    }
}
