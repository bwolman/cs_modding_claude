using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct WaterConsumer : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Pollution;

	public int m_WantedConsumption;

	public int m_FulfilledFresh;

	public int m_FulfilledSewage;

	public byte m_FreshCooldownCounter;

	public byte m_SewageCooldownCounter;

	public WaterConsumerFlags m_Flags;

	public bool waterConnected => (m_Flags & WaterConsumerFlags.WaterConnected) != 0;

	public bool sewageConnected => (m_Flags & WaterConsumerFlags.SewageConnected) != 0;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float pollution = m_Pollution;
		writer.Write(pollution);
		int wantedConsumption = m_WantedConsumption;
		writer.Write(wantedConsumption);
		int fulfilledFresh = m_FulfilledFresh;
		writer.Write(fulfilledFresh);
		int fulfilledSewage = m_FulfilledSewage;
		writer.Write(fulfilledSewage);
		byte freshCooldownCounter = m_FreshCooldownCounter;
		writer.Write(freshCooldownCounter);
		byte sewageCooldownCounter = m_SewageCooldownCounter;
		writer.Write(sewageCooldownCounter);
		WaterConsumerFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.waterPipePollution)
		{
			ref float pollution = ref m_Pollution;
			reader.Read(out pollution);
		}
		else
		{
			reader.Read(out int _);
		}
		if (reader.context.version < Version.waterPipeFlowSim)
		{
			reader.Read(out int _);
			reader.Read(out int _);
		}
		if (reader.context.version >= Version.waterConsumption)
		{
			ref int wantedConsumption = ref m_WantedConsumption;
			reader.Read(out wantedConsumption);
		}
		if (reader.context.version < Version.buildingEfficiencyRework)
		{
			if (reader.context.version >= Version.utilityFeePrecision)
			{
				reader.Read(out float _);
			}
			else if (reader.context.version >= Version.waterFee)
			{
				reader.Read(out int _);
			}
		}
		if (reader.context.version >= Version.waterPipeFlowSim)
		{
			ref int fulfilledFresh = ref m_FulfilledFresh;
			reader.Read(out fulfilledFresh);
			ref int fulfilledSewage = ref m_FulfilledSewage;
			reader.Read(out fulfilledSewage);
			ref byte freshCooldownCounter = ref m_FreshCooldownCounter;
			reader.Read(out freshCooldownCounter);
			ref byte sewageCooldownCounter = ref m_SewageCooldownCounter;
			reader.Read(out sewageCooldownCounter);
		}
		if (reader.context.version >= Version.waterConsumerFlags)
		{
			reader.Read(out byte value6);
			m_Flags = (WaterConsumerFlags)value6;
		}
	}
}
