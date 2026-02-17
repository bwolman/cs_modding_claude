#define UNITY_ASSERTIONS
using System;
using Colossal.Collections;
using Game.Net;
using Game.Simulation.Flow;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation;

[BurstCompile]
public struct WaterPipeFlowJob : IJob
{
	public enum Phase
	{
		Initial,
		Producer,
		PostProducer,
		Trade,
		PostTrade,
		FluidFlow,
		Complete
	}

	public struct State
	{
		public Phase m_Phase;

		public int m_LastTotalSteps;

		public int m_StepCounter;

		public bool m_Error;

		public State(int lastTotalSteps)
		{
			this = default(State);
			m_Phase = Phase.Initial;
			m_LastTotalSteps = lastTotalSteps;
		}
	}

	public struct Data : IDisposable
	{
		public NativeReference<State> m_State;

		public NativeList<Game.Simulation.Flow.Node> m_Nodes;

		public NativeList<Game.Simulation.Flow.Edge> m_Edges;

		public NativeReference<MaxFlowSolverState> m_MaxFlowState;

		public NativeList<LayerState> m_LayerStates;

		public NativeList<CutElement> m_LayerElements;

		public NativeList<CutElementRef> m_LayerElementRefs;

		public NativeReference<FluidFlowSolverState> m_FluidFlowState;

		public Data(int lastTotalSteps, Allocator allocator)
		{
			m_State = new NativeReference<State>(new State(lastTotalSteps), allocator);
			m_Nodes = new NativeList<Game.Simulation.Flow.Node>(allocator);
			m_Edges = new NativeList<Game.Simulation.Flow.Edge>(allocator);
			m_MaxFlowState = new NativeReference<MaxFlowSolverState>(default(MaxFlowSolverState), allocator);
			m_LayerStates = new NativeList<LayerState>(allocator);
			m_LayerElements = new NativeList<CutElement>(allocator);
			m_LayerElementRefs = new NativeList<CutElementRef>(allocator);
			m_FluidFlowState = new NativeReference<FluidFlowSolverState>(default(FluidFlowSolverState), allocator);
		}

		public void Dispose()
		{
			m_State.Dispose();
			m_Nodes.Dispose();
			m_Edges.Dispose();
			m_MaxFlowState.Dispose();
			m_LayerStates.Dispose();
			m_LayerElements.Dispose();
			m_LayerElementRefs.Dispose();
			m_FluidFlowState.Dispose();
		}
	}

	private const int kShortageNodeLabel = -1;

	private const int kConnectedNodeLabel = -2;

	public const int kShortageEdgeLabel = -1;

	public const int kConnectedEdgeLabel = -2;

	private const int kSinkEdgeLabel = -200;

	public NativeReference<State> m_State;

	public NativeArray<Game.Simulation.Flow.Node> m_Nodes;

	public NativeArray<Game.Simulation.Flow.Edge> m_Edges;

	[ReadOnly]
	public NativeArray<Connection> m_Connections;

	[ReadOnly]
	public NativeReference<NodeIndices> m_NodeIndices;

	[ReadOnly]
	public NativeArray<int> m_TradeNodes;

	public NativeReference<MaxFlowSolverState> m_MaxFlowState;

	public NativeList<LayerState> m_LayerStates;

	public NativeList<CutElement> m_LayerElements;

	public NativeList<CutElementRef> m_LayerElementRefs;

	public NativeReference<FluidFlowSolverState> m_FluidFlowState;

	public int m_ImportCapacity;

	public int m_ExportCapacity;

	public bool m_FluidFlowEnabled;

	public int m_LayerHeight;

	public int m_FrameCount;

	public bool m_FinalFrame;

	public void Execute()
	{
		ref State reference = ref m_State.ValueAsRef();
		if (reference.m_Error)
		{
			if (m_FinalFrame)
			{
				UnityEngine.Debug.LogError($"Water pipe solver error in phase: {reference.m_Phase}");
				Finalize(ref reference);
			}
			return;
		}
		reference.m_Error = true;
		int num = math.max(100, reference.m_LastTotalSteps / 124);
		int num2 = reference.m_StepCounter + m_FrameCount * num;
		while (reference.m_Phase != Phase.Complete && (m_FinalFrame || reference.m_StepCounter < num2))
		{
			ExecutePhase(ref reference, num2);
		}
		reference.m_Error = false;
		if (m_FinalFrame)
		{
			Finalize(ref reference);
		}
	}

	private void Finalize(ref State state)
	{
		state.m_Phase = Phase.Initial;
		state.m_LastTotalSteps = state.m_StepCounter;
		state.m_StepCounter = 0;
		state.m_Error = false;
	}

	private void ExecutePhase(ref State state, int maxSteps)
	{
		if (state.m_Phase == Phase.Initial)
		{
			InitialPhase(ref state);
			return;
		}
		if (state.m_Phase == Phase.Producer)
		{
			MaxFlowPhase(ref state, maxSteps, Phase.PostProducer);
			return;
		}
		if (state.m_Phase == Phase.PostProducer)
		{
			PostProducerPhase(ref state);
			return;
		}
		if (state.m_Phase == Phase.Trade)
		{
			MaxFlowPhase(ref state, maxSteps, Phase.PostTrade);
			return;
		}
		if (state.m_Phase == Phase.PostTrade)
		{
			PostTradePhase(ref state);
			return;
		}
		if (state.m_Phase == Phase.FluidFlow)
		{
			FluidFlowPhase(ref state, maxSteps);
			return;
		}
		throw new Exception("Invalid phase");
	}

	private void InitialPhase(ref State state)
	{
		ResetMaxFlowState();
		state.m_Phase = Phase.Producer;
	}

	private void ResetMaxFlowState()
	{
		m_MaxFlowState.Value = default(MaxFlowSolverState);
		m_LayerStates.Length = 0;
		m_LayerElements.Length = 0;
		m_LayerElementRefs.Length = 0;
	}

	private void MaxFlowPhase(ref State state, int maxSteps, Phase phaseAfterCompletion)
	{
		NativeList<Game.Simulation.Flow.Layer> layers = new NativeList<Game.Simulation.Flow.Layer>(m_LayerStates.Capacity, Allocator.Temp);
		NativeQueue<int> labelQueue = new NativeQueue<int>(Allocator.Temp);
		NativeArray<UnsafeList<int>> activeQueue = new NativeArray<UnsafeList<int>>(m_LayerHeight, Allocator.Temp);
		for (int i = 0; i < m_LayerHeight; i++)
		{
			activeQueue[i] = new UnsafeList<int>(128, Allocator.Temp);
		}
		NodeIndices value = m_NodeIndices.Value;
		MaxFlowSolver maxFlowSolver = new MaxFlowSolver
		{
			m_LayerHeight = m_LayerHeight,
			m_SourceNode = value.m_SourceNode,
			m_SinkNode = value.m_SinkNode,
			m_Nodes = m_Nodes,
			m_Edges = m_Edges,
			m_Connections = m_Connections,
			m_Layers = layers,
			m_LabelQueue = labelQueue,
			m_ActiveQueue = activeQueue,
			m_StepCounter = state.m_StepCounter
		};
		if (m_MaxFlowState.Value.m_CurrentLabelVersion == 0)
		{
			maxFlowSolver.ResetNetwork();
			maxFlowSolver.InitializeState();
		}
		else
		{
			maxFlowSolver.LoadState(m_MaxFlowState, m_LayerStates, m_LayerElements, m_LayerElementRefs);
		}
		while (!maxFlowSolver.m_Complete && (m_FinalFrame || maxFlowSolver.m_StepCounter < maxSteps))
		{
			maxFlowSolver.SolveNextLayer();
		}
		maxFlowSolver.SaveState(m_MaxFlowState, m_LayerStates, m_LayerElements, m_LayerElementRefs);
		foreach (Game.Simulation.Flow.Layer item in layers)
		{
			item.Dispose();
		}
		layers.Dispose();
		foreach (UnsafeList<int> item2 in activeQueue)
		{
			item2.Dispose();
		}
		labelQueue.Dispose();
		activeQueue.Dispose();
		state.m_StepCounter = maxFlowSolver.m_StepCounter;
		if (maxFlowSolver.m_Complete)
		{
			state.m_Phase = phaseAfterCompletion;
		}
	}

	private void PostProducerPhase(ref State state)
	{
		LabelShortages(ref state);
		EnableTradeConnections();
		ResetMaxFlowState();
		state.m_Phase = Phase.Trade;
	}

	private void LabelShortages(ref State state)
	{
		NativeQueue<int> labelQueue = new NativeQueue<int>(Allocator.Temp);
		NodeIndices value = m_NodeIndices.Value;
		Game.Simulation.Flow.Node node = m_Nodes[value.m_SinkNode];
		for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
		{
			Connection connection = m_Connections[i];
			if (connection.m_EndNode != value.m_SourceNode)
			{
				if (m_Nodes[connection.m_EndNode].m_CutElementId.m_Version == -1)
				{
					m_Edges.ElementAt(connection.m_Edge).m_CutElementId = new Identifier(connection.m_EndNode, -1);
				}
				else if (connection.GetIncomingCapacity(m_Edges) > 0 && connection.GetIncomingResidualCapacity(m_Edges) > 0)
				{
					m_Edges.ElementAt(connection.m_Edge).m_CutElementId = new Identifier(connection.m_EndNode, -1);
					LabelShortageSubGraph(ref state, connection.m_EndNode, labelQueue);
				}
			}
		}
		labelQueue.Dispose();
	}

	private void LabelShortageSubGraph(ref State state, int initialNodeIndex, NativeQueue<int> labelQueue)
	{
		NodeIndices value = m_NodeIndices.Value;
		Assert.IsTrue(labelQueue.IsEmpty());
		Identifier identifier = new Identifier(initialNodeIndex, -1);
		Identifier cutElementId = new Identifier(initialNodeIndex, -1);
		m_Nodes.ElementAt(initialNodeIndex).m_CutElementId = identifier;
		labelQueue.Enqueue(initialNodeIndex);
		int item;
		while (labelQueue.TryDequeue(out item))
		{
			state.m_StepCounter++;
			Game.Simulation.Flow.Node node = m_Nodes[item];
			Assert.AreEqual(identifier, node.m_CutElementId);
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				int endNode = connection.m_EndNode;
				if (connection.GetIncomingCapacity(m_Edges) <= 0)
				{
					continue;
				}
				m_Edges.ElementAt(connection.m_Edge).m_CutElementId = cutElementId;
				if (endNode != value.m_SinkNode && endNode != value.m_SourceNode)
				{
					ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(endNode);
					if (reference.m_CutElementId.m_Version != -1 && connection.GetIncomingResidualCapacity(m_Edges) > 0)
					{
						reference.m_CutElementId = identifier;
						labelQueue.Enqueue(endNode);
					}
				}
			}
		}
	}

	private void EnableTradeConnections()
	{
		NodeIndices value = m_NodeIndices.Value;
		foreach (int tradeNode in m_TradeNodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[tradeNode];
			bool flag = node.m_CutElementId.m_Version == -1;
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(connection.m_Edge);
				if (connection.m_EndNode == value.m_SourceNode)
				{
					Assert.IsTrue(connection.m_Backwards);
					reference.m_Capacity = (flag ? m_ImportCapacity : 0);
				}
				else if (connection.m_EndNode == value.m_SinkNode)
				{
					Assert.IsFalse(connection.m_Backwards);
					reference.m_Capacity = ((!flag) ? m_ExportCapacity : 0);
				}
			}
		}
	}

	private void PostTradePhase(ref State state)
	{
		SetTradeConnectionsEnabled(import: true, export: false);
		LabelConnected(ref state);
		LabelShortages(ref state);
		if (m_FluidFlowEnabled)
		{
			FluidFlowSolver.ResetNodes(m_Nodes);
			LimitImportEdgeCapacity();
			PreflowSinkEdges();
			ResetNonSinkEdges();
			m_FluidFlowState.Value = default(FluidFlowSolverState);
			state.m_Phase = Phase.FluidFlow;
		}
		else
		{
			FinalizeFlows();
			state.m_Phase = Phase.Complete;
		}
	}

	private void SetTradeConnectionsEnabled(bool import, bool export)
	{
		NodeIndices value = m_NodeIndices.Value;
		foreach (int tradeNode in m_TradeNodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[tradeNode];
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(connection.m_Edge);
				if (connection.m_EndNode == value.m_SourceNode)
				{
					Assert.IsTrue(connection.m_Backwards);
					reference.m_Capacity = (import ? m_ImportCapacity : 0);
				}
				else if (connection.m_EndNode == value.m_SinkNode)
				{
					Assert.IsFalse(connection.m_Backwards);
					reference.m_Capacity = (export ? m_ExportCapacity : 0);
				}
			}
		}
	}

	private void LabelConnected(ref State state)
	{
		NativeQueue<int> labelQueue = new NativeQueue<int>(Allocator.Temp);
		NodeIndices value = m_NodeIndices.Value;
		Game.Simulation.Flow.Node node = m_Nodes[value.m_SourceNode];
		for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
		{
			Connection connection = m_Connections[i];
			if (m_Nodes[connection.m_EndNode].m_CutElementId.m_Version == -2)
			{
				m_Edges.ElementAt(connection.m_Edge).m_CutElementId = new Identifier(connection.m_EndNode, -2);
			}
			else if (connection.GetOutgoingCapacity(m_Edges) > 0)
			{
				m_Edges.ElementAt(connection.m_Edge).m_CutElementId = new Identifier(connection.m_EndNode, -2);
				LabelConnectedSubGraph(ref state, connection.m_EndNode, labelQueue);
			}
		}
		labelQueue.Dispose();
	}

	private void LabelConnectedSubGraph(ref State state, int initialNodeIndex, NativeQueue<int> labelQueue)
	{
		NodeIndices value = m_NodeIndices.Value;
		Assert.IsTrue(labelQueue.IsEmpty());
		Identifier identifier = new Identifier(initialNodeIndex, -2);
		Identifier cutElementId = new Identifier(initialNodeIndex, -2);
		m_Nodes.ElementAt(initialNodeIndex).m_CutElementId = identifier;
		labelQueue.Enqueue(initialNodeIndex);
		int item;
		while (labelQueue.TryDequeue(out item))
		{
			state.m_StepCounter++;
			Game.Simulation.Flow.Node node = m_Nodes[item];
			Assert.AreEqual(identifier, node.m_CutElementId);
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				int endNode = connection.m_EndNode;
				if (connection.GetOutgoingCapacity(m_Edges) <= 0 && endNode != value.m_SinkNode)
				{
					continue;
				}
				m_Edges.ElementAt(connection.m_Edge).m_CutElementId = cutElementId;
				if (endNode != value.m_SinkNode && endNode != value.m_SourceNode)
				{
					ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(endNode);
					if (reference.m_CutElementId.m_Version != -2)
					{
						reference.m_CutElementId = identifier;
						labelQueue.Enqueue(endNode);
					}
				}
			}
		}
	}

	private void LimitImportEdgeCapacity()
	{
		NodeIndices value = m_NodeIndices.Value;
		foreach (int tradeNode in m_TradeNodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[tradeNode];
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				if (connection.m_EndNode == value.m_SourceNode)
				{
					ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(connection.m_Edge);
					Assert.IsTrue(reference.flow >= 0);
					reference.m_Capacity = reference.flow;
				}
			}
		}
	}

	private void PreflowSinkEdges()
	{
		NodeIndices value = m_NodeIndices.Value;
		ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(value.m_SinkNode);
		for (int i = reference.m_FirstConnection; i < reference.m_LastConnection; i++)
		{
			Connection connection = m_Connections[i];
			ref Game.Simulation.Flow.Edge reference2 = ref m_Edges.ElementAt(connection.m_Edge);
			Assert.IsTrue(reference2.flow >= 0);
			reference2.m_Direction = FlowDirection.None;
			reference2.m_CutElementId = new Identifier(-200, reference2.m_CutElementId.m_Version);
			reference2.FinalizeTempFlow();
			m_Nodes.ElementAt(connection.m_EndNode).m_Excess = reference2.flow;
			reference.m_Excess -= reference2.flow;
		}
	}

	private void ResetNonSinkEdges()
	{
		for (int i = 0; i < m_Edges.Length; i++)
		{
			ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(i);
			if (reference.m_CutElementId.m_Index != -200)
			{
				reference.m_FinalFlow = 0;
				reference.m_TempFlow = 0;
			}
		}
	}

	private void FinalizeFlows()
	{
		for (int i = 0; i < m_Edges.Length; i++)
		{
			m_Edges.ElementAt(i).FinalizeTempFlow();
		}
	}

	private void FluidFlowPhase(ref State state, int maxSteps)
	{
		NodeIndices value = m_NodeIndices.Value;
		FluidFlowSolver fluidFlowSolver = new FluidFlowSolver
		{
			m_SourceNode = value.m_SourceNode,
			m_SinkNode = value.m_SinkNode,
			m_Nodes = m_Nodes,
			m_Edges = m_Edges,
			m_Connections = m_Connections,
			m_LabelQueue = new NativeMinHeap<LabelHeapData>(256, Allocator.Temp),
			m_PushQueue = new NativeMinHeap<PushHeapData>(2048, Allocator.Temp),
			m_StepCounter = state.m_StepCounter
		};
		if (m_FluidFlowState.Value.m_CurrentVersion == 0)
		{
			fluidFlowSolver.InitializeState();
		}
		else
		{
			fluidFlowSolver.LoadState(m_FluidFlowState);
		}
		while (!fluidFlowSolver.m_Complete && (m_FinalFrame || fluidFlowSolver.m_StepCounter < maxSteps))
		{
			fluidFlowSolver.SolveStep();
		}
		fluidFlowSolver.SaveState(m_FluidFlowState);
		fluidFlowSolver.m_LabelQueue.Dispose();
		fluidFlowSolver.m_PushQueue.Dispose();
		state.m_StepCounter = fluidFlowSolver.m_StepCounter;
		if (fluidFlowSolver.m_Complete)
		{
			state.m_Phase = Phase.Complete;
		}
	}
}
