using Colossal.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Colossal.Serialization.Entities;

public interface IWriter
{
	Context context { get; }

	void Initialize(Context context, NativeList<byte> buffer, NativeArray<Entity> entityTable);

	WriterBlock Begin();

	bool End(WriterBlock block);

	bool End(WriterBlock block, out int size);

	void Write(NativeArray<Entity> value);

	void Write(NativeList<Entity> value);

	void Write(NativeList<int> value);

	void Write<TSerializable>(NativeArray<TSerializable> value) where TSerializable : struct, ISerializable;

	void Write(NativeArray<int> value);

	void Write(NativeArray<int2> value);

	void Write(NativeArray<ushort> value);

	void Write(NativeArray<byte> value);

	void Write(NativeArray<float4> value);

	void Write(NativeArray<float2> value);

	void Write(NativeArray<byte> value, int stride);

	void Write(Entity value);

	void Write(Entity value, bool ignoreVersion);

	void Write<TSerializable>(TSerializable value) where TSerializable : ISerializable;

	void Write(Bezier4x3 curve);

	void Write(string value);

	void Write(Color value);

	void Write(Color32 value);

	void Write(quaternion value);

	void Write(float4 value);

	void Write(float3 value);

	void Write(float2 value);

	void Write(int4 value);

	void Write(int3 value);

	void Write(int2 value);

	void Write(bool4 value);

	void Write(bool3 value);

	void Write(bool2 value);

	void Write(uint4 value);

	void Write(Hash128 hash);

	void Write(char value);

	void Write(float value);

	void Write(double value);

	void Write(int value);

	void Write(uint value);

	void Write(short value);

	void Write(ushort value);

	void Write(sbyte value);

	void Write(byte value);

	void Write(long value);

	void Write(ulong value);

	void Write(bool value);
}
