// Decompiled from Game.dll - Game.Simulation.ICityStatisticsSystem
using System;
using Game.City;
using Unity.Collections;
using Unity.Entities;

namespace Game.Simulation;

public interface ICityStatisticsSystem
{
    Action eventStatisticsUpdated { get; set; }
    int sampleCount { get; }

    int GetStatisticValue(StatisticType type, int parameter = 0);
    long GetStatisticValueLong(StatisticType type, int parameter = 0);
    int GetStatisticValue(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0);
    long GetStatisticValueLong(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0);

    NativeArray<CityStatistic> GetStatisticArray(StatisticType type, int parameter = 0);
    NativeArray<int> GetStatisticDataArray(StatisticType type, int parameter = 0);
    NativeArray<int> GetStatisticDataArray(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0);
    NativeArray<long> GetStatisticDataArrayLong(StatisticType type, int parameter = 0);
    NativeArray<long> GetStatisticDataArrayLong(BufferLookup<CityStatistic> stats, StatisticType type, int parameter = 0);

    NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> GetLookup();
    void CompleteWriters();
    uint GetSampleFrameIndex(int index);
}
