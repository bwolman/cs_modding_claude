using Unity.Mathematics;

namespace Game.Simulation;

public struct SurfaceWater
{
	public float m_Depth;

	public float m_Polluted;

	public float2 m_Velocity;

	public SurfaceWater(float4 data)
	{
		m_Depth = math.max(data.x, 0f);
		m_Polluted = data.w;
		m_Velocity = new float2(data.y, data.z);
	}
}
