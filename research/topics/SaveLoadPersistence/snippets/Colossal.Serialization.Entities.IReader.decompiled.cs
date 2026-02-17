using Colossal.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Colossal.Serialization.Entities;

public interface IReader
{
	Context context { get; }

	void Initialize(Context context, NativeArray<byte> buffer, NativeReference<int> position, NativeArray<Entity> entityTable);

	ReaderBlock Begin();

	ReaderBlock Begin(out int size);

	bool End(ReaderBlock block);

	void Read(NativeArray<Entity> value);

	void Read(NativeArray<int> value);

	void Read(NativeArray<int2> value);

	void Read(NativeArray<ushort> value);

	void Read(NativeArray<byte> value);

	void Read(NativeArray<float4> value);

	void Read(NativeArray<float2> value);

	void Read(NativeArray<byte> value, int stride);

	void Read(NativeList<int> value);

	void Read(NativeList<Entity> value);

	void Read<TSerializable>(NativeArray<TSerializable> value) where TSerializable : struct, ISerializable;

	void Read(out Entity value);

	void Read<TSerializable>(out TSerializable value) where TSerializable : struct, ISerializable;

	void Read<TSerializable>(TSerializable value) where TSerializable : class, ISerializable;

	void Read(out Bezier4x3 curve);

	void Read(out string value);

	void Read(out Color value);

	void Read(out Color32 value);

	void Read(out quaternion value);

	void Read(out float4 value);

	void Read(out float3 value);

	void Read(out float2 value);

	void Read(out int4 value);

	void Read(out int3 value);

	void Read(out int2 value);

	void Read(out bool4 value);

	void Read(out bool3 value);

	void Read(out bool2 value);

	void Read(out uint4 value);

	void Read(out Hash128 value);

	void Read(out char value);

	void Read(out float value);

	void Read(out double value);

	void Read(out int value);

	void Read(out uint value);

	void Read(out short value);

	void Read(out ushort value);

	void Read(out sbyte value);

	void Read(out byte value);

	void Read(out long value);

	void Read(out ulong value);

	void Read(out bool value);

	void Skip(int size);
}
