using Unity.Collections;
using Unity.Jobs;

namespace Colossal.Serialization.Entities;

public interface IReadBuffer
{
	NativeArray<byte> buffer { get; }

	NativeReference<int> position { get; }

	void Done(JobHandle handle);

	void Done();
}
