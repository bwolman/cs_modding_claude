using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Companies;

public struct StorageLimitData : IComponentData, IQueryTypeParameter, ISerializable, ICombineData<StorageLimitData>
{
	public int m_Limit;

	public int GetAdjustedLimitForWarehouse(SpawnableBuildingData spawnable, BuildingData building)
	{
		return m_Limit * spawnable.m_Level * building.m_LotSize.x * building.m_LotSize.y;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Limit);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Limit);
	}

	public void Combine(StorageLimitData otherData)
	{
		m_Limit += otherData.m_Limit;
	}
}
