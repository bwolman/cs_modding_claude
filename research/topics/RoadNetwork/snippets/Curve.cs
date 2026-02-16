// Source: Game.dll -> Game.Net.Curve (decompiled with ilspycmd v9.1)
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct Curve : IComponentData, IQueryTypeParameter, IStrideSerializable, ISerializable
{
    public Bezier4x3 m_Bezier;

    public float m_Length;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Bezier);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out m_Bezier);
        m_Length = MathUtils.Length(m_Bezier);
    }

    public int GetStride(Context context)
    {
        return UnsafeUtility.SizeOf<float3>();
    }
}
