// Source: Game.dll -> Game.City.PlayerMoney
// ECS component on the City entity that tracks the player's money balance

using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.City;

public struct PlayerMoney : IComponentData, IQueryTypeParameter, ISerializable
{
    public const int kMaxMoney = 2000000000;

    private int m_Money;
    public bool m_Unlimited;

    public int money
    {
        get
        {
            if (!m_Unlimited)
                return m_Money;
            return 2000000000;
        }
    }

    public PlayerMoney(int amount)
    {
        m_Money = math.clamp(amount, -2000000000, 2000000000);
        m_Unlimited = false;
    }

    public void Add(int value)
    {
        m_Money = math.clamp(m_Money + value, -2000000000, 2000000000);
    }

    public void Subtract(int amount)
    {
        Add(-amount);
    }
}
