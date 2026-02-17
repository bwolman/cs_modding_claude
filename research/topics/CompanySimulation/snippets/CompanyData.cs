using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Companies;

public struct CompanyData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Random m_RandomSeed;

	public Entity m_Brand;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint state = m_RandomSeed.state;
		writer.Write(state);
		Entity brand = m_Brand;
		writer.Write(brand);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint state = ref m_RandomSeed.state;
		reader.Read(out state);
		ref Entity brand = ref m_Brand;
		reader.Read(out brand);
	}
}
