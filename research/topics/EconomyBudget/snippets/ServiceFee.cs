// Source: Game.dll -> Game.City.ServiceFee
// Buffer element on the City entity that stores per-service fee rates

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct ServiceFee : IBufferElementData, ISerializable
{
    public PlayerResource m_Resource;
    public float m_Fee;

    public float GetDefaultFee(PlayerResource resource)
    {
        return resource switch
        {
            PlayerResource.BasicEducation => 100f,
            PlayerResource.SecondaryEducation => 200f,
            PlayerResource.HigherEducation => 300f,
            PlayerResource.Healthcare => 100f,
            PlayerResource.Garbage => 0.1f,
            PlayerResource.Electricity => 0.2f,
            PlayerResource.Water => 0.1f,
            _ => 0f,
        };
    }
}
