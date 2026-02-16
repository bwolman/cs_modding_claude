using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ElectricityConsumer : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_WantedConsumption;

	public int m_FulfilledConsumption;

	public short m_CooldownCounter;

	public ElectricityConsumerFlags m_Flags;

	public bool electricityConnected => (m_Flags & ElectricityConsumerFlags.Connected) != 0;
}

[Flags]
public enum ElectricityConsumerFlags : byte
{
	None = 0,
	Connected = 1,
	NoElectricityWarning = 2,
	BottleneckWarning = 4
}
