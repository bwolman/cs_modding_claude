// Source: Game.dll -> Game.Net.Node (decompiled with ilspycmd v9.1)
using Colossal.Serialization.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct Node : IComponentData, IQueryTypeParameter, IStrideSerializable, ISerializable
{
    public float3 m_Position;

    public quaternion m_Rotation;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        float3 position = m_Position;
        writer.Write(position);
        quaternion rotation = m_Rotation;
        writer.Write(rotation);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        ref float3 position = ref m_Position;
        reader.Read(out position);
        ref quaternion rotation = ref m_Rotation;
        reader.Read(out rotation);
    }

    public int GetStride(Context context)
    {
        return UnsafeUtility.SizeOf<float3>() + UnsafeUtility.SizeOf<quaternion>();
    }
}
