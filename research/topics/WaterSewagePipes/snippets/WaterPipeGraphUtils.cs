#define UNITY_ASSERTIONS
using Game.Common;
using Unity.Assertions;
using Unity.Entities;

namespace Game.Simulation;

public static class WaterPipeGraphUtils
{
	public static bool HasAnyFlowEdge(Entity node1, Entity node2, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<WaterPipeEdge> flowEdges)
	{
		Assert.IsTrue(node1.Index > 0);
		Assert.IsTrue(node2.Index > 0);
		foreach (ConnectedFlowEdge item in flowConnections[node1])
		{
			WaterPipeEdge waterPipeEdge = flowEdges[item.m_Edge];
			if ((waterPipeEdge.m_Start == node1 && waterPipeEdge.m_End == node2) || (waterPipeEdge.m_Start == node2 && waterPipeEdge.m_End == node1))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetFlowEdge(Entity startNode, Entity endNode, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<WaterPipeEdge> flowEdges, out Entity entity)
	{
		WaterPipeEdge edge;
		return TryGetFlowEdge(startNode, endNode, ref flowConnections, ref flowEdges, out entity, out edge);
	}

	public static bool TryGetFlowEdge(Entity startNode, Entity endNode, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<WaterPipeEdge> flowEdges, out WaterPipeEdge edge)
	{
		Entity entity;
		return TryGetFlowEdge(startNode, endNode, ref flowConnections, ref flowEdges, out entity, out edge);
	}

	public static bool TryGetFlowEdge(Entity startNode, Entity endNode, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<WaterPipeEdge> flowEdges, out Entity entity, out WaterPipeEdge edge)
	{
		Assert.IsTrue(startNode.Index > 0);
		Assert.IsTrue(endNode.Index > 0);
		foreach (ConnectedFlowEdge item in flowConnections[startNode])
		{
			entity = item.m_Edge;
			edge = flowEdges[entity];
			if (edge.m_Start == startNode && edge.m_End == endNode)
			{
				return true;
			}
		}
		entity = default(Entity);
		edge = default(WaterPipeEdge);
		return false;
	}

	public static bool TrySetFlowEdge(Entity startNode, Entity endNode, int freshCapacity, int sewageCapacity, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<WaterPipeEdge> flowEdges)
	{
		if (TryGetFlowEdge(startNode, endNode, ref flowConnections, ref flowEdges, out var entity, out var edge))
		{
			edge.m_FreshCapacity = freshCapacity;
			edge.m_SewageCapacity = sewageCapacity;
			flowEdges[entity] = edge;
			return true;
		}
		return false;
	}

	public static Entity CreateFlowEdge(EntityCommandBuffer commandBuffer, EntityArchetype edgeArchetype, Entity startNode, Entity endNode, int freshCapacity, int sewageCapacity)
	{
		Assert.AreNotEqual(startNode, Entity.Null);
		Assert.AreNotEqual(endNode, Entity.Null);
		Entity entity = commandBuffer.CreateEntity(edgeArchetype);
		commandBuffer.SetComponent(entity, new WaterPipeEdge
		{
			m_Start = startNode,
			m_End = endNode,
			m_FreshCapacity = freshCapacity,
			m_SewageCapacity = sewageCapacity
		});
		commandBuffer.AppendToBuffer(startNode, new ConnectedFlowEdge(entity));
		commandBuffer.AppendToBuffer(endNode, new ConnectedFlowEdge(entity));
		return entity;
	}

	public static Entity CreateFlowEdge(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, EntityArchetype edgeArchetype, Entity startNode, Entity endNode, int freshCapacity, int sewageCapacity)
	{
		Assert.AreNotEqual(startNode, Entity.Null);
		Assert.AreNotEqual(endNode, Entity.Null);
		Entity entity = commandBuffer.CreateEntity(jobIndex, edgeArchetype);
		commandBuffer.SetComponent(jobIndex, entity, new WaterPipeEdge
		{
			m_Start = startNode,
			m_End = endNode,
			m_FreshCapacity = freshCapacity,
			m_SewageCapacity = sewageCapacity
		});
		commandBuffer.AppendToBuffer(jobIndex, startNode, new ConnectedFlowEdge(entity));
		commandBuffer.AppendToBuffer(jobIndex, endNode, new ConnectedFlowEdge(entity));
		return entity;
	}

	public static Entity CreateFlowEdge(EntityManager entityManager, EntityArchetype edgeArchetype, Entity startNode, Entity endNode, int freshCapacity, int sewageCapacity)
	{
		Assert.AreNotEqual(startNode, Entity.Null);
		Assert.AreNotEqual(endNode, Entity.Null);
		Entity entity = entityManager.CreateEntity(edgeArchetype);
		entityManager.SetComponentData(entity, new WaterPipeEdge
		{
			m_Start = startNode,
			m_End = endNode,
			m_FreshCapacity = freshCapacity,
			m_SewageCapacity = sewageCapacity
		});
		entityManager.GetBuffer<ConnectedFlowEdge>(startNode).Add(new ConnectedFlowEdge(entity));
		entityManager.GetBuffer<ConnectedFlowEdge>(endNode).Add(new ConnectedFlowEdge(entity));
		return entity;
	}

	public static void DeleteFlowNode(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, Entity node, ref BufferLookup<ConnectedFlowEdge> flowConnections)
	{
		commandBuffer.AddComponent<Deleted>(jobIndex, node);
		foreach (ConnectedFlowEdge item in flowConnections[node])
		{
			commandBuffer.AddComponent<Deleted>(jobIndex, item.m_Edge);
		}
	}

	public static void DeleteFlowNode(EntityManager entityManager, Entity node)
	{
		entityManager.AddComponent<Deleted>(node);
		foreach (ConnectedFlowEdge item in entityManager.GetBuffer<ConnectedFlowEdge>(node, isReadOnly: true))
		{
			entityManager.AddComponent<Deleted>(item.m_Edge);
		}
	}

	public static void DeleteBuildingNodes(EntityCommandBuffer.ParallelWriter commandBuffer, int jobIndex, WaterPipeBuildingConnection connection, ref BufferLookup<ConnectedFlowEdge> flowConnections, ref ComponentLookup<WaterPipeEdge> flowEdges)
	{
		if (connection.m_ProducerEdge != Entity.Null)
		{
			DeleteFlowNode(commandBuffer, jobIndex, connection.GetProducerNode(ref flowEdges), ref flowConnections);
		}
		if (connection.m_ConsumerEdge != Entity.Null)
		{
			DeleteFlowNode(commandBuffer, jobIndex, connection.GetConsumerNode(ref flowEdges), ref flowConnections);
		}
	}
}
