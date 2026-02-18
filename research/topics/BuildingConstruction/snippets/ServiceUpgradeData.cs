using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ServiceUpgradeData : IComponentData, IQueryTypeParameter, ISerializable
{
	public uint m_UpgradeCost;

	public int m_XPReward;

	public int m_MaxPlacementOffset;

	public float m_MaxPlacementDistance;

	public bool m_ForbidMultiple;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint upgradeCost = m_UpgradeCost;
		writer.Write(upgradeCost);
		int xPReward = m_XPReward;
		writer.Write(xPReward);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint upgradeCost = ref m_UpgradeCost;
		reader.Read(out upgradeCost);
		ref int xPReward = ref m_XPReward;
		reader.Read(out xPReward);
	}
}
