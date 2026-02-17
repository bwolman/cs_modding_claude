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
		writer.Write(input);
		ResourceStack input2 = m_Input2;
		writer.Write(input2);
		ResourceStack output = m_Output;
		writer.Write(output);
		int workPerUnit = m_WorkPerUnit;
		writer.Write(workPerUnit);
		float maxWorkersPerCell = m_MaxWorkersPerCell;
		writer.Write(maxWorkersPerCell);
		byte isImport = m_IsImport;
		writer.Write(isImport);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref ResourceStack input = ref m_Input1;
		reader.Read(out input);
		ref ResourceStack input2 = ref m_Input2;
		reader.Read(out input2);
		ref ResourceStack output = ref m_Output;
		reader.Read(out output);
		ref int workPerUnit = ref m_WorkPerUnit;
		reader.Read(out workPerUnit);
		ref float maxWorkersPerCell = ref m_MaxWorkersPerCell;
		reader.Read(out maxWorkersPerCell);
		ref byte isImport = ref m_IsImport;
		reader.Read(out isImport);
	}
}
