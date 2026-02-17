using Unity.Collections;
using Unity.Jobs;

namespace Colossal.Serialization.Entities;

public interface IWriteBuffer
{
	NativeList<byte> buffer { get; }

	void Done(JobHandle handle);

	void Done();
}
