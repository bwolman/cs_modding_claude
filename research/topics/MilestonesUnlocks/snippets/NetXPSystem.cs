using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NetXPSystem : GameSystemBase
{
	[BurstCompile]
	private struct NetXPJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PlaceableNetDatas;

		[ReadOnly]
		public ComponentLookup<Elevation> m_Elevations;

		[ReadOnly]
		public ComponentLookup<RoadData> m_RoadDatas;

		[ReadOnly]
		public ComponentLookup<TrackData> m_TrackDatas;

		[ReadOnly]
		public ComponentLookup<WaterwayData> m_WaterwayDatas;

		[ReadOnly]
		public ComponentLookup<PipelineData> m_PipelineDatas;

		[ReadOnly]
		public ComponentLookup<PowerLineData> m_PowerLineDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<Edge> m_Edges;

		[ReadOnly]
		public ComponentLookup<Curve> m_Curves;

		[ReadOnly]
		public NativeArray<Entity> m_CreatedEntities;

		[ReadOnly]
		public NativeArray<Entity> m_DeletedEntities;

		public NativeQueue<XPGain> m_XPQueue;

		private float GetElevationBonus(Edge edge, ComponentLookup<Elevation> elevations, bool isRoad)
		{
			if (!isRoad || ((!elevations.TryGetComponent(edge.m_Start, out var componentData) || !(componentData.m_Elevation.x > 0f) || !(componentData.m_Elevation.y > 0f)) && (!elevations.TryGetComponent(edge.m_End, out componentData) || !(componentData.m_Elevation.x > 0f) || !(componentData.m_Elevation.y > 0f))))
			{
				return 0f;
			}
			return 1f;
		}

		private NetXPs CountXP(ref NativeArray<Entity> entities)
		{
			NetXPs result = default(NetXPs);
			for (int i = 0; i < entities.Length; i++)
			{
				Entity entity = entities[i];
				Entity prefab = m_PrefabRefs[entity].m_Prefab;
				if (!m_PlaceableNetDatas.TryGetComponent(prefab, out var componentData) || componentData.m_XPReward <= 0)
				{
					continue;
				}
				float num = ((float)componentData.m_XPReward + GetElevationBonus(m_Edges[entity], m_Elevations, m_RoadDatas.HasComponent(prefab))) * m_Curves[entity].m_Length / kXPRewardLength;
				if (m_RoadDatas.HasComponent(prefab))
				{
					result.m_Roads += num;
				}
				else if (m_TrackDatas.HasComponent(prefab))
				{
					TrackData trackData = m_TrackDatas[prefab];
					if (trackData.m_TrackType == TrackTypes.Train)
					{
						result.m_Trains += num;
					}
					else if (trackData.m_TrackType == TrackTypes.Tram)
					{
						result.m_Trams += num;
					}
					else if (trackData.m_TrackType == TrackTypes.Subway)
					{
						result.m_Subways += num;
					}
				}
				else if (m_WaterwayDatas.HasComponent(prefab))
				{
					result.m_Waterways += num;
				}
				else if (m_PipelineDatas.HasComponent(prefab))
				{
					result.m_Pipes += num;
				}
				else if (m_PowerLineDatas.HasComponent(prefab))
				{
					result.m_Powerlines += num;
				}
			}
			return result;
		}

		public void Execute()
		{
			NetXPs netXPs = CountXP(ref m_CreatedEntities);
			if (m_DeletedEntities.Length > 0)
			{
				netXPs -= CountXP(ref m_DeletedEntities);
			}
			netXPs.GetMaxValue(out var max, out var reason);
			int num = Mathf.FloorToInt(max);
			if (num > 0)
			{
				m_XPQueue.Enqueue(new XPGain
				{
					amount = num,
					entity = Entity.Null,
					reason = reason
				});
			}
		}
	}

	private struct NetXPs
	{
		public float m_Roads;

		public float m_Trains;

		public float m_Trams;

		public float m_Subways;

		public float m_Waterways;

		public float m_Pipes;

		public float m_Powerlines;

		public void Clear()
		{
			m_Roads = 0f;
			m_Trains = 0f;
			m_Trams = 0f;
			m_Subways = 0f;
			m_Waterways = 0f;
			m_Pipes = 0f;
			m_Powerlines = 0f;
		}

		public static NetXPs operator +(NetXPs a, NetXPs b)
		{
			return new NetXPs
			{
				m_Roads = a.m_Roads + b.m_Roads,
				m_Trains = a.m_Trains + b.m_Trains,
				m_Trams = a.m_Trams + b.m_Trams,
				m_Subways = a.m_Subways + b.m_Subways,
				m_Waterways = a.m_Waterways + b.m_Waterways,
				m_Pipes = a.m_Pipes + b.m_Pipes,
				m_Powerlines = a.m_Powerlines + b.m_Powerlines
			};
		}

		public static NetXPs operator -(NetXPs a, NetXPs b)
		{
			return new NetXPs
			{
				m_Roads = a.m_Roads - b.m_Roads,
				m_Trains = a.m_Trains - b.m_Trains,
				m_Trams = a.m_Trams - b.m_Trams,
				m_Subways = a.m_Subways - b.m_Subways,
				m_Waterways = a.m_Waterways - b.m_Waterways,
				m_Pipes = a.m_Pipes - b.m_Pipes,
				m_Powerlines = a.m_Powerlines - b.m_Powerlines
			};
		}

		public void GetMaxValue(out float max, out XPReason reason)
		{
			reason = XPReason.Unknown;
			max = 0f;
			Check(m_Roads, XPReason.Road, ref max, ref reason);
			Check(m_Trains, XPReason.TrainTrack, ref max, ref reason);
			Check(m_Trams, XPReason.TramTrack, ref max, ref reason);
			Check(m_Subways, XPReason.SubwayTrack, ref max, ref reason);
			Check(m_Waterways, XPReason.Waterway, ref max, ref reason);
			Check(m_Pipes, XPReason.Pipe, ref max, ref reason);
			Check(m_Powerlines, XPReason.PowerLine, ref max, ref reason);
		}

		private void Check(float value, XPReason reason, ref float max, ref XPReason maxReason)
		{
			if (value > max)
			{
				max = value;
				maxReason = reason;
			}
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RW_ComponentLookup;

		public ComponentLookup<Elevation> __Game_Net_Elevation_RW_ComponentLookup;

		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RW_ComponentLookup;

		public ComponentLookup<TrackData> __Game_Prefabs_TrackData_RW_ComponentLookup;

		public ComponentLookup<WaterwayData> __Game_Prefabs_WaterwayData_RW_ComponentLookup;

		public ComponentLookup<PipelineData> __Game_Prefabs_PipelineData_RW_ComponentLookup;

		public ComponentLookup<PowerLineData> __Game_Prefabs_PowerLineData_RW_ComponentLookup;

		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentLookup;

		public ComponentLookup<Edge> __Game_Net_Edge_RW_ComponentLookup;

		public ComponentLookup<Curve> __Game_Net_Curve_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PlaceableNetData_RW_ComponentLookup = state.GetComponentLookup<PlaceableNetData>();
			__Game_Net_Elevation_RW_ComponentLookup = state.GetComponentLookup<Elevation>();
			__Game_Prefabs_RoadData_RW_ComponentLookup = state.GetComponentLookup<RoadData>();
			__Game_Prefabs_TrackData_RW_ComponentLookup = state.GetComponentLookup<TrackData>();
			__Game_Prefabs_WaterwayData_RW_ComponentLookup = state.GetComponentLookup<WaterwayData>();
			__Game_Prefabs_PipelineData_RW_ComponentLookup = state.GetComponentLookup<PipelineData>();
			__Game_Prefabs_PowerLineData_RW_ComponentLookup = state.GetComponentLookup<PowerLineData>();
			__Game_Prefabs_PrefabRef_RW_ComponentLookup = state.GetComponentLookup<PrefabRef>();
			__Game_Net_Edge_RW_ComponentLookup = state.GetComponentLookup<Edge>();
			__Game_Net_Curve_RW_ComponentLookup = state.GetComponentLookup<Curve>();
		}
	}

	private static readonly float kXPRewardLength = 112f;

	private XPSystem m_XPSystem;

	private EntityQuery m_CreatedNetQuery;

	private EntityQuery m_DeletedNetQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CreatedNetQuery = GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Curve>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_DeletedNetQuery = GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Curve>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Created>(), ComponentType.Exclude<Temp>());
		m_XPSystem = base.World.GetOrCreateSystemManaged<XPSystem>();
		RequireForUpdate(m_CreatedNetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<Entity> nativeList = m_CreatedNetQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle outJobHandle2;
		NativeList<Entity> nativeList2 = m_DeletedNetQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle2);
		JobHandle deps;
		NetXPJob jobData = new NetXPJob
		{
			m_PlaceableNetDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Elevations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RoadDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TrackDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WaterwayDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterwayData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PipelineDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PipelineData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PowerLineDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PowerLineData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Curves = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CreatedEntities = nativeList.AsDeferredJobArray(),
			m_DeletedEntities = nativeList2.AsDeferredJobArray(),
			m_XPQueue = m_XPSystem.GetQueue(out deps)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(outJobHandle, outJobHandle2, JobHandle.CombineDependencies(deps, base.Dependency)));
		m_XPSystem.AddQueueWriter(base.Dependency);
		nativeList.Dispose(base.Dependency);
		nativeList2.Dispose(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public NetXPSystem()
	{
	}
}
