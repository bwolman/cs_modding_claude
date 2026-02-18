// Decompiled from Game.dll - Game.City.CityStatistic
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct CityStatistic : IBufferElementData, ISerializable
{
    public double m_Value;       // Current period's accumulated value
    public double m_TotalValue;  // Computed total (depends on CollectionType)

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_Value);
        writer.Write(m_TotalValue);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        // Handles version migration from int -> long -> double
        reader.Read(out m_Value);
        reader.Read(out m_TotalValue);
    }
}
