using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct CrimeVictim : IComponentData, IQueryTypeParameter, ISerializable, IEnableableComponent
{
	public byte m_Effect;

	public CrimeVictim(byte effect)
	{
		m_Effect = effect;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Effect);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Effect);
	}
}
