using Unity.Jobs;

namespace Colossal.Serialization.Entities;

public interface IJobSerializable
{
	JobHandle Serialize<TWriter>(EntityWriterData writerData, JobHandle inputDeps) where TWriter : struct, IWriter;

	JobHandle Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps) where TReader : struct, IReader;

	JobHandle SetDefaults(Context context);
}
