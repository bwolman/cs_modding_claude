namespace Colossal.Serialization.Entities;

public interface ISerializable
{
	void Serialize<TWriter>(TWriter writer) where TWriter : IWriter;

	void Deserialize<TReader>(TReader reader) where TReader : IReader;
}
