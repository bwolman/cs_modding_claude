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
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		Entity payer = m_Payer;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(payer);
		byte num = (byte)m_Flags;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(num);
		sbyte num2 = (sbyte)EconomyUtils.GetResourceIndex(m_ResourceNeeded);
		((IWriter)writer/*cast due to .constrained prefix*/).Write(num2);
		int amountNeeded = m_AmountNeeded;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(amountNeeded);
		float3 location = m_Location;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(location);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity payer = ref m_Payer;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref payer);
		byte flags = default(byte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref flags);
		sbyte index = default(sbyte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref index);
		ref int amountNeeded = ref m_AmountNeeded;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref amountNeeded);
		ref float3 location = ref m_Location;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref location);
		m_Flags = (SetupTargetFlags)flags;
		m_ResourceNeeded = EconomyUtils.GetResource(index);
	}
}
