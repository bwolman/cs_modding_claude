using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct WaterPipeBuildingConnection : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_ProducerEdge;

	public Entity m_ConsumerEdge;

	public Entity GetProducerNode(ref ComponentLookup<WaterPipeEdge> flowEdges)
	{
		return flowEdges[m_ProducerEdge].m_End;
	}

	public Entity GetConsumerNode(ref ComponentLookup<WaterPipeEdge> flowEdges)
	{
		return flowEdges[m_ConsumerEdge].m_Start;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity producerEdge = m_ProducerEdge;
		writer.Write(producerEdge);
		Entity consumerEdge = m_ConsumerEdge;
		writer.Write(consumerEdge);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity producerEdge = ref m_ProducerEdge;
		reader.Read(out producerEdge);
		ref Entity consumerEdge = ref m_ConsumerEdge;
		reader.Read(out consumerEdge);
	}
}
