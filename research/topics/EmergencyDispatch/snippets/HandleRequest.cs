// Decompiled from Game.dll -> Game.Simulation.HandleRequest
using Unity.Entities;

namespace Game.Simulation;

public struct HandleRequest : IComponentData, IQueryTypeParameter
{
    public Entity m_Request;
    public Entity m_Handler;
    public bool m_Completed;
    public bool m_PathConsumed;

    public HandleRequest(Entity request, Entity handler, bool completed, bool pathConsumed = false)
    {
        m_Request = request;
        m_Handler = handler;
        m_Completed = completed;
        m_PathConsumed = pathConsumed;
    }
}
