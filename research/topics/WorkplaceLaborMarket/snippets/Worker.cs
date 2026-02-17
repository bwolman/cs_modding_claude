using Colossal.Serialization.Entities;
using Game.Companies;
using Unity.Entities;

namespace Game.Citizens;

public struct Worker : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Workplace;

	public float m_LastCommuteTime;

	public byte m_Level;

	public Workshift m_Shift;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Entity workplace = m_Workplace;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(workplace);
		float lastCommuteTime = m_LastCommuteTime;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(lastCommuteTime);
		byte level = m_Level;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(level);
		Workshift shift = m_Shift;
		((IWriter)writer/*cast due to .constrained prefix*/).Write((byte)shift);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity workplace = ref m_Workplace;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref workplace);
		ref float lastCommuteTime = ref m_LastCommuteTime;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref lastCommuteTime);
		ref byte level = ref m_Level;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref level);
		byte shift = default(byte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref shift);
		m_Shift = (Workshift)shift;
	}
}
