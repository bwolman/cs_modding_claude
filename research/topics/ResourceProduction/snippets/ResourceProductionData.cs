using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct ResourceProductionData : IBufferElementData, ISerializable
{
	public Resource m_Type;

	public int m_ProductionRate;

	public int m_StorageCapacity;

	public ResourceProductionData(Resource type, int productionRate, int storageCapacity)
	{
		m_Type = type;
		m_ProductionRate = productionRate;
		m_StorageCapacity = storageCapacity;
	}

	public static void Combine(NativeList<ResourceProductionData> resources, DynamicBuffer<ResourceProductionData> others)
	{
		for (int i = 0; i < others.Length; i++)
		{
			ResourceProductionData resourceProductionData = others[i];
			int num = 0;
			while (true)
			{
				if (num < resources.Length)
				{
					ResourceProductionData resourceProductionData2 = resources[num];
					if (resourceProductionData2.m_Type == resourceProductionData.m_Type)
					{
						resourceProductionData2.m_ProductionRate += resourceProductionData.m_ProductionRate;
						resourceProductionData2.m_StorageCapacity += resourceProductionData.m_StorageCapacity;
						resources[num] = resourceProductionData2;
						break;
					}
					num++;
					continue;
				}
				resources.Add(ref resourceProductionData);
				break;
			}
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int productionRate = m_ProductionRate;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(productionRate);
		int storageCapacity = m_StorageCapacity;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(storageCapacity);
		sbyte num = (sbyte)EconomyUtils.GetResourceIndex(m_Type);
		((IWriter)writer/*cast due to .constrained prefix*/).Write(num);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int productionRate = ref m_ProductionRate;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref productionRate);
		ref int storageCapacity = ref m_StorageCapacity;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref storageCapacity);
		sbyte index = default(sbyte);
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref index);
		m_Type = EconomyUtils.GetResource(index);
	}
}
