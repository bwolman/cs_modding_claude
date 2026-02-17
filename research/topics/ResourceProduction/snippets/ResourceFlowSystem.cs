using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ResourceFlowSystem : GameSystemBase
{
	private struct SourceNodeData
	{
		public int m_Flow;

		public Entity m_Node;
	}

	public struct TargetDirectionData
	{
		public Entity m_Node;

		public Entity m_Edge;

		public int2 m_Direction;
	}

	private struct ResourceNodeItem : ILessThan<ResourceNodeItem>
	{
		public float m_Distance;

		public Entity m_Node;

		public TargetDirectionData m_Target;

		public bool LessThan(ResourceNodeItem other)
		{
			return m_Distance < other.m_Distance;
		}
	}

	[BurstCompile]
	private struct ResourceFlowJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Object> m_ObjectType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgrade> m_ServiceUpgradeType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[NativeDisableContainerSafetyRestriction]
		public ComponentTypeHandle<ResourceConnection> m_ResourceConnectionType;

		[NativeDisableContainerSafetyRestriction]
		public ComponentLookup<ResourceConnection> m_ResourceConnectionData;

		public void Execute()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_027a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0298: Unknown result type (might be due to invalid IL or missing references)
			//IL_029d: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02be: Unknown result type (might be due to invalid IL or missing references)
			//IL_022e: Unknown result type (might be due to invalid IL or missing references)
			//IL_023c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0789: Unknown result type (might be due to invalid IL or missing references)
			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_079c: Unknown result type (might be due to invalid IL or missing references)
			//IL_07a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_07b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_07bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0301: Unknown result type (might be due to invalid IL or missing references)
			//IL_0315: Unknown result type (might be due to invalid IL or missing references)
			//IL_0329: Unknown result type (might be due to invalid IL or missing references)
			//IL_032e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0333: Unknown result type (might be due to invalid IL or missing references)
			//IL_0348: Unknown result type (might be due to invalid IL or missing references)
			//IL_034f: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			//IL_0156: Unknown result type (might be due to invalid IL or missing references)
			//IL_0370: Unknown result type (might be due to invalid IL or missing references)
			//IL_0377: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_07fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0809: Unknown result type (might be due to invalid IL or missing references)
			//IL_0810: Unknown result type (might be due to invalid IL or missing references)
			//IL_081c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0821: Unknown result type (might be due to invalid IL or missing references)
			//IL_0826: Unknown result type (might be due to invalid IL or missing references)
			//IL_082f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0834: Unknown result type (might be due to invalid IL or missing references)
			//IL_083d: Unknown result type (might be due to invalid IL or missing references)
			//IL_03db: Unknown result type (might be due to invalid IL or missing references)
			//IL_0866: Unknown result type (might be due to invalid IL or missing references)
			//IL_086b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0870: Unknown result type (might be due to invalid IL or missing references)
			//IL_084d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0852: Unknown result type (might be due to invalid IL or missing references)
			//IL_0485: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03af: Unknown result type (might be due to invalid IL or missing references)
			//IL_049c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0428: Unknown result type (might be due to invalid IL or missing references)
			//IL_042d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0440: Unknown result type (might be due to invalid IL or missing references)
			//IL_0445: Unknown result type (might be due to invalid IL or missing references)
			//IL_044e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0453: Unknown result type (might be due to invalid IL or missing references)
			//IL_045d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0469: Unknown result type (might be due to invalid IL or missing references)
			//IL_046e: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_04dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0503: Unknown result type (might be due to invalid IL or missing references)
			//IL_050c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0519: Unknown result type (might be due to invalid IL or missing references)
			//IL_051e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0548: Unknown result type (might be due to invalid IL or missing references)
			//IL_061d: Unknown result type (might be due to invalid IL or missing references)
			//IL_055f: Unknown result type (might be due to invalid IL or missing references)
			//IL_05a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_05a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_05c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_05cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_05dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_0649: Unknown result type (might be due to invalid IL or missing references)
			//IL_065c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0673: Unknown result type (might be due to invalid IL or missing references)
			//IL_0686: Unknown result type (might be due to invalid IL or missing references)
			//IL_068b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0692: Unknown result type (might be due to invalid IL or missing references)
			//IL_0697: Unknown result type (might be due to invalid IL or missing references)
			//IL_069c: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_06df: Unknown result type (might be due to invalid IL or missing references)
			//IL_06e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_06f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_06fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0705: Unknown result type (might be due to invalid IL or missing references)
			//IL_070a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0713: Unknown result type (might be due to invalid IL or missing references)
			//IL_071a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0721: Unknown result type (might be due to invalid IL or missing references)
			//IL_0726: Unknown result type (might be due to invalid IL or missing references)
			//IL_072b: Unknown result type (might be due to invalid IL or missing references)
			NativeList<SourceNodeData> val = default(NativeList<SourceNodeData>);
			val..ctor(10, AllocatorHandle.op_Implicit((Allocator)2));
			NativeHashMap<Entity, TargetDirectionData> val2 = default(NativeHashMap<Entity, TargetDirectionData>);
			val2..ctor(100, AllocatorHandle.op_Implicit((Allocator)2));
			NativeMinHeap<ResourceNodeItem> val3 = default(NativeMinHeap<ResourceNodeItem>);
			val3..ctor(10, (Allocator)2);
			Owner owner = default(Owner);
			Transform transform = default(Transform);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk val4 = m_Chunks[i];
				NativeArray<Node> nativeArray = ((ArchetypeChunk)(ref val4)).GetNativeArray<Node>(ref m_NodeType);
				NativeArray<ResourceConnection> nativeArray2 = ((ArchetypeChunk)(ref val4)).GetNativeArray<ResourceConnection>(ref m_ResourceConnectionType);
				if (nativeArray.Length != 0)
				{
					NativeArray<Entity> nativeArray3 = ((ArchetypeChunk)(ref val4)).GetNativeArray(m_EntityType);
					NativeArray<Owner> nativeArray4 = ((ArchetypeChunk)(ref val4)).GetNativeArray<Owner>(ref m_OwnerType);
					bool flag = ((ArchetypeChunk)(ref val4)).Has<ServiceUpgrade>(ref m_ServiceUpgradeType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						ref ResourceConnection reference = ref CollectionUtils.ElementAt<ResourceConnection>(nativeArray2, j);
						if (reference.m_Flow.y >> 1 != 0)
						{
							SourceNodeData sourceNodeData = new SourceNodeData
							{
								m_Flow = reference.m_Flow.y >> 1,
								m_Node = nativeArray3[j]
							};
							val.Add(ref sourceNodeData);
						}
						else if (!flag && CollectionUtils.TryGet<Owner>(nativeArray4, j, ref owner) && !m_ServiceUpgradeData.HasComponent(owner.m_Owner) && m_TransformData.TryGetComponent(owner.m_Owner, ref transform))
						{
							val3.Insert(new ResourceNodeItem
							{
								m_Node = nativeArray3[j],
								m_Distance = math.distancesq(transform.m_Position, nativeArray[j].m_Position)
							});
						}
						reference.m_Flow = default(int2);
					}
				}
				else if (((ArchetypeChunk)(ref val4)).Has<Object>(ref m_ObjectType))
				{
					NativeArray<Entity> nativeArray5 = ((ArchetypeChunk)(ref val4)).GetNativeArray(m_EntityType);
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						ref ResourceConnection reference2 = ref CollectionUtils.ElementAt<ResourceConnection>(nativeArray2, k);
						if (reference2.m_Flow.y >> 1 != 0)
						{
							SourceNodeData sourceNodeData = new SourceNodeData
							{
								m_Flow = reference2.m_Flow.y >> 1,
								m_Node = nativeArray5[k]
							};
							val.Add(ref sourceNodeData);
						}
						reference2.m_Flow = default(int2);
					}
				}
				else
				{
					for (int l = 0; l < nativeArray2.Length; l++)
					{
						CollectionUtils.ElementAt<ResourceConnection>(nativeArray2, l).m_Flow = default(int2);
					}
				}
			}
			RefRW<ResourceConnection> refRW;
			DynamicBuffer<ConnectedEdge> val5 = default(DynamicBuffer<ConnectedEdge>);
			bool3 val7 = default(bool3);
			DynamicBuffer<SubObject> val8 = default(DynamicBuffer<SubObject>);
			Transform transform2 = default(Transform);
			float num4 = default(float);
			while (val3.Length != 0)
			{
				ResourceNodeItem resourceNodeItem = val3.Extract();
				if (!val2.TryAdd(resourceNodeItem.m_Node, resourceNodeItem.m_Target))
				{
					continue;
				}
				refRW = m_ResourceConnectionData.GetRefRW(resourceNodeItem.m_Node);
				refRW.ValueRW.m_Flow.y = 1;
				if (!m_ConnectedEdges.TryGetBuffer(resourceNodeItem.m_Node, ref val5))
				{
					continue;
				}
				for (int m = 0; m < val5.Length; m++)
				{
					ConnectedEdge connectedEdge = val5[m];
					if (!m_ResourceConnectionData.HasComponent(connectedEdge.m_Edge))
					{
						continue;
					}
					Edge edge = m_EdgeData[connectedEdge.m_Edge];
					Curve curve = m_CurveData[connectedEdge.m_Edge];
					DynamicBuffer<ConnectedNode> val6 = m_ConnectedNodes[connectedEdge.m_Edge];
					float num = 0.5f;
					((bool3)(ref val7))..ctor(false, true, false);
					if (edge.m_Start == resourceNodeItem.m_Node)
					{
						num = 0f;
						((bool3)(ref val7))..ctor(true, false, false);
					}
					else if (edge.m_End == resourceNodeItem.m_Node)
					{
						num = 1f;
						((bool3)(ref val7))..ctor(false, false, true);
					}
					else
					{
						for (int n = 0; n < val6.Length; n++)
						{
							ConnectedNode connectedNode = val6[n];
							if (connectedNode.m_Node == resourceNodeItem.m_Node)
							{
								num = connectedNode.m_CurvePosition;
								break;
							}
						}
					}
					if (!val2.ContainsKey(edge.m_Start) && m_ResourceConnectionData.HasComponent(edge.m_Start))
					{
						val3.Insert(new ResourceNodeItem
						{
							m_Distance = resourceNodeItem.m_Distance + curve.m_Length * num,
							m_Node = edge.m_Start,
							m_Target = new TargetDirectionData
							{
								m_Node = resourceNodeItem.m_Node,
								m_Edge = connectedEdge.m_Edge,
								m_Direction = new int2(-1, math.select(-1, 0, val7.y))
							}
						});
					}
					if (!val2.ContainsKey(edge.m_End) && m_ResourceConnectionData.HasComponent(edge.m_End))
					{
						val3.Insert(new ResourceNodeItem
						{
							m_Distance = resourceNodeItem.m_Distance + curve.m_Length * (1f - num),
							m_Node = edge.m_End,
							m_Target = new TargetDirectionData
							{
								m_Node = resourceNodeItem.m_Node,
								m_Edge = connectedEdge.m_Edge,
								m_Direction = new int2(math.select(1, 0, val7.y), 1)
							}
						});
					}
					for (int num2 = 0; num2 < val6.Length; num2++)
					{
						ConnectedNode connectedNode2 = val6[num2];
						if (!val2.ContainsKey(connectedNode2.m_Node) && m_ResourceConnectionData.HasComponent(connectedNode2.m_Node))
						{
							val3.Insert(new ResourceNodeItem
							{
								m_Distance = resourceNodeItem.m_Distance + curve.m_Length * math.abs(connectedNode2.m_CurvePosition - num),
								m_Node = connectedNode2.m_Node,
								m_Target = new TargetDirectionData
								{
									m_Node = resourceNodeItem.m_Node,
									m_Edge = connectedEdge.m_Edge,
									m_Direction = math.select(new int2(0, 0), new int2(1, -1), ((bool3)(ref val7)).xz)
								}
							});
						}
					}
					if (!m_SubObjects.TryGetBuffer(connectedEdge.m_Edge, ref val8))
					{
						continue;
					}
					for (int num3 = 0; num3 < val8.Length; num3++)
					{
						SubObject subObject = val8[num3];
						if (m_ResourceConnectionData.HasComponent(subObject.m_SubObject) && !val2.ContainsKey(subObject.m_SubObject) && m_TransformData.TryGetComponent(subObject.m_SubObject, ref transform2))
						{
							MathUtils.Distance(new Segment(curve.m_Bezier.a, curve.m_Bezier.d), transform2.m_Position, ref num4);
							val3.Insert(new ResourceNodeItem
							{
								m_Distance = resourceNodeItem.m_Distance + curve.m_Length * math.abs(num4 - num),
								m_Node = subObject.m_SubObject,
								m_Target = new TargetDirectionData
								{
									m_Node = resourceNodeItem.m_Node,
									m_Edge = connectedEdge.m_Edge,
									m_Direction = math.select(new int2(0, 0), new int2(1, -1), ((bool3)(ref val7)).xz)
								}
							});
						}
					}
				}
			}
			TargetDirectionData targetDirectionData = default(TargetDirectionData);
			for (int num5 = 0; num5 < val.Length; num5++)
			{
				SourceNodeData sourceNodeData2 = val[num5];
				if (!val2.TryGetValue(sourceNodeData2.m_Node, ref targetDirectionData) || targetDirectionData.m_Edge == Entity.Null)
				{
					continue;
				}
				refRW = m_ResourceConnectionData.GetRefRW(sourceNodeData2.m_Node);
				refRW.ValueRW.m_Flow.x += sourceNodeData2.m_Flow;
				for (int num6 = 0; num6 < 10000; num6++)
				{
					refRW = m_ResourceConnectionData.GetRefRW(targetDirectionData.m_Edge);
					ref int2 flow = ref refRW.ValueRW.m_Flow;
					flow += targetDirectionData.m_Direction * sourceNodeData2.m_Flow;
					sourceNodeData2.m_Node = targetDirectionData.m_Node;
					if (!val2.TryGetValue(sourceNodeData2.m_Node, ref targetDirectionData) || targetDirectionData.m_Edge == Entity.Null)
					{
						refRW = m_ResourceConnectionData.GetRefRW(sourceNodeData2.m_Node);
						refRW.ValueRW.m_Flow.x -= sourceNodeData2.m_Flow;
						break;
					}
				}
			}
			val.Dispose();
			val2.Dispose();
			val3.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Object> __Game_Objects_Object_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		public ComponentTypeHandle<ResourceConnection> __Game_Net_ResourceConnection_RW_ComponentTypeHandle;

		public ComponentLookup<ResourceConnection> __Game_Net_ResourceConnection_RW_ComponentLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Net_Node_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Node>(true);
			__Game_Common_Owner_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Owner>(true);
			__Game_Objects_Object_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Object>(true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ServiceUpgrade>(true);
			__Game_Net_Edge_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Edge>(true);
			__Game_Net_Curve_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Curve>(true);
			__Game_Objects_Transform_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Transform>(true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ServiceUpgrade>(true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<ConnectedEdge>(true);
			__Game_Net_ConnectedNode_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<ConnectedNode>(true);
			__Game_Objects_SubObject_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<SubObject>(true);
			__Game_Net_ResourceConnection_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<ResourceConnection>(false);
			__Game_Net_ResourceConnection_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ResourceConnection>(false);
		}
	}

	private ExtractorCompanySystem m_ExtractorCompanySystem;

	private EntityQuery m_NetQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / EconomyUtils.kCompanyUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_ExtractorCompanySystem = ((ComponentSystemBase)this).World.GetExistingSystemManaged<ExtractorCompanySystem>();
		m_NetQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadWrite<ResourceConnection>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		((ComponentSystemBase)this).RequireForUpdate(m_NetQuery);
		Assert.AreEqual(GetUpdateInterval(SystemUpdatePhase.GameSimulation), m_ExtractorCompanySystem.GetUpdateInterval(SystemUpdatePhase.GameSimulation) * 16);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		JobHandle val = default(JobHandle);
		ResourceFlowJob resourceFlowJob = new ResourceFlowJob
		{
			m_Chunks = ((EntityQuery)(ref m_NetQuery)).ToArchetypeChunkListAsync(AllocatorHandle.op_Implicit((Allocator)3), ref val),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle<Node>(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle<Owner>(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ObjectType = InternalCompilerInterface.GetComponentTypeHandle<Object>(ref __TypeHandle.__Game_Objects_Object_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle<ServiceUpgrade>(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup<Edge>(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup<Curve>(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup<Transform>(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup<ServiceUpgrade>(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup<ConnectedEdge>(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup<ConnectedNode>(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup<SubObject>(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ResourceConnectionType = InternalCompilerInterface.GetComponentTypeHandle<ResourceConnection>(ref __TypeHandle.__Game_Net_ResourceConnection_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_ResourceConnectionData = InternalCompilerInterface.GetComponentLookup<ResourceConnection>(ref __TypeHandle.__Game_Net_ResourceConnection_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef)
		};
		JobHandle val2 = IJobExtensions.Schedule<ResourceFlowJob>(resourceFlowJob, JobHandle.CombineDependencies(((SystemBase)this).Dependency, val));
		resourceFlowJob.m_Chunks.Dispose(val2);
		((SystemBase)this).Dependency = val2;
	}

	[MethodImpl((MethodImplOptions)256)]
	private void __AssignQueries(ref SystemState state)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		EntityQueryBuilder val = default(EntityQueryBuilder);
		((EntityQueryBuilder)(ref val))..ctor(AllocatorHandle.op_Implicit((Allocator)2));
		((EntityQueryBuilder)(ref val)).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		((ComponentSystemBase)this).OnCreateForCompiler();
		__AssignQueries(ref ((SystemBase)this).CheckedStateRef);
		__TypeHandle.__AssignHandles(ref ((SystemBase)this).CheckedStateRef);
	}

	[Preserve]
	public ResourceFlowSystem()
	{
	}
}
