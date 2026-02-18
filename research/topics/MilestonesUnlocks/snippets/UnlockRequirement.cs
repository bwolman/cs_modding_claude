using System;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct UnlockRequirement : IBufferElementData, IEquatable<UnlockRequirement>
{
	public Entity m_Prefab;

	public UnlockFlags m_Flags;

	public UnlockRequirement(Entity prefab, UnlockFlags flags)
	{
		m_Prefab = prefab;
		m_Flags = flags;
	}

	public bool Equals(UnlockRequirement other)
	{
		if (m_Prefab.Equals(other.m_Prefab))
		{
			return m_Flags == other.m_Flags;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is UnlockRequirement other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (m_Prefab.GetHashCode() * 397) ^ (int)m_Flags;
	}
}
