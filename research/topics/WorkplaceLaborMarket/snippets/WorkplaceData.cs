using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct WorkplaceData : IComponentData, IQueryTypeParameter, ICombineData<WorkplaceData>, ISerializable
{
	public WorkplaceComplexity m_Complexity;

	public int m_MaxWorkers;

	public float m_EveningShiftProbability;

	public float m_NightShiftProbability;

	public int m_MinimumWorkersLimit;

	public int m_WorkConditions;

	public void Combine(WorkplaceData other)
	{
		int maxWorkers = m_MaxWorkers;
		m_MaxWorkers += other.m_MaxWorkers;
		m_MinimumWorkersLimit += other.m_MinimumWorkersLimit;
		m_WorkConditions += other.m_WorkConditions;
		if (m_MaxWorkers > 0)
		{
			m_EveningShiftProbability = math.lerp(other.m_EveningShiftProbability, m_EveningShiftProbability, maxWorkers / m_MaxWorkers);
			m_NightShiftProbability = math.lerp(other.m_NightShiftProbability, m_NightShiftProbability, maxWorkers / m_MaxWorkers);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int maxWorkers = m_MaxWorkers;
		writer.Write(maxWorkers);
		float eveningShiftProbability = m_EveningShiftProbability;
		writer.Write(eveningShiftProbability);
		float nightShiftProbability = m_NightShiftProbability;
		writer.Write(nightShiftProbability);
		byte value = (byte)m_Complexity;
		writer.Write(value);
		int minimumWorkersLimit = m_MinimumWorkersLimit;
		writer.Write(minimumWorkersLimit);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int maxWorkers = ref m_MaxWorkers;
		reader.Read(out maxWorkers);
		ref float eveningShiftProbability = ref m_EveningShiftProbability;
		reader.Read(out eveningShiftProbability);
		ref float nightShiftProbability = ref m_NightShiftProbability;
		reader.Read(out nightShiftProbability);
		reader.Read(out byte value);
		m_Complexity = (WorkplaceComplexity)value;
		if (reader.context.version > Version.addMinimumWorkersLimit)
		{
			ref int minimumWorkersLimit = ref m_MinimumWorkersLimit;
			reader.Read(out minimumWorkersLimit);
		}
	}
}
