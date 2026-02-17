using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct ServiceCompanyData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_MaxService;

	public int m_WorkPerUnit;

	public float m_MaxWorkersPerCell;

	public int m_ServiceConsuming;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int maxService = m_MaxService;
		writer.Write(maxService);
		int workPerUnit = m_WorkPerUnit;
		writer.Write(workPerUnit);
		float maxWorkersPerCell = m_MaxWorkersPerCell;
		writer.Write(maxWorkersPerCell);
		int serviceConsuming = m_ServiceConsuming;
		writer.Write(serviceConsuming);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int maxService = ref m_MaxService;
		reader.Read(out maxService);
		ref int workPerUnit = ref m_WorkPerUnit;
		reader.Read(out workPerUnit);
		ref float maxWorkersPerCell = ref m_MaxWorkersPerCell;
		reader.Read(out maxWorkersPerCell);
		if (reader.context.version >= Version.serviceCompanyConsuming)
		{
			ref int serviceConsuming = ref m_ServiceConsuming;
			reader.Read(out serviceConsuming);
		}
	}
}
