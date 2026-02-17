using Colossal.Serialization.Entities;
using Game.Economy;
using Game.Pathfind;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Companies;

public struct ResourceBuyer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Payer;

	public SetupTargetFlags m_Flags;

	public Resource m_ResourceNeeded;

	public int m_AmountNeeded;

	public float3 m_Location;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity payer = m_Payer;
		writer.Write(payer);
		byte value = (byte)m_Flags;
		writer.Write(value);
		sbyte value2 = (sbyte)EconomyUtils.GetResourceIndex(m_ResourceNeeded);
		writer.Write(value2);
		int amountNeeded = m_AmountNeeded;
		writer.Write(amountNeeded);
		float3 location = m_Location;
		writer.Write(location);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity payer = ref m_Payer;
		reader.Read(out payer);
		reader.Read(out byte value);
		reader.Read(out sbyte value2);
		ref int amountNeeded = ref m_AmountNeeded;
		reader.Read(out amountNeeded);
		ref float3 location = ref m_Location;
		reader.Read(out location);
		m_Flags = (SetupTargetFlags)value;
		m_ResourceNeeded = EconomyUtils.GetResource(value2);
	}
}
