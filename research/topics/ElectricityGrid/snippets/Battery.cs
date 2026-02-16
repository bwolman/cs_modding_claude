using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Battery : IComponentData, IQueryTypeParameter, ISerializable
{
	public long m_StoredEnergy;

	public int m_Capacity;

	public int m_LastFlow;

	/// <summary>
	/// Stored energy in hour-ticks. Divides by kUpdatesPerHour (85).
	/// </summary>
	public int storedEnergyHours => (int)(m_StoredEnergy / 85);
}
