using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct ConsumptionData : IComponentData, IQueryTypeParameter, ICombineData<ConsumptionData>, ISerializable
{
	public int m_Upkeep;

	public float m_ElectricityConsumption;

	public float m_WaterConsumption;

	public float m_GarbageAccumulation;

	public float m_TelecomNeed;

	public void AddArchetypeComponents(HashSet<ComponentType> components)
	{
		if (m_ElectricityConsumption > 0f)
		{
			components.Add(ComponentType.ReadWrite<ElectricityConsumer>());
		}
		if (m_WaterConsumption > 0f)
		{
			components.Add(ComponentType.ReadWrite<WaterConsumer>());
		}
		if (m_GarbageAccumulation > 0f)
		{
			components.Add(ComponentType.ReadWrite<GarbageProducer>());
		}
		if (m_TelecomNeed > 0f)
		{
			components.Add(ComponentType.ReadWrite<TelecomConsumer>());
		}
	}

	public void Combine(ConsumptionData otherData)
	{
		m_Upkeep += otherData.m_Upkeep;
		m_ElectricityConsumption += otherData.m_ElectricityConsumption;
		m_WaterConsumption += otherData.m_WaterConsumption;
		m_GarbageAccumulation += otherData.m_GarbageAccumulation;
		m_TelecomNeed = math.max(m_TelecomNeed, otherData.m_TelecomNeed);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int upkeep = m_Upkeep;
		writer.Write(upkeep);
		float electricityConsumption = m_ElectricityConsumption;
		writer.Write(electricityConsumption);
		float waterConsumption = m_WaterConsumption;
		writer.Write(waterConsumption);
		float garbageAccumulation = m_GarbageAccumulation;
		writer.Write(garbageAccumulation);
		float telecomNeed = m_TelecomNeed;
		writer.Write(telecomNeed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int upkeep = ref m_Upkeep;
		reader.Read(out upkeep);
		ref float electricityConsumption = ref m_ElectricityConsumption;
		reader.Read(out electricityConsumption);
		ref float waterConsumption = ref m_WaterConsumption;
		reader.Read(out waterConsumption);
		ref float garbageAccumulation = ref m_GarbageAccumulation;
		reader.Read(out garbageAccumulation);
		if (reader.context.version > Version.telecomNeed)
		{
			ref float telecomNeed = ref m_TelecomNeed;
			reader.Read(out telecomNeed);
		}
	}
}
