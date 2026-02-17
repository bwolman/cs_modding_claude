using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct WorkProvider : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_MaxWorkers;

	public short m_UneducatedCooldown;

	public short m_EducatedCooldown;

	public Entity m_UneducatedNotificationEntity;

	public Entity m_EducatedNotificationEntity;

	public short m_EfficiencyCooldown;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int maxWorkers = m_MaxWorkers;
		writer.Write(maxWorkers);
		short uneducatedCooldown = m_UneducatedCooldown;
		writer.Write(uneducatedCooldown);
		short educatedCooldown = m_EducatedCooldown;
		writer.Write(educatedCooldown);
		Entity uneducatedNotificationEntity = m_UneducatedNotificationEntity;
		writer.Write(uneducatedNotificationEntity);
		Entity educatedNotificationEntity = m_EducatedNotificationEntity;
		writer.Write(educatedNotificationEntity);
		short efficiencyCooldown = m_EfficiencyCooldown;
		writer.Write(efficiencyCooldown);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int maxWorkers = ref m_MaxWorkers;
		reader.Read(out maxWorkers);
		if (reader.context.version >= Version.companyNotifications)
		{
			ref short uneducatedCooldown = ref m_UneducatedCooldown;
			reader.Read(out uneducatedCooldown);
			ref short educatedCooldown = ref m_EducatedCooldown;
			reader.Read(out educatedCooldown);
			ref Entity uneducatedNotificationEntity = ref m_UneducatedNotificationEntity;
			reader.Read(out uneducatedNotificationEntity);
			ref Entity educatedNotificationEntity = ref m_EducatedNotificationEntity;
			reader.Read(out educatedNotificationEntity);
		}
		if (reader.context.version >= Version.buildingEfficiencyRework)
		{
			ref short efficiencyCooldown = ref m_EfficiencyCooldown;
			reader.Read(out efficiencyCooldown);
		}
	}
}
