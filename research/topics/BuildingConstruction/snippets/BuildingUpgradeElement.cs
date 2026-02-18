using System;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct BuildingUpgradeElement : IBufferElementData, IEquatable<BuildingUpgradeElement>
{
	public Entity m_Upgrade;

	public BuildingUpgradeElement(Entity upgrade)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		m_Upgrade = upgrade;
	}

	public bool Equals(BuildingUpgradeElement other)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return ((Entity)(ref m_Upgrade)).Equals(other.m_Upgrade);
	}

	public override int GetHashCode()
	{
		return ((object)System.Runtime.CompilerServices.Unsafe.As<Entity, Entity>(ref m_Upgrade)/*cast due to .constrained prefix*/).GetHashCode();
	}
}
