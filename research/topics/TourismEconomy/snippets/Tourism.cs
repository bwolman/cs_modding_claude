using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.City;

public struct Tourism : IComponentData, IQueryTypeParameter, IDefaultSerializable, ISerializable
{
	public int m_CurrentTourists;

	public int m_AverageTourists;

	public int m_Attractiveness;

	public int2 m_Lodging;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int currentTourists = ref m_CurrentTourists;
		reader.Read(out currentTourists);
		ref int averageTourists = ref m_AverageTourists;
		reader.Read(out averageTourists);
		ref int attractiveness = ref m_Attractiveness;
		reader.Read(out attractiveness);
		ref int2 lodging = ref m_Lodging;
		reader.Read(out lodging);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int currentTourists = m_CurrentTourists;
		writer.Write(currentTourists);
		int averageTourists = m_AverageTourists;
		writer.Write(averageTourists);
		int attractiveness = m_Attractiveness;
		writer.Write(attractiveness);
		int2 lodging = m_Lodging;
		writer.Write(lodging);
	}

	public void SetDefaults(Context context)
	{
		m_CurrentTourists = 0;
		m_AverageTourists = 0;
		m_Attractiveness = 0;
		m_Lodging = default(int2);
	}
}
