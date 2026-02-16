using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ElectricityProducer : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Capacity;

	public int m_LastProduction;
}
