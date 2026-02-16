using Unity.Mathematics;

namespace Game.Simulation;

public struct WaterSourceCache
{
	public int m_ConstantDepth;

	public float3 m_Position;

	public float m_Polluted;

	public float m_Multiplier;

	public float m_Radius;

	public float m_Height;
}
