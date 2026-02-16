// Source: Game.dll -> Game.Net.ConnectedEdge (decompiled with ilspycmd v9.1)
using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(4)]
public struct ConnectedEdge : IBufferElementData, IEquatable<ConnectedEdge>, IEmptySerializable
{
    public Entity m_Edge;

    public ConnectedEdge(Entity edge)
    {
        m_Edge = edge;
    }

    public bool Equals(ConnectedEdge other)
    {
        return m_Edge.Equals(other.m_Edge);
    }

    public override int GetHashCode()
    {
        return m_Edge.GetHashCode();
    }
}
