using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct ObjectDefinition : IComponentData, IQueryTypeParameter
{
	public float3 m_Position;

	public float3 m_LocalPosition;

	public float3 m_Scale;

	public quaternion m_Rotation;

	public quaternion m_LocalRotation;

	public float m_Elevation;

	public float m_Intensity;

	public float m_Age;

	public int m_ParentMesh;

	public int m_GroupIndex;

	public int m_Probability;

	public int m_PrefabSubIndex;
}
