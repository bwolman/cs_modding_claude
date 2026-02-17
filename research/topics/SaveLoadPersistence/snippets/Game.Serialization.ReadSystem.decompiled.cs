using System;
using Colossal;
using Colossal.Compression;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class ReadSystem : GameSystemBase, IReadBufferProvider<ReadBuffer>
{
	private struct BufferHeader
	{
		public int size;

		public int compressedSize;
	}

	private LoadGameSystem m_DeserializationSystem;

	private SerializerSystem m_SerializerSystem;

	private StreamBinaryReader m_Reader;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_DeserializationSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_SerializerSystem = base.World.GetOrCreateSystemManaged<SerializerSystem>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		Clear();
		base.OnDestroy();
	}

	public unsafe ReadBuffer GetBuffer(BufferFormat format)
	{
		if (m_DeserializationSystem.dataDescriptor == AsyncReadDescriptor.Invalid)
		{
			return null;
		}
		if (m_Reader == null)
		{
			m_Reader = new StreamBinaryReader(m_DeserializationSystem.dataDescriptor, 65536L);
		}
		BufferHeader data = default(BufferHeader);
		if (format.IsCompressed())
		{
			ReadData(m_Reader, out data);
			m_SerializerSystem.totalSize += sizeof(BufferHeader);
		}
		else
		{
			ReadData(m_Reader, out data.size);
			m_SerializerSystem.totalSize += 4;
		}
		ReadBuffer readBuffer = new ReadBuffer(data.size);
		m_SerializerSystem.totalSize += data.size;
		if (format.IsCompressed())
		{
			NativeArray<byte> nativeArray = new NativeArray<byte>(data.compressedSize, Allocator.Persistent);
			ReadData(m_Reader, nativeArray);
			using (PerformanceCounter.Start(delegate(TimeSpan t)
			{
				COSystemBase.baseLog.VerboseFormat("Decompressed in {0}ms", t.TotalMilliseconds);
			}))
			{
				CompressionUtils.Decompress(SerializationUtils.BufferToCompressionFormat(format), nativeArray, readBuffer.buffer).Complete();
			}
			nativeArray.Dispose();
		}
		else if (format == BufferFormat.Raw)
		{
			ReadData(m_Reader, readBuffer.buffer);
		}
		else
		{
			COSystemBase.baseLog.WarnFormat("Unsupported BufferFormat {0}", format);
		}
		return readBuffer;
	}

	public unsafe ReadBuffer GetBuffer(BufferFormat format, out JobHandle dependency)
	{
		dependency = default(JobHandle);
		if (m_DeserializationSystem.dataDescriptor == AsyncReadDescriptor.Invalid)
		{
			return null;
		}
		if (m_Reader == null)
		{
			m_Reader = new StreamBinaryReader(m_DeserializationSystem.dataDescriptor, 65536L);
		}
		BufferHeader data = default(BufferHeader);
		if (format.IsCompressed())
		{
			ReadData(m_Reader, out data);
			m_SerializerSystem.totalSize += sizeof(BufferHeader);
		}
		else
		{
			ReadData(m_Reader, out data.size);
			m_SerializerSystem.totalSize += 4;
		}
		ReadBuffer readBuffer = new ReadBuffer(data.size);
		m_SerializerSystem.totalSize += data.size;
		if (format.IsCompressed())
		{
			NativeArray<byte> nativeArray = new NativeArray<byte>(data.compressedSize, Allocator.Persistent);
			ReadData(m_Reader, nativeArray, out dependency);
			dependency = CompressionUtils.Decompress(SerializationUtils.BufferToCompressionFormat(format), nativeArray, readBuffer.buffer, dependency);
			nativeArray.Dispose(dependency);
		}
		else if (format == BufferFormat.Raw)
		{
			ReadData(m_Reader, readBuffer.buffer, out dependency);
		}
		else
		{
			COSystemBase.baseLog.WarnFormat("Unsupported BufferFormat {0}", format);
		}
		return readBuffer;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Clear();
	}

	private void Clear()
	{
		if (m_Reader != null)
		{
			m_Reader.Dispose();
			m_Reader = null;
		}
	}

	private unsafe static void ReadData<T>(StreamBinaryReader reader, out T data) where T : unmanaged
	{
		data = default(T);
		void* data2 = UnsafeUtility.AddressOf(ref data);
		reader.ReadBytes(data2, sizeof(T));
	}

	private unsafe static void ReadData(StreamBinaryReader reader, NativeArray<byte> data)
	{
		void* unsafePtr = data.GetUnsafePtr();
		reader.ReadBytes(unsafePtr, data.Length);
	}

	private unsafe static void ReadData(StreamBinaryReader reader, NativeArray<byte> data, out JobHandle dependency)
	{
		void* unsafePtr = data.GetUnsafePtr();
		reader.ReadBytes(unsafePtr, data.Length, out dependency);
	}

	[Preserve]
	public ReadSystem()
	{
	}
}
