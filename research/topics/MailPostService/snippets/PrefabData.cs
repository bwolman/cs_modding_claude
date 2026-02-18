using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct PostFacilityData : IComponentData, IQueryTypeParameter, ICombineData<PostFacilityData>
{
	public int m_PostVanCapacity;

	public int m_PostTruckCapacity;

	public int m_MailCapacity;

	public int m_SortingRate;
}

public struct MailBoxData : IComponentData, IQueryTypeParameter
{
	public int m_MailCapacity;
}

public struct PostVanData : IComponentData, IQueryTypeParameter
{
	public int m_MailCapacity;
}

public struct MailAccumulationData : IComponentData, IQueryTypeParameter
{
	public bool m_RequireCollect;

	public float2 m_AccumulationRate;
}
