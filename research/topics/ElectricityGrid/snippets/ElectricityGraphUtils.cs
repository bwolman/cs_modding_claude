using Game.Common;
using Game.Net;
using Unity.Entities;

namespace Game.Simulation;

public static class ElectricityGraphUtils
{
	public static bool HasAnyFlowEdge(Entity node1, Entity node2,
		ref BufferLookup<ConnectedFlowEdge> flowConnections,
		ref ComponentLookup<ElectricityFlowEdge> flowEdges);

	public static bool TryGetFlowEdge(Entity startNode, Entity endNode,
		ref BufferLookup<ConnectedFlowEdge> flowConnections,
		ref ComponentLookup<ElectricityFlowEdge> flowEdges,
		out Entity entity, out ElectricityFlowEdge edge);

	public static bool TrySetFlowEdge(Entity startNode, Entity endNode,
		FlowDirection direction, int capacity,
		ref BufferLookup<ConnectedFlowEdge> flowConnections,
		ref ComponentLookup<ElectricityFlowEdge> flowEdges);

	public static Entity CreateFlowEdge(EntityCommandBuffer commandBuffer,
		EntityArchetype edgeArchetype, Entity startNode, Entity endNode,
		FlowDirection direction, int capacity);

	public static Entity CreateFlowEdge(EntityManager entityManager,
		EntityArchetype edgeArchetype, Entity startNode, Entity endNode,
		FlowDirection direction, int capacity);

	public static void DeleteFlowNode(EntityCommandBuffer.ParallelWriter commandBuffer,
		int jobIndex, Entity node,
		ref BufferLookup<ConnectedFlowEdge> flowConnections);

	public static void DeleteFlowNode(EntityManager entityManager, Entity node);

	public static void DeleteBuildingNodes(EntityCommandBuffer.ParallelWriter commandBuffer,
		int jobIndex, ElectricityBuildingConnection connection,
		ref BufferLookup<ConnectedFlowEdge> flowConnections,
		ref ComponentLookup<ElectricityFlowEdge> flowEdges);
}
