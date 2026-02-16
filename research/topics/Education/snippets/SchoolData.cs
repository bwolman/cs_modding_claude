using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct SchoolData : IComponentData, IQueryTypeParameter, ICombineData<SchoolData>, ISerializable
{
	public int m_StudentCapacity;

	public float m_GraduationModifier;

	public byte m_EducationLevel;

	public sbyte m_StudentWellbeing;

	public sbyte m_StudentHealth;

	public void Combine(SchoolData otherData)
	{
		m_StudentCapacity += otherData.m_StudentCapacity;
		m_EducationLevel = (byte)math.max((int)m_EducationLevel, (int)otherData.m_EducationLevel);
		m_GraduationModifier += otherData.m_GraduationModifier;
		m_StudentWellbeing += otherData.m_StudentWellbeing;
		m_StudentHealth += otherData.m_StudentHealth;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int studentCapacity = m_StudentCapacity;
		writer.Write(studentCapacity);
		float graduationModifier = m_GraduationModifier;
		writer.Write(graduationModifier);
		byte educationLevel = m_EducationLevel;
		writer.Write(educationLevel);
		sbyte studentWellbeing = m_StudentWellbeing;
		writer.Write(studentWellbeing);
		sbyte studentHealth = m_StudentHealth;
		writer.Write(studentHealth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int studentCapacity = ref m_StudentCapacity;
		reader.Read(out studentCapacity);
		ref float graduationModifier = ref m_GraduationModifier;
		reader.Read(out graduationModifier);
		ref byte educationLevel = ref m_EducationLevel;
		reader.Read(out educationLevel);
		if (reader.context.version >= Version.happinessAdjustRefactoring)
		{
			ref sbyte studentWellbeing = ref m_StudentWellbeing;
			reader.Read(out studentWellbeing);
			ref sbyte studentHealth = ref m_StudentHealth;
			reader.Read(out studentHealth);
		}
	}
}
