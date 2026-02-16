// Source: Game.dll -> Game.Net.SubLane (decompiled with ilspycmd v9.1)
using System;
using Colossal.Serialization.Entities;
using Game.Pathfind;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(0)]
public struct SubLane : IBufferElementData, IEquatable<SubLane>, IEmptySerializable
{
    public Entity m_SubLane;

    public PathMethod m_PathMethods;

    public SubLane(Entity lane, PathMethod pathMethods)
    {
        m_SubLane = lane;
        m_PathMethods = pathMethods;
    }

    public bool Equals(SubLane other)
    {
        return m_SubLane.Equals(other.m_SubLane);
    }

    public override int GetHashCode()
    {
        return m_SubLane.GetHashCode();
    }
}
