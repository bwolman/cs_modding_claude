using Colossal.Serialization.Entities;
using Game.Economy;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Companies;

public struct FreeWorkplaces : IComponentData, IQueryTypeParameter, ISerializable
{
	public byte m_Uneducated;

	public byte m_PoorlyEducated;

	public byte m_Educated;

	public byte m_WellEducated;

	public byte m_HighlyEducated;

	public int Count => m_Uneducated + m_PoorlyEducated + m_Educated + m_WellEducated + m_HighlyEducated;

	public FreeWorkplaces(Workplaces free)
	{
		m_Uneducated = (byte)math.clamp(free.m_Uneducated, 0, 255);
		m_PoorlyEducated = (byte)math.clamp(free.m_PoorlyEducated, 0, 255);
		m_Educated = (byte)math.clamp(free.m_Educated, 0, 255);
		m_WellEducated = (byte)math.clamp(free.m_WellEducated, 0, 255);
		m_HighlyEducated = (byte)math.clamp(free.m_HighlyEducated, 0, 255);
	}

	public void Refresh(DynamicBuffer<Employee> employees, int maxWorkers, WorkplaceComplexity complexity, int level)
	{
		Workplaces workplaces = EconomyUtils.CalculateNumberOfWorkplaces(maxWorkers, complexity, level);
		for (int i = 0; i < employees.Length; i++)
		{
			workplaces[employees[i].m_Level]--;
		}
		m_Uneducated = (byte)math.clamp(workplaces.m_Uneducated, 0, 255);
		m_PoorlyEducated = (byte)math.clamp(workplaces.m_PoorlyEducated, 0, 255);
		m_Educated = (byte)math.clamp(workplaces.m_Educated, 0, 255);
		m_WellEducated = (byte)math.clamp(workplaces.m_WellEducated, 0, 255);
		m_HighlyEducated = (byte)math.clamp(workplaces.m_HighlyEducated, 0, 255);
	}

	public byte GetLowestFree()
	{
		for (byte b = 0; b <= 4; b++)
		{
			if (GetFree(b) > 0)
			{
				return b;
			}
		}
		return 5;
	}

	public int GetBestFor(int level)
	{
		for (int num = level; num >= 0; num--)
		{
			if (GetFree((byte)num) > 0)
			{
				return num;
			}
		}
		return -1;
	}

	public byte GetFree(int level)
	{
		return level switch
		{
			0 => m_Uneducated, 
			1 => m_PoorlyEducated, 
			2 => m_Educated, 
			3 => m_WellEducated, 
			4 => m_HighlyEducated, 
			_ => 0, 
		};
	}

	private void SetFree(int level, byte amount)
	{
		switch (level)
		{
		case 0:
			m_Uneducated = amount;
			break;
		case 1:
			m_PoorlyEducated = amount;
			break;
		case 2:
			m_Educated = amount;
			break;
		case 3:
			m_WellEducated = amount;
			break;
		case 4:
			m_HighlyEducated = amount;
			break;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte uneducated = m_Uneducated;
		writer.Write(uneducated);
		byte poorlyEducated = m_PoorlyEducated;
		writer.Write(poorlyEducated);
		byte educated = m_Educated;
		writer.Write(educated);
		byte wellEducated = m_WellEducated;
		writer.Write(wellEducated);
		byte highlyEducated = m_HighlyEducated;
		writer.Write(highlyEducated);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref byte uneducated = ref m_Uneducated;
		reader.Read(out uneducated);
		ref byte poorlyEducated = ref m_PoorlyEducated;
		reader.Read(out poorlyEducated);
		ref byte educated = ref m_Educated;
		reader.Read(out educated);
		ref byte wellEducated = ref m_WellEducated;
		reader.Read(out wellEducated);
		ref byte highlyEducated = ref m_HighlyEducated;
		reader.Read(out highlyEducated);
	}
}
