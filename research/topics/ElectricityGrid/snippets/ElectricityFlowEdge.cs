using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Simulation;

public struct ElectricityFlowEdge : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Index;

	public Entity m_Start;

	public Entity m_End;

	public int m_Capacity;

	public int m_Flow;

	public ElectricityFlowEdgeFlags m_Flags;

	public FlowDirection direction
	{
		get => (FlowDirection)(m_Flags & ElectricityFlowEdgeFlags.ForwardBackward);
		set
		{
			m_Flags &= ~ElectricityFlowEdgeFlags.ForwardBackward;
			m_Flags |= (ElectricityFlowEdgeFlags)value;
		}
	}

	public bool isBottleneck => (m_Flags & ElectricityFlowEdgeFlags.Bottleneck) != 0;
	public bool isBeyondBottleneck => (m_Flags & ElectricityFlowEdgeFlags.BeyondBottleneck) != 0;
	public bool isDisconnected => (m_Flags & ElectricityFlowEdgeFlags.Disconnected) != 0;
}

[Flags]
public enum ElectricityFlowEdgeFlags : byte
{
	None = 0,
	Forward = 1,
	Backward = 2,
	Bottleneck = 4,
	BeyondBottleneck = 8,
	Disconnected = 0x10,
	ForwardBackward = 3
}
