using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct ElectricityBuildingConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TransformerNode;

	public Entity m_ProducerEdge;

	public Entity m_ConsumerEdge;

	public Entity m_ChargeEdge;

	public Entity m_DischargeEdge;

	public Entity GetProducerNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_ProducerEdge].m_End;
	}

	public Entity GetConsumerNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_ConsumerEdge].m_Start;
	}

	public Entity GetChargeNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_ChargeEdge].m_Start;
	}

	public Entity GetDischargeNode(ref ComponentLookup<ElectricityFlowEdge> flowEdges)
	{
		return flowEdges[m_DischargeEdge].m_End;
	}
}
