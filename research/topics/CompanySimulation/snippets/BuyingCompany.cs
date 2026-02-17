using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Companies;

public struct BuyingCompany : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_LastTradePartner;

	public float m_MeanInputTripLength;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity lastTradePartner = m_LastTradePartner;
		writer.Write(lastTradePartner);
		float meanInputTripLength = m_MeanInputTripLength;
		writer.Write(meanInputTripLength);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity lastTradePartner = ref m_LastTradePartner;
		reader.Read(out lastTradePartner);
		ref float meanInputTripLength = ref m_MeanInputTripLength;
		reader.Read(out meanInputTripLength);
	}
}
