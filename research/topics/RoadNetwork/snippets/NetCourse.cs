// Source: Game.dll -> Game.Tools.NetCourse (decompiled with ilspycmd v9.1)
using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct NetCourse : IComponentData, IQueryTypeParameter
{
    public CoursePos m_StartPosition;

    public CoursePos m_EndPosition;

    public Bezier4x3 m_Curve;

    public float2 m_Elevation;

    public float m_Length;

    public int m_FixedIndex;
}

// Source: Game.dll -> Game.Tools.CoursePos
public struct CoursePos
{
    public Entity m_Entity;

    public float3 m_Position;

    public quaternion m_Rotation;

    public float2 m_Elevation;

    public float m_CourseDelta;

    public float m_SplitPosition;

    public CoursePosFlags m_Flags;

    public int m_ParentMesh;
}
