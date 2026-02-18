using System;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[InternalBufferCapacity(0)]
public struct InstalledUpgrade : IBufferElementData, IEquatable<InstalledUpgrade>, IEmptySerializable
{
	public Entity m_Upgrade;

	public uint m_OptionMask;

	public InstalledUpgrade(Entity upgrade, uint optionMask)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		m_Upgrade = upgrade;
		m_OptionMask = optionMask;
	}

	public bool Equals(InstalledUpgrade other)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return ((Entity)(ref m_Upgrade)).Equals(other.m_Upgrade);
	}

	public override int GetHashCode()
	{
		return ((object)System.Runtime.CompilerServices.Unsafe.As<Entity, Entity>(ref m_Upgrade)/*cast due to .constrained prefix*/).GetHashCode();
	}

	public static implicit operator Entity(InstalledUpgrade upgrade)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return upgrade.m_Upgrade;
	}
}
