using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Zones;

[InternalBufferCapacity(4)]
public struct SubBlock : IBufferElementData, IEquatable<SubBlock>, IEmptySerializable
{
	public Entity m_SubBlock;

	public SubBlock(Entity block)
	{
		m_SubBlock = block;
	}

	public bool Equals(SubBlock other)
	{
		return m_SubBlock.Equals(other.m_SubBlock);
	}

	public override int GetHashCode()
	{
		return m_SubBlock.GetHashCode();
	}
}
