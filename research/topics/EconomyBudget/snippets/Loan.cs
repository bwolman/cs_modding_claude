// Source: Game.dll -> Game.Simulation.Loan
// ECS component on the City entity tracking the current loan

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct Loan : IComponentData, IQueryTypeParameter, ISerializable
{
    public int m_Amount;
    public uint m_LastModified;  // simulation frame when last modified
}

// Source: Game.dll -> Game.Simulation.Creditworthiness
public struct Creditworthiness : IComponentData, IQueryTypeParameter, ISerializable
{
    public int m_Amount;  // max loan amount available
}

// Source: Game.dll -> Game.Tools.LoanInfo
public struct LoanInfo
{
    public int m_Amount;
    public float m_DailyInterestRate;
    public int m_DailyPayment;
}
