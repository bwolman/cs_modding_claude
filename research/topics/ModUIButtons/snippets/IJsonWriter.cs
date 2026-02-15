// Decompiled from: Colossal.UI.Binding.dll
// IJsonWriter — interface for serializing C# values to JSON for the JS frontend
// IJsonReader — interface for deserializing JS values back to C#
// IJsonWritable / IJsonReadable — marker interfaces for custom serializable types

namespace Colossal.UI.Binding;

// === IJsonWriter ===
// Used by IWriter<T> implementations to serialize values to JSON events.
// Supports structured output: types, maps (objects), arrays, and primitives.
public interface IJsonWriter
{
	string debugName { get; }

	// Structured output
	void TypeBegin(string name);   // Begin named object: { "__Type": name, ... }
	void TypeEnd();
	void MapBegin(uint size);      // Begin anonymous object: { ... }
	void MapEnd();
	void ArrayBegin(uint size);    // Begin array: [ ... ]
	void ArrayEnd();
	void PropertyName(string name); // Write property key

	// Primitive writes
	void WriteNull();
	void Write(bool value);
	void Write(int value);
	void Write(uint value);
	void Write(long value);
	void Write(ulong value);
	void Write(float value);
	void Write(double value);
	void Write(string value);
}

// === IJsonReader ===
// Used by IReader<T> implementations to deserialize JS trigger arguments.
public interface IJsonReader
{
	string debugName { get; }

	// Primitive reads
	void Read(out bool value);
	void Read(out uint value);
	void Read(out int value);
	void Read(out ulong value);
	void Read(out long value);
	void Read(out float value);
	void Read(out double value);
	void Read(out string value);

	// Collection reads
	ulong ReadArrayBegin();
	void ReadArrayElement(ulong index);
	void ReadArrayEnd();
	ulong ReadMapBegin();
	void ReadMapKeyValue();
	void ReadMapEnd();

	// Structured reads
	bool ReadProperty(string name);
	ValueType PeekValueType();
	void SkipValue();
	int GetArgumentsCount();
}

// === IJsonWritable ===
// Implement on custom types to enable automatic serialization.
// ValueWriters.Create<T>() auto-detects this interface.
public interface IJsonWritable
{
	void Write(IJsonWriter writer);
}

// === IJsonReadable ===
// Implement on custom types to enable automatic deserialization.
// ValueReaders.Create<T>() auto-detects this interface.
public interface IJsonReadable
{
	void Read(IJsonReader reader);
}
