using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Colossal.Serialization.Entities;

public abstract class ComponentSerializer
{
	public virtual void Initialize(SystemBase system, NativeArray<int> dataSizes, int sizeIndex)
	{
	}

	public virtual void Update(SystemBase system, Context context)
	{
	}

	public virtual void Dispose()
	{
	}

	public virtual JobHandle Apply(ExclusiveEntityTransaction transaction, JobHandle inputDeps)
	{
		return inputDeps;
	}

	public virtual void Clear()
	{
	}

	public void SerializeType<TWriter>(WriterData writerData, out int overhead) where TWriter : struct, IWriter
	{
		TWriter writer = writerData.GetWriter<TWriter>();
		WriterBlock block = writer.Begin();
		writer.Write((byte)GetSerializerType());
		writer.Write(GetComponentType().AssemblyQualifiedName);
		writer.End(block, out overhead);
		overhead += 4;
	}

	public static bool DeserializeType<TReader>(ReaderData readerData, Dictionary<string, Type> typeTable, out ComponentType componentType, out ComponentSerializerType serializerType, out int overhead) where TReader : struct, IReader
	{
		TReader reader = readerData.GetReader<TReader>();
		ReaderBlock block = reader.Begin(out overhead);
		reader.Read(out byte value);
		reader.Read(out string value2);
		reader.End(block);
		overhead += 4;
		Type value3 = Type.GetType(value2);
		serializerType = (ComponentSerializerType)value;
		if (value3 == null)
		{
			string text = value2;
			while (!typeTable.TryGetValue(text, out value3))
			{
				int num = text.LastIndexOf(',');
				if (num < 0)
				{
					break;
				}
				text = text.Substring(0, num);
			}
		}
		if (value3 != null && (typeof(ISerializable).IsAssignableFrom(value3) || typeof(IEmptySerializable).IsAssignableFrom(value3)))
		{
			componentType = new ComponentType(value3);
			return true;
		}
		Debug.LogWarningFormat("Not serializable type: {0}", value2);
		componentType = default(ComponentType);
		return false;
	}

	public abstract ComponentSerializerType GetSerializerType();

	public abstract Type GetComponentType();

	public abstract JobHandle SerializeData<TWriter>(EntityWriterData writerData, NativeList<ComponentSerializerChunk> chunks, JobHandle inputDeps) where TWriter : struct, IWriter;

	public abstract JobHandle DeserializeData<TReader>(EntityReaderData readerData, NativeArray<Entity> entities, ComponentSerializerType serializerType, JobHandle inputDeps) where TReader : struct, IReader;
}
