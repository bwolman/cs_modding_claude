using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct IndustrialProcessData : IComponentData, IQueryTypeParameter, ISerializable
{
	public ResourceStack m_Input1;

	public ResourceStack m_Input2;

	public ResourceStack m_Output;

	public int m_WorkPerUnit;

	public float m_MaxWorkersPerCell;

	public byte m_IsImport;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ResourceStack input = m_Input1;
		((IWriter)writer/*cast due to .constrained prefix*/).Write<ResourceStack>(input);
		ResourceStack input2 = m_Input2;
		((IWriter)writer/*cast due to .constrained prefix*/).Write<ResourceStack>(input2);
		ResourceStack output = m_Output;
		((IWriter)writer/*cast due to .constrained prefix*/).Write<ResourceStack>(output);
		int workPerUnit = m_WorkPerUnit;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(workPerUnit);
		float maxWorkersPerCell = m_MaxWorkersPerCell;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(maxWorkersPerCell);
		byte isImport = m_IsImport;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(isImport);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref ResourceStack input = ref m_Input1;
		((IReader)reader/*cast due to .constrained prefix*/).Read<ResourceStack>(ref input);
		ref ResourceStack input2 = ref m_Input2;
		((IReader)reader/*cast due to .constrained prefix*/).Read<ResourceStack>(ref input2);
		ref ResourceStack output = ref m_Output;
		((IReader)reader/*cast due to .constrained prefix*/).Read<ResourceStack>(ref output);
		ref int workPerUnit = ref m_WorkPerUnit;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref workPerUnit);
		ref float maxWorkersPerCell = ref m_MaxWorkersPerCell;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref maxWorkersPerCell);
		ref byte isImport = ref m_IsImport;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref isImport);
	}
}
