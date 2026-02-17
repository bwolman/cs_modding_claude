using System.Collections.Generic;
using System.Runtime.InteropServices;
using Colossal.AssetPipeline.Native;
using Colossal.Compression;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class WriteSystem : GameSystemBase, IWriteBufferProvider<WriteBuffer>
{
	private struct WriteRawBufferJob : IJob
	{
		[ReadOnly]
		public NativeList<byte> m_Buffer;

		public GCHandle m_WriterHandle;

		public void Execute()
		{
			StreamBinaryWriter writer = (StreamBinaryWriter)m_WriterHandle.Target;
			WriteData(writer, m_Buffer.Length);
			WriteData(writer, m_Buffer.AsArray());
		}
	}

	private struct WriteCompressedBufferJob : IJob
	{
		[ReadOnly]
		public CompressedBytesStorage m_CompressedData;

		public int m_UncompressedSize;

		public GCHandle m_WriterHandle;

		public unsafe void Execute()
		{
			StreamBinaryWriter obj = (StreamBinaryWriter)m_WriterHandle.Target;
			BufferHeader data = default(BufferHeader);
			data.size = m_UncompressedSize;
			void* bytes = m_CompressedData.GetBytes(out data.compressedSize);
			WriteData(obj, data);
			obj.WriteBytes(bytes, data.compressedSize);
		}
	}

	private struct DisposeWriterJob : IJob
	{
		public GCHandle m_WriterHandle;

		public void Execute()
		{
			((StreamBinaryWriter)m_WriterHandle.Target).Dispose();
			m_WriterHandle.Free();
		}
	}

	private struct BufferHeader
	{
		public int size;

		public int compressedSize;
	}

	private SaveGameSystem m_SerializationSystem;

	private SerializerSystem m_SerializerSystem;

	private List<(WriteBuffer, BufferFormat)> m_Buffers;

	private JobHandle m_WriteDependency;

	private GCHandle m_WriterHandle;

	public JobHandle writeDependency => m_WriteDependency;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SerializationSystem = base.World.GetOrCreateSystemManaged<SaveGameSystem>();
		m_SerializerSystem = base.World.GetOrCreateSystemManaged<SerializerSystem>();
		m_Buffers = new List<(WriteBuffer, BufferFormat)>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_WriteDependency.Complete();
		for (int i = 0; i < m_Buffers.Count; i++)
		{
			m_Buffers[i].Item1.Dispose();
		}
		base.OnDestroy();
	}

	public WriteBuffer AddBuffer(BufferFormat format)
	{
		int num = 0;
		for (int i = 0; i < m_Buffers.Count; i++)
		{
			var (writeBuffer, format2) = m_Buffers[i];
			if (!writeBuffer.isCompleted)
			{
				break;
			}
			WriteBuffer(writeBuffer, format2);
			num++;
		}
		if (num != 0)
		{
			m_Buffers.RemoveRange(0, num);
		}
		WriteBuffer writeBuffer2 = new WriteBuffer();
		m_Buffers.Add((writeBuffer2, format));
		return writeBuffer2;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		for (int i = 0; i < m_Buffers.Count; i++)
		{
			var (buffer, format) = m_Buffers[i];
			WriteBuffer(buffer, format);
		}
		m_Buffers.Clear();
		if (m_WriterHandle.IsAllocated)
		{
			DisposeWriterJob jobData = new DisposeWriterJob
			{
				m_WriterHandle = m_WriterHandle
			};
			m_WriterHandle = default(GCHandle);
			m_WriteDependency = jobData.Schedule(m_WriteDependency);
		}
	}

	private void WriteBuffer(WriteBuffer buffer, BufferFormat format)
	{
		if (!m_WriterHandle.IsAllocated)
		{
			StreamBinaryWriter value = new StreamBinaryWriter(m_SerializationSystem.stream);
			m_WriterHandle = GCHandle.Alloc(value);
		}
		buffer.CompleteDependencies();
		if (format == BufferFormat.Raw)
		{
			m_SerializerSystem.totalSize += 4 + buffer.buffer.Length;
			WriteRawBufferJob jobData = new WriteRawBufferJob
			{
				m_Buffer = buffer.buffer,
				m_WriterHandle = m_WriterHandle
			};
			m_WriteDependency = jobData.Schedule(m_WriteDependency);
			buffer.buffer.Dispose(m_WriteDependency);
		}
		else if (format.IsCompressed())
		{
			m_SerializerSystem.totalSize += 8 + buffer.buffer.Length;
			CompressionFormat format2 = SerializationUtils.BufferToCompressionFormat(format);
			CompressedBytesStorage compressedBytesStorage = new CompressedBytesStorage(format2, buffer.buffer.Length, Allocator.Persistent);
			int compressionLevel = 3;
			JobHandle jobHandle = CompressionUtils.Compress(format2, buffer.buffer.AsArray(), compressedBytesStorage, default(JobHandle), compressionLevel);
			WriteCompressedBufferJob jobData2 = new WriteCompressedBufferJob
			{
				m_CompressedData = compressedBytesStorage,
				m_UncompressedSize = buffer.buffer.Length,
				m_WriterHandle = m_WriterHandle
			};
			buffer.buffer.Dispose(jobHandle);
			m_WriteDependency = jobData2.Schedule(JobHandle.CombineDependencies(m_WriteDependency, jobHandle));
			compressedBytesStorage.Dispose(m_WriteDependency);
		}
		else
		{
			COSystemBase.baseLog.WarnFormat("Unsupported BufferFormat {0}", format);
		}
	}

	private unsafe static void WriteData<T>(StreamBinaryWriter writer, T data) where T : unmanaged
	{
		void* data2 = UnsafeUtility.AddressOf(ref data);
		writer.WriteBytes(data2, sizeof(T));
	}

	private unsafe static void WriteData(StreamBinaryWriter writer, NativeArray<byte> data)
	{
		void* unsafeReadOnlyPtr = data.GetUnsafeReadOnlyPtr();
		writer.WriteBytes(unsafeReadOnlyPtr, data.Length);
	}

	private unsafe static void WriteData(StreamBinaryWriter writer, NativeSlice<byte> data)
	{
		void* unsafeReadOnlyPtr = data.GetUnsafeReadOnlyPtr();
		writer.WriteBytes(unsafeReadOnlyPtr, data.Length);
	}

	[Preserve]
	public WriteSystem()
	{
	}
}
