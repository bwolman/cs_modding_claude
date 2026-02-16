using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct School : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_AverageGraduationTime;

	public float m_AverageFailProbability;

	public sbyte m_StudentWellbeing;

	public sbyte m_StudentHealth;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float averageGraduationTime = m_AverageGraduationTime;
		writer.Write(averageGraduationTime);
		float averageFailProbability = m_AverageFailProbability;
		writer.Write(averageFailProbability);
		sbyte studentWellbeing = m_StudentWellbeing;
		writer.Write(studentWellbeing);
		sbyte studentHealth = m_StudentHealth;
		writer.Write(studentHealth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float averageGraduationTime = ref m_AverageGraduationTime;
		reader.Read(out averageGraduationTime);
		ref float averageFailProbability = ref m_AverageFailProbability;
		reader.Read(out averageFailProbability);
		if (reader.context.version >= Version.happinessAdjustRefactoring)
		{
			ref sbyte studentWellbeing = ref m_StudentWellbeing;
			reader.Read(out studentWellbeing);
			ref sbyte studentHealth = ref m_StudentHealth;
			reader.Read(out studentHealth);
		}
	}
}
