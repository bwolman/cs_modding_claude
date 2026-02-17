using System;

namespace Game.Prefabs;

[Serializable]
public struct IndustrialProcess
{
	public ResourceStackInEditor m_Input1;

	public ResourceStackInEditor m_Input2;

	public ResourceStackInEditor m_Output;

	public float m_MaxWorkersPerCell;
}
