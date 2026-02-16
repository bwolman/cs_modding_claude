// Source: Game.dll -> Game.Simulation.BudgetApplySystem
// Applies accumulated income/expense to PlayerMoney every tick (1024 times/day)

using Game.City;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public class BudgetApplySystem : GameSystemBase
{
    [BurstCompile]
    private struct BudgetApplyJob : IJob
    {
        public NativeArray<int> m_Income;
        public NativeArray<int> m_Expenses;
        public ComponentLookup<PlayerMoney> m_PlayerMoneys;
        public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;
        public Entity m_City;

        public void Execute()
        {
            int num = 0;
            // Sum all expenses (15 categories)
            for (int i = 0; i < 15; i++)
            {
                ExpenseSource parameter = (ExpenseSource)i;
                int expense = CityServiceBudgetSystem.GetExpense((ExpenseSource)i, m_Expenses);
                num -= expense;
                m_StatisticsEventQueue.Enqueue(new StatisticsEvent
                {
                    m_Statistic = StatisticType.Expense,
                    m_Change = math.abs((float)expense / (float)kUpdatesPerDay),
                    m_Parameter = (int)parameter
                });
            }
            // Sum all income (14 categories)
            for (int j = 0; j < 14; j++)
            {
                IncomeSource parameter2 = (IncomeSource)j;
                int income = CityServiceBudgetSystem.GetIncome((IncomeSource)j, m_Income);
                num += income;
                m_StatisticsEventQueue.Enqueue(new StatisticsEvent
                {
                    m_Statistic = StatisticType.Income,
                    m_Change = math.abs((float)income / (float)kUpdatesPerDay),
                    m_Parameter = (int)parameter2
                });
            }
            // Apply net change to player money
            PlayerMoney value = m_PlayerMoneys[m_City];
            value.Add(num / kUpdatesPerDay);
            m_PlayerMoneys[m_City] = value;
        }
    }

    public static readonly int kUpdatesPerDay = 1024;

    // Update interval: 262144 / 1024 = 256 frames between updates
    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 262144 / kUpdatesPerDay;
    }
}
