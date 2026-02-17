using System;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct ControlPoint : IEquatable<ControlPoint>
{
	public float3 m_Position;

	public float3 m_HitPosition;

	public float2 m_Direction;

	public float3 m_HitDirection;

	public quaternion m_Rotation;

	public Entity m_OriginalEntity;

	public float2 m_SnapPriority;

	public int2 m_ElementIndex;

	public float m_CurvePosition;

	public float m_Elevation;

	public ControlPoint(Entity raycastEntity, RaycastHit raycastHit)
	{
		m_Position = raycastHit.m_Position;
		m_HitPosition = raycastHit.m_HitPosition;
		m_Direction = default(float2);
		m_HitDirection = raycastHit.m_HitDirection;
		m_Rotation = quaternion.identity;
		m_OriginalEntity = raycastEntity;
		m_SnapPriority = default(float2);
		m_ElementIndex = raycastHit.m_CellIndex;
		m_CurvePosition = raycastHit.m_CurvePosition;
		m_Elevation = 0f;
	}

	public bool Equals(ControlPoint other)
	{
		if (m_Position.Equals(other.m_Position) && m_HitPosition.Equals(other.m_HitPosition) && m_Direction.Equals(other.m_Direction) && m_HitDirection.Equals(other.m_HitDirection) && m_Rotation.Equals(other.m_Rotation) && m_OriginalEntity.Equals(other.m_OriginalEntity) && m_SnapPriority.Equals(other.m_SnapPriority) && m_ElementIndex.Equals(other.m_ElementIndex) && m_CurvePosition == other.m_CurvePosition)
		{
			return m_Elevation == other.m_Elevation;
		}
		return false;
	}

	public bool EqualsIgnoreHit(ControlPoint other)
	{
		if (math.all(math.abs(m_Position - other.m_Position) < 0.001f) && math.all(math.abs(m_Direction - other.m_Direction) < 0.001f) && math.all(math.abs(m_Rotation.value - other.m_Rotation.value) < 0.001f) && math.abs(m_CurvePosition - other.m_CurvePosition) < 0.001f && math.abs(m_Elevation - other.m_Elevation) < 0.001f && m_OriginalEntity.Equals(other.m_OriginalEntity))
		{
			return m_ElementIndex.Equals(other.m_ElementIndex);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((17 * 31 + m_Position.GetHashCode()) * 31 + m_HitPosition.GetHashCode()) * 31 + m_Direction.GetHashCode()) * 31 + m_HitDirection.GetHashCode()) * 31 + m_Rotation.GetHashCode()) * 31 + m_OriginalEntity.GetHashCode()) * 31 + m_SnapPriority.GetHashCode()) * 31 + m_ElementIndex.GetHashCode()) * 31 + m_CurvePosition.GetHashCode()) * 31 + m_Elevation.GetHashCode();
	}
}
