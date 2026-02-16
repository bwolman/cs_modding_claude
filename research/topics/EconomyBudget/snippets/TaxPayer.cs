// Source: Game.dll -> Game.Agents.TaxPayer
// ECS component attached to households and companies that tracks taxable income

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Agents;

public struct TaxPayer : IComponentData, IQueryTypeParameter, ISerializable
{
    public int m_UntaxedIncome;
    public int m_AverageTaxRate;
    public int m_AverageTaxPaid;

    // Tax formula: tax = round(0.01 * m_AverageTaxRate * m_UntaxedIncome)
    // After payment, m_UntaxedIncome is reset to 0
    // m_AverageTaxPaid is set to tax * kUpdatesPerDay (32)
}
