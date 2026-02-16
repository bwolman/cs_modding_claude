using Colossal.Serialization.Entities;
using Game.Common;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Citizens;

public struct Citizen : IComponentData, IQueryTypeParameter, ISerializable
{
	public ushort m_PseudoRandom;

	public CitizenFlags m_State;

	public byte m_WellBeing;

	public byte m_Health;

	public byte m_LeisureCounter;

	public byte m_PenaltyCounter;

	public int m_UnemploymentCounter;

	public short m_BirthDay;

	public float m_UnemploymentTimeCounter;

	public int m_SicknessPenalty;

	public int Happiness => (m_WellBeing + m_Health) / 2;

	public float GetAgeInDays(uint simulationFrame, TimeData timeData)
	{
		return TimeSystem.GetDay(simulationFrame, timeData) - m_BirthDay;
	}

	public Random GetPseudoRandom(CitizenPseudoRandom reason)
	{
		Random val = default(Random);
		val..ctor((uint)((ulong)reason ^ (ulong)((m_PseudoRandom << 16) | m_PseudoRandom)));
		val.NextUInt();
		uint num = val.NextUInt();
		num = math.select(num, 4294967295u, num == 0);
		return new Random(num);
	}

	public int GetEducationLevel()
	{
		if ((m_State & CitizenFlags.EducationBit3) != CitizenFlags.None)
		{
			return 4;
		}
		return (((m_State & CitizenFlags.EducationBit1) != CitizenFlags.None) ? 2 : 0) + (((m_State & CitizenFlags.EducationBit2) != CitizenFlags.None) ? 1 : 0);
	}

	public void SetAge(CitizenAge newAge)
	{
		m_State = (CitizenFlags)((int)((uint)(m_State & ~(CitizenFlags.AgeBit1 | CitizenFlags.AgeBit2)) | (uint)(((newAge & CitizenAge.Adult) != CitizenAge.Child) ? 1 : 0)) | (((int)newAge % 2 != 0) ? 2 : 0));
	}

	public CitizenAge GetAge()
	{
		return (CitizenAge)(2 * (((m_State & CitizenFlags.AgeBit1) != CitizenFlags.None) ? 1 : 0) + (((m_State & CitizenFlags.AgeBit2) != CitizenFlags.None) ? 1 : 0));
	}

	// Serialize/Deserialize omitted for brevity — see full decompile
}
