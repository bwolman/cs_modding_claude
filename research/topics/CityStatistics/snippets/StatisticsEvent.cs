// Decompiled from Game.dll - Game.City.StatisticsEvent
using Colossal.Serialization.Entities;

namespace Game.City;

public struct StatisticsEvent : ISerializable
{
    public StatisticType m_Statistic;  // Which stat this event contributes to
    public int m_Parameter;             // Sub-parameter (e.g., education level, age group)
    public float m_Change;              // The value to add to the accumulator

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write((int)m_Statistic);
        writer.Write(m_Parameter);
        writer.Write(m_Change);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out int statistic);
        m_Statistic = (StatisticType)statistic;
        reader.Read(out m_Parameter);
        reader.Read(out m_Change);
    }
}
