using Colossal.Serialization.Entities;
using Game.Net;
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct PlaceableObjectData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_PlacementOffset;

	public uint m_ConstructionCost;

	public int m_XPReward;

	public byte m_DefaultProbability;

	public RotationSymmetry m_RotationSymmetry;

	public SubReplacementType m_SubReplacementType;

	public Game.Objects.PlacementFlags m_Flags;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 placementOffset = ref m_PlacementOffset;
		reader.Read(out placementOffset);
		ref uint constructionCost = ref m_ConstructionCost;
		reader.Read(out constructionCost);
		ref int xPReward = ref m_XPReward;
		reader.Read(out xPReward);
		reader.Read(out uint value);
		m_Flags = (Game.Objects.PlacementFlags)((int)value & -32769);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 placementOffset = m_PlacementOffset;
		writer.Write(placementOffset);
		uint constructionCost = m_ConstructionCost;
		writer.Write(constructionCost);
		int xPReward = m_XPReward;
		writer.Write(xPReward);
		Game.Objects.PlacementFlags flags = m_Flags;
		writer.Write((uint)flags);
	}
}
