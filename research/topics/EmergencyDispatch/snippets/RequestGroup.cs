// Decompiled from Game.dll -> Game.Simulation.RequestGroup
using Unity.Entities;

namespace Game.Simulation;

public struct RequestGroup : IComponentData, IQueryTypeParameter
{
    public uint m_GroupCount;

    public RequestGroup(uint groupCount)
    {
        m_GroupCount = groupCount;
    }
}
