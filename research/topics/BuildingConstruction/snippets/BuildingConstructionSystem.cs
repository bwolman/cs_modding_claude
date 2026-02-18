using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BuildingConstructionSystem : GameSystemBase
{
	[BurstCompile]
	private struct BuildingConstructionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<UnderConstruction> m_UnderConstructionType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Crane> m_CraneData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<CraneData> m_PrefabCraneData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<MeshBatch> m_MeshBatches;

		[ReadOnly]
		public BufferLookup<MeshColor> m_MeshColors;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		[ReadOnly]
		public BufferLookup<ColorVariation> m_PrefabColorVariations;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PointOfInterest> m_PointOfInterest;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public bool m_DebugFastSpawn;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public ParallelWriter m_CommandBuffer;

		public ParallelWriter<Entity, Entity> m_PreviousPrefabMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_0174: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			//IL_018f: Unknown result type (might be due to invalid IL or missing references)
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0168: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Entity> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray(m_EntityType);
			NativeArray<UnderConstruction> nativeArray2 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<UnderConstruction>(ref m_UnderConstructionType);
			NativeArray<Transform> nativeArray3 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Transform>(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray4 = ((ArchetypeChunk)(ref chunk)).GetNativeArray<PrefabRef>(ref m_PrefabRefType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity val = nativeArray[i];
				Transform transform = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				ref UnderConstruction reference = ref CollectionUtils.ElementAt<UnderConstruction>(nativeArray2, i);
				if (reference.m_Progress < 100)
				{
					if (reference.m_Speed == 0)
					{
						reference.m_Speed = (byte)((Random)(ref random)).NextInt(39, 89);
					}
					if (m_DebugFastSpawn)
					{
						reference.m_Progress = 100;
						continue;
					}
					if (reference.m_Progress == 0)
					{
						reference.m_Progress++;
						UpdateCranes(ref random, val, transform, prefabRef);
						continue;
					}
					uint num = (m_SimulationFrame >> 6) + reference.m_Speed;
					uint num2 = (uint)((ulong)((long)num * (long)reference.m_Speed) >> 7);
					uint num3 = (uint)((ulong)((long)(num + 1) * (long)reference.m_Speed) >> 7);
					reference.m_Progress = (byte)math.min(255, (int)(reference.m_Progress + (num3 - num2)));
					if (((Random)(ref random)).NextInt(10) == 0)
					{
						UpdateCranes(ref random, val, transform, prefabRef);
					}
				}
				else
				{
					if (reference.m_NewPrefab == Entity.Null)
					{
						reference.m_NewPrefab = prefabRef.m_Prefab;
					}
					UpdatePrefab(unfilteredChunkIndex, val, reference.m_NewPrefab, transform, ref random, ref selectedSpawnables);
					((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<UnderConstruction>(unfilteredChunkIndex, val);
					m_PreviousPrefabMap.TryAdd(val, prefabRef.m_Prefab);
				}
			}
			if (selectedSpawnables.IsCreated)
			{
				selectedSpawnables.Dispose();
			}
		}

		private void UpdateCranes(ref Random random, Entity entity, Transform transform, PrefabRef prefabRef)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_0160: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_017f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0151: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			DynamicBuffer<Game.Objects.SubObject> val = default(DynamicBuffer<Game.Objects.SubObject>);
			if (!m_SubObjects.TryGetBuffer(entity, ref val))
			{
				return;
			}
			CraneData craneData = default(CraneData);
			for (int i = 0; i < val.Length; i++)
			{
				Entity subObject = val[i].m_SubObject;
				if (!m_CraneData.HasComponent(subObject))
				{
					continue;
				}
				Transform transform2 = m_TransformData[subObject];
				PrefabRef prefabRef2 = m_PrefabRefData[subObject];
				float3 position = ((Random)(ref random)).NextFloat3(objectGeometryData.m_Bounds.min, objectGeometryData.m_Bounds.max);
				position = ObjectUtils.LocalToWorld(transform, position);
				if (m_PrefabCraneData.TryGetComponent(prefabRef2.m_Prefab, ref craneData))
				{
					position = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(transform2), position);
					float num = math.length(((float3)(ref position)).xz);
					if (num < craneData.m_DistanceRange.min)
					{
						((float3)(ref position)).xz = math.normalizesafe(((float3)(ref position)).xz, new float2(0f, 1f)) * craneData.m_DistanceRange.min;
					}
					else if (num > craneData.m_DistanceRange.max)
					{
						((float3)(ref position)).xz = math.normalizesafe(((float3)(ref position)).xz, new float2(0f, 1f)) * craneData.m_DistanceRange.max;
					}
					position = ObjectUtils.LocalToWorld(transform2, position);
				}
				PointOfInterest pointOfInterest = m_PointOfInterest[subObject];
				pointOfInterest.m_Position = position;
				pointOfInterest.m_IsValid = true;
				m_PointOfInterest[subObject] = pointOfInterest;
			}
		}

		private void UpdatePrefab(int jobIndex, Entity entity, Entity newPrefab, Transform transform, ref Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0190: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0441: Unknown result type (might be due to invalid IL or missing references)
			//IL_0411: Unknown result type (might be due to invalid IL or missing references)
			//IL_0412: Unknown result type (might be due to invalid IL or missing references)
			//IL_0401: Unknown result type (might be due to invalid IL or missing references)
			//IL_0402: Unknown result type (might be due to invalid IL or missing references)
			//IL_0247: Unknown result type (might be due to invalid IL or missing references)
			//IL_041a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0423: Unknown result type (might be due to invalid IL or missing references)
			//IL_0424: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0393: Unknown result type (might be due to invalid IL or missing references)
			//IL_0269: Unknown result type (might be due to invalid IL or missing references)
			//IL_026e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0276: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0283: Unknown result type (might be due to invalid IL or missing references)
			//IL_0288: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_029b: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0318: Unknown result type (might be due to invalid IL or missing references)
			//IL_0349: Unknown result type (might be due to invalid IL or missing references)
			//IL_032d: Unknown result type (might be due to invalid IL or missing references)
			//IL_035e: Unknown result type (might be due to invalid IL or missing references)
			((ParallelWriter)(ref m_CommandBuffer)).SetComponent<PrefabRef>(jobIndex, entity, new PrefabRef(newPrefab));
			((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, entity, default(Updated));
			DynamicBuffer<MeshBatch> val = default(DynamicBuffer<MeshBatch>);
			if (m_MeshBatches.TryGetBuffer(entity, ref val))
			{
				DynamicBuffer<MeshBatch> val2 = ((ParallelWriter)(ref m_CommandBuffer)).SetBuffer<MeshBatch>(jobIndex, entity);
				val2.ResizeUninitialized(val.Length);
				for (int i = 0; i < val.Length; i++)
				{
					MeshBatch meshBatch = val[i];
					meshBatch.m_MeshGroup = 255;
					meshBatch.m_MeshIndex = 255;
					meshBatch.m_TileIndex = 255;
					val2[i] = meshBatch;
				}
			}
			bool flag = false;
			DynamicBuffer<SubMesh> val3 = default(DynamicBuffer<SubMesh>);
			if (m_PrefabSubMeshes.TryGetBuffer(newPrefab, ref val3))
			{
				for (int j = 0; j < val3.Length; j++)
				{
					if (m_PrefabColorVariations.HasBuffer(val3[j].m_SubMesh))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag != m_MeshColors.HasBuffer(entity))
			{
				if (flag)
				{
					((ParallelWriter)(ref m_CommandBuffer)).AddBuffer<MeshColor>(jobIndex, entity);
				}
				else
				{
					((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<MeshColor>(jobIndex, entity);
				}
			}
			DynamicBuffer<Game.Objects.SubObject> val4 = default(DynamicBuffer<Game.Objects.SubObject>);
			if (m_SubObjects.TryGetBuffer(entity, ref val4))
			{
				for (int k = 0; k < val4.Length; k++)
				{
					Entity subObject = val4[k].m_SubObject;
					((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, subObject, default(Updated));
				}
			}
			DynamicBuffer<Game.Areas.SubArea> val5 = default(DynamicBuffer<Game.Areas.SubArea>);
			if (m_SubAreas.TryGetBuffer(entity, ref val5))
			{
				for (int l = 0; l < val5.Length; l++)
				{
					((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Deleted>(jobIndex, val5[l].m_Area);
				}
			}
			DynamicBuffer<Game.Prefabs.SubArea> subAreas = default(DynamicBuffer<Game.Prefabs.SubArea>);
			if (m_PrefabSubAreas.TryGetBuffer(newPrefab, ref subAreas))
			{
				if (!val5.IsCreated)
				{
					((ParallelWriter)(ref m_CommandBuffer)).AddBuffer<Game.Areas.SubArea>(jobIndex, entity);
				}
				if (selectedSpawnables.IsCreated)
				{
					selectedSpawnables.Clear();
				}
				CreateAreas(jobIndex, entity, transform, subAreas, m_PrefabSubAreaNodes[newPrefab], ref random, ref selectedSpawnables);
			}
			else if (val5.IsCreated)
			{
				((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<Game.Areas.SubArea>(jobIndex, entity);
			}
			DynamicBuffer<Game.Net.SubNet> val6 = default(DynamicBuffer<Game.Net.SubNet>);
			if (m_SubNets.TryGetBuffer(entity, ref val6))
			{
				DynamicBuffer<ConnectedEdge> val7 = default(DynamicBuffer<ConnectedEdge>);
				Owner owner = default(Owner);
				for (int m = 0; m < val6.Length; m++)
				{
					Game.Net.SubNet subNet = val6[m];
					bool flag2 = true;
					if (m_ConnectedEdges.TryGetBuffer(subNet.m_SubNet, ref val7))
					{
						for (int n = 0; n < val7.Length; n++)
						{
							Entity edge = val7[n].m_Edge;
							if ((!m_OwnerData.TryGetComponent(edge, ref owner) || (!(owner.m_Owner == entity) && !m_DeletedData.HasComponent(owner.m_Owner))) && !m_DeletedData.HasComponent(edge))
							{
								Edge edge2 = m_EdgeData[edge];
								if (edge2.m_Start == subNet.m_SubNet || edge2.m_End == subNet.m_SubNet)
								{
									flag2 = false;
								}
								((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, edge, default(Updated));
								if (!m_DeletedData.HasComponent(edge2.m_Start))
								{
									((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, edge2.m_Start, default(Updated));
								}
								if (!m_DeletedData.HasComponent(edge2.m_End))
								{
									((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, edge2.m_End, default(Updated));
								}
							}
						}
					}
					if (flag2)
					{
						((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Deleted>(jobIndex, subNet.m_SubNet);
						continue;
					}
					((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<Owner>(jobIndex, subNet.m_SubNet);
					((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, subNet.m_SubNet, default(Updated));
				}
			}
			if (m_PrefabSubNets.HasBuffer(newPrefab))
			{
				if (val6.IsCreated)
				{
					((ParallelWriter)(ref m_CommandBuffer)).SetBuffer<Game.Net.SubNet>(jobIndex, entity);
				}
				else
				{
					((ParallelWriter)(ref m_CommandBuffer)).AddBuffer<Game.Net.SubNet>(jobIndex, entity);
				}
				CreateNets(jobIndex, entity, transform, m_PrefabSubNets[newPrefab], ref random);
			}
			else if (val6.IsCreated)
			{
				((ParallelWriter)(ref m_CommandBuffer)).RemoveComponent<Game.Net.SubNet>(jobIndex, entity);
			}
		}

		private void CreateAreas(int jobIndex, Entity owner, Transform transform, DynamicBuffer<Game.Prefabs.SubArea> subAreas, DynamicBuffer<SubAreaNode> subAreaNodes, ref Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_018f: Unknown result type (might be due to invalid IL or missing references)
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			DynamicBuffer<PlaceholderObjectElement> placeholderElements = default(DynamicBuffer<PlaceholderObjectElement>);
			for (int i = 0; i < subAreas.Length; i++)
			{
				Game.Prefabs.SubArea subArea = subAreas[i];
				int seed;
				if (m_PrefabPlaceholderElements.TryGetBuffer(subArea.m_Prefab, ref placeholderElements))
				{
					if (!selectedSpawnables.IsCreated)
					{
						selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, AllocatorHandle.op_Implicit((Allocator)2));
					}
					if (!AreaUtils.SelectAreaPrefab(placeholderElements, m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
					{
						continue;
					}
				}
				else
				{
					seed = ((Random)(ref random)).NextInt();
				}
				Entity val = ((ParallelWriter)(ref m_CommandBuffer)).CreateEntity(jobIndex);
				CreationDefinition creationDefinition = new CreationDefinition
				{
					m_Prefab = subArea.m_Prefab,
					m_Owner = owner,
					m_RandomSeed = seed
				};
				creationDefinition.m_Flags |= CreationFlags.Permanent;
				((ParallelWriter)(ref m_CommandBuffer)).AddComponent<CreationDefinition>(jobIndex, val, creationDefinition);
				((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, val, default(Updated));
				DynamicBuffer<Game.Areas.Node> val2 = ((ParallelWriter)(ref m_CommandBuffer)).AddBuffer<Game.Areas.Node>(jobIndex, val);
				val2.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
				int num = ObjectToolBaseSystem.GetFirstNodeIndex(subAreaNodes, subArea.m_NodeRange);
				int num2 = 0;
				for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
				{
					float3 position = subAreaNodes[num].m_Position;
					float3 position2 = ObjectUtils.LocalToWorld(transform, position);
					int parentMesh = subAreaNodes[num].m_ParentMesh;
					float elevation = math.select(-3.4028235E+38f, position.y, parentMesh >= 0);
					val2[num2] = new Game.Areas.Node(position2, elevation);
					num2++;
					if (++num == subArea.m_NodeRange.y)
					{
						num = subArea.m_NodeRange.x;
					}
				}
			}
		}

		private void CreateNets(int jobIndex, Entity owner, Transform transform, DynamicBuffer<Game.Prefabs.SubNet> subNets, ref Random random)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_014b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_018d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0194: Unknown result type (might be due to invalid IL or missing references)
			//IL_019b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
			NativeList<float4> nodePositions = default(NativeList<float4>);
			nodePositions..ctor(subNets.Length * 2, AllocatorHandle.op_Implicit((Allocator)2));
			for (int i = 0; i < subNets.Length; i++)
			{
				Game.Prefabs.SubNet subNet = subNets[i];
				if (subNet.m_NodeIndex.x >= 0)
				{
					while (nodePositions.Length <= subNet.m_NodeIndex.x)
					{
						float4 val = default(float4);
						nodePositions.Add(ref val);
					}
					ref NativeList<float4> reference = ref nodePositions;
					int x = subNet.m_NodeIndex.x;
					reference[x] += new float4(subNet.m_Curve.a, 1f);
				}
				if (subNet.m_NodeIndex.y >= 0)
				{
					while (nodePositions.Length <= subNet.m_NodeIndex.y)
					{
						float4 val = default(float4);
						nodePositions.Add(ref val);
					}
					ref NativeList<float4> reference = ref nodePositions;
					int x = subNet.m_NodeIndex.y;
					reference[x] += new float4(subNet.m_Curve.d, 1f);
				}
			}
			for (int j = 0; j < nodePositions.Length; j++)
			{
				ref NativeList<float4> reference = ref nodePositions;
				int x = j;
				reference[x] /= math.max(1f, nodePositions[j].w);
			}
			for (int k = 0; k < subNets.Length; k++)
			{
				Game.Prefabs.SubNet subNet2 = NetUtils.GetSubNet(subNets, k, m_LefthandTraffic, ref m_PrefabNetGeometryData);
				CreateSubNet(jobIndex, subNet2.m_Prefab, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, owner, transform, ref random);
			}
			nodePositions.Dispose();
		}

		private void CreateSubNet(int jobIndex, Entity netPrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, Entity owner, Transform transform, ref Random random)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			//IL_014e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_0160: Unknown result type (might be due to invalid IL or missing references)
			//IL_0165: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_0194: Unknown result type (might be due to invalid IL or missing references)
			//IL_019e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0251: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0286: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
			Entity val = ((ParallelWriter)(ref m_CommandBuffer)).CreateEntity(jobIndex);
			CreationDefinition creationDefinition = new CreationDefinition
			{
				m_Prefab = netPrefab,
				m_Owner = owner,
				m_RandomSeed = ((Random)(ref random)).NextInt()
			};
			creationDefinition.m_Flags |= CreationFlags.Permanent;
			((ParallelWriter)(ref m_CommandBuffer)).AddComponent<CreationDefinition>(jobIndex, val, creationDefinition);
			((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Updated>(jobIndex, val, default(Updated));
			NetCourse netCourse = default(NetCourse);
			netCourse.m_Curve = ObjectUtils.LocalToWorld(transform.m_Position, transform.m_Rotation, curve);
			netCourse.m_StartPosition.m_Position = netCourse.m_Curve.a;
			netCourse.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(netCourse.m_Curve), transform.m_Rotation);
			netCourse.m_StartPosition.m_CourseDelta = 0f;
			netCourse.m_StartPosition.m_Elevation = float2.op_Implicit(curve.a.y);
			netCourse.m_StartPosition.m_ParentMesh = parentMesh.x;
			float4 val2;
			if (nodeIndex.x >= 0)
			{
				ref CoursePos startPosition = ref netCourse.m_StartPosition;
				float3 position = transform.m_Position;
				quaternion rotation = transform.m_Rotation;
				val2 = nodePositions[nodeIndex.x];
				startPosition.m_Position = ObjectUtils.LocalToWorld(position, rotation, ((float4)(ref val2)).xyz);
			}
			netCourse.m_EndPosition.m_Position = netCourse.m_Curve.d;
			netCourse.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(netCourse.m_Curve), transform.m_Rotation);
			netCourse.m_EndPosition.m_CourseDelta = 1f;
			netCourse.m_EndPosition.m_Elevation = float2.op_Implicit(curve.d.y);
			netCourse.m_EndPosition.m_ParentMesh = parentMesh.y;
			if (nodeIndex.y >= 0)
			{
				ref CoursePos endPosition = ref netCourse.m_EndPosition;
				float3 position2 = transform.m_Position;
				quaternion rotation2 = transform.m_Rotation;
				val2 = nodePositions[nodeIndex.y];
				endPosition.m_Position = ObjectUtils.LocalToWorld(position2, rotation2, ((float4)(ref val2)).xyz);
			}
			netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
			netCourse.m_FixedIndex = -1;
			netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.DisableMerge;
			netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsLast | CoursePosFlags.DisableMerge;
			if (((float3)(ref netCourse.m_StartPosition.m_Position)).Equals(netCourse.m_EndPosition.m_Position))
			{
				netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			((ParallelWriter)(ref m_CommandBuffer)).AddComponent<NetCourse>(jobIndex, val, netCourse);
			if (upgrades != default(CompositionFlags))
			{
				Upgraded upgraded = new Upgraded
				{
					m_Flags = upgrades
				};
				((ParallelWriter)(ref m_CommandBuffer)).AddComponent<Upgraded>(jobIndex, val, upgraded);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<UnderConstruction> __Game_Objects_UnderConstruction_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Crane> __Game_Objects_Crane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CraneData> __Game_Prefabs_CraneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshBatch> __Game_Rendering_MeshBatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshColor> __Game_Rendering_MeshColor_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ColorVariation> __Game_Prefabs_ColorVariation_RO_BufferLookup;

		public ComponentLookup<PointOfInterest> __Game_Common_PointOfInterest_RW_ComponentLookup;

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
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_012d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			__Unity_Entities_Entity_TypeHandle = ((SystemState)(ref state)).GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Transform>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PrefabRef>(true);
			__Game_Objects_UnderConstruction_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<UnderConstruction>(false);
			__Game_Common_Owner_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Owner>(true);
			__Game_Common_Deleted_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Deleted>(true);
			__Game_Objects_Crane_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Crane>(true);
			__Game_Objects_Transform_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Transform>(true);
			__Game_Net_Edge_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Edge>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PrefabRef>(true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SpawnableObjectData>(true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ObjectGeometryData>(true);
			__Game_Prefabs_CraneData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<CraneData>(true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<NetGeometryData>(true);
			__Game_Objects_SubObject_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Objects.SubObject>(true);
			__Game_Areas_SubArea_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Areas.SubArea>(true);
			__Game_Net_SubNet_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Net.SubNet>(true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<ConnectedEdge>(true);
			__Game_Rendering_MeshBatch_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<MeshBatch>(true);
			__Game_Rendering_MeshColor_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<MeshColor>(true);
			__Game_Prefabs_SubArea_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Prefabs.SubArea>(true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<SubAreaNode>(true);
			__Game_Prefabs_SubNet_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Prefabs.SubNet>(true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<PlaceholderObjectElement>(true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<SubMesh>(true);
			__Game_Prefabs_ColorVariation_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<ColorVariation>(true);
			__Game_Common_PointOfInterest_RW_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PointOfInterest>(false);
		}
	}

	private const int UPDATE_INTERVAL_BITS = 6;

	public const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private ZoneSpawnSystem m_ZoneSpawnSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_BuildingQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_SimulationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<TerrainSystem>();
		m_ZoneSpawnSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ZoneSpawnSystem>();
		m_CityConfigurationSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_EndFrameBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_BuildingQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[5]
		{
			ComponentType.ReadOnly<UnderConstruction>(),
			ComponentType.ReadOnly<Building>(),
			ComponentType.Exclude<Destroyed>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Temp>()
		});
		((ComponentSystemBase)this).RequireForUpdate(m_BuildingQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		BuildingConstructionJob buildingConstructionJob = new BuildingConstructionJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle<Transform>(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_UnderConstructionType = InternalCompilerInterface.GetComponentTypeHandle<UnderConstruction>(ref __TypeHandle.__Game_Objects_UnderConstruction_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup<Owner>(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup<Deleted>(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_CraneData = InternalCompilerInterface.GetComponentLookup<Crane>(ref __TypeHandle.__Game_Objects_Crane_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup<Transform>(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup<Edge>(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup<SpawnableObjectData>(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabCraneData = InternalCompilerInterface.GetComponentLookup<CraneData>(ref __TypeHandle.__Game_Prefabs_CraneData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup<NetGeometryData>(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup<Game.Objects.SubObject>(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup<Game.Areas.SubArea>(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup<Game.Net.SubNet>(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup<ConnectedEdge>(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_MeshBatches = InternalCompilerInterface.GetBufferLookup<MeshBatch>(ref __TypeHandle.__Game_Rendering_MeshBatch_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_MeshColors = InternalCompilerInterface.GetBufferLookup<MeshColor>(ref __TypeHandle.__Game_Rendering_MeshColor_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup<Game.Prefabs.SubArea>(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup<SubAreaNode>(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup<Game.Prefabs.SubNet>(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup<PlaceholderObjectElement>(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup<SubMesh>(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabColorVariations = InternalCompilerInterface.GetBufferLookup<ColorVariation>(ref __TypeHandle.__Game_Prefabs_ColorVariation_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PointOfInterest = InternalCompilerInterface.GetComponentLookup<PointOfInterest>(ref __TypeHandle.__Game_Common_PointOfInterest_RW_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_DebugFastSpawn = m_ZoneSpawnSystem.debugFastSpawn,
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next()
		};
		EntityCommandBuffer val = m_EndFrameBarrier.CreateCommandBuffer();
		buildingConstructionJob.m_CommandBuffer = ((EntityCommandBuffer)(ref val)).AsParallelWriter();
		buildingConstructionJob.m_PreviousPrefabMap = m_TerrainSystem.GetBuildingUpgradeWriter(((EntityQuery)(ref m_BuildingQuery)).CalculateEntityCountWithoutFiltering());
		JobHandle val2 = JobChunkExtensions.ScheduleParallel<BuildingConstructionJob>(buildingConstructionJob, m_BuildingQuery, ((SystemBase)this).Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(val2);
		m_TerrainSystem.SetBuildingUpgradeWriterDependency(val2);
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
	public BuildingConstructionSystem()
	{
	}
}
