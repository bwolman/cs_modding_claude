using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct CompanyNotifications : IComponentData, IQueryTypeParameter, ISerializable
{
	public short m_NoInputCounter;

	public short m_NoCustomersCounter;

	public Entity m_NoInputEntity;

	public Entity m_NoCustomersEntity;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref short noInputCounter = ref m_NoInputCounter;
		reader.Read(out noInputCounter);
		ref short noCustomersCounter = ref m_NoCustomersCounter;
		reader.Read(out noCustomersCounter);
		ref Entity noInputEntity = ref m_NoInputEntity;
		reader.Read(out noInputEntity);
		ref Entity noCustomersEntity = ref m_NoCustomersEntity;
		reader.Read(out noCustomersEntity);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		short noInputCounter = m_NoInputCounter;
		writer.Write(noInputCounter);
		short noCustomersCounter = m_NoCustomersCounter;
		writer.Write(noCustomersCounter);
		Entity noInputEntity = m_NoInputEntity;
		writer.Write(noInputEntity);
		Entity noCustomersEntity = m_NoCustomersEntity;
		writer.Write(noCustomersEntity);
	}
}
