using System;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class DestroySystem : GameSystemBase
{
	[BurstCompile]
	private struct DestroyObjectsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Destroy> m_DestroyType;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<Clip> m_ClipAreas;

		[ReadOnly]
		public ComponentLookup<Space> m_SpaceAreas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<AreaData> m_AreaData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_MeshData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public BufferLookup<SubMesh> m_PrefabSubMeshes;

		public NativeHashSet<Entity> m_ProcessedObjects;

		public EntityCommandBuffer m_CommandBuffer;

		public NativeQueue<Entity> m_UpdatedElectricityRoadEdges;

		public NativeQueue<Entity> m_UpdatedWaterPipeRoadEdges;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_DestroyedBuildingComponents;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			NativeArray<Destroy> nativeArray = ((ArchetypeChunk)(ref chunk)).GetNativeArray<Destroy>(ref m_DestroyType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < ((ArchetypeChunk)(ref chunk)).Count; i++)
			{
				Destroy destroyEvent = nativeArray[i];
				DestroyObject(ref random, destroyEvent.m_Object, destroyEvent);
			}
		}

		private void DestroyObject(ref Random random, Entity entity, Destroy destroyEvent)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0417: Unknown result type (might be due to invalid IL or missing references)
			//IL_0447: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0479: Unknown result type (might be due to invalid IL or missing references)
			//IL_0485: Unknown result type (might be due to invalid IL or missing references)
			//IL_045c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0463: Unknown result type (might be due to invalid IL or missing references)
			//IL_0385: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0495: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_040d: Unknown result type (might be due to invalid IL or missing references)
			//IL_040e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_04da: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_039d: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			//IL_014e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_0160: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_017b: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0182: Unknown result type (might be due to invalid IL or missing references)
			//IL_0184: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_018b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_0198: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0512: Unknown result type (might be due to invalid IL or missing references)
			//IL_0517: Unknown result type (might be due to invalid IL or missing references)
			//IL_051f: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0205: Unknown result type (might be due to invalid IL or missing references)
			//IL_0207: Unknown result type (might be due to invalid IL or missing references)
			//IL_0218: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_052a: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_023d: Unknown result type (might be due to invalid IL or missing references)
			//IL_023f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0244: Unknown result type (might be due to invalid IL or missing references)
			//IL_0251: Unknown result type (might be due to invalid IL or missing references)
			//IL_0258: Unknown result type (might be due to invalid IL or missing references)
			//IL_025d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0262: Unknown result type (might be due to invalid IL or missing references)
			//IL_0267: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0226: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02be: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02da: Unknown result type (might be due to invalid IL or missing references)
			//IL_02df: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0300: Unknown result type (might be due to invalid IL or missing references)
			//IL_0302: Unknown result type (might be due to invalid IL or missing references)
			//IL_0304: Unknown result type (might be due to invalid IL or missing references)
			//IL_0306: Unknown result type (might be due to invalid IL or missing references)
			//IL_030d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0312: Unknown result type (might be due to invalid IL or missing references)
			//IL_0317: Unknown result type (might be due to invalid IL or missing references)
			//IL_0319: Unknown result type (might be due to invalid IL or missing references)
			//IL_0320: Unknown result type (might be due to invalid IL or missing references)
			//IL_0325: Unknown result type (might be due to invalid IL or missing references)
			//IL_032a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0330: Unknown result type (might be due to invalid IL or missing references)
			//IL_035b: Unknown result type (might be due to invalid IL or missing references)
			PrefabRef prefabRef = default(PrefabRef);
			if (m_DestroyedData.HasComponent(entity) || !m_ProcessedObjects.Add(entity) || !m_PrefabRefs.TryGetComponent(entity, ref prefabRef))
			{
				return;
			}
			float num = 0f;
			ObjectGeometryData objectGeometryData = default(ObjectGeometryData);
			if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, ref objectGeometryData) && (objectGeometryData.m_Flags & (GeometryFlags.Physical | GeometryFlags.HasLot)) == (GeometryFlags.Physical | GeometryFlags.HasLot))
			{
				num = BuildingUtils.GetCollapseTime(objectGeometryData.m_Size.y);
				bool flag = false;
				DynamicBuffer<SubMesh> val = default(DynamicBuffer<SubMesh>);
				if (m_PrefabSubMeshes.TryGetBuffer(prefabRef.m_Prefab, ref val))
				{
					MeshData meshData = default(MeshData);
					DynamicBuffer<PlaceholderObjectElement> placeholderElements = default(DynamicBuffer<PlaceholderObjectElement>);
					float2 val10 = default(float2);
					for (int i = 0; i < val.Length; i++)
					{
						SubMesh subMesh = val[i];
						if (m_MeshData.TryGetComponent(subMesh.m_SubMesh, ref meshData))
						{
							float2 val2 = MathUtils.Center(((Bounds3)(ref meshData.m_Bounds)).xz);
							float2 val3 = MathUtils.Extents(((Bounds3)(ref meshData.m_Bounds)).xz);
							float3 val4 = math.rotate(subMesh.m_Rotation, new float3(val3.x, 0f, 0f));
							float3 val5 = math.rotate(subMesh.m_Rotation, new float3(0f, 0f, val3.y));
							float3 position = subMesh.m_Position + math.rotate(subMesh.m_Rotation, new float3(val2.x, 0f, val2.y));
							Transform transform = m_TransformData[entity];
							val4 = math.rotate(transform.m_Rotation, val4);
							val5 = math.rotate(transform.m_Rotation, val5);
							position = ObjectUtils.LocalToWorld(transform, position);
							Entity result = m_BuildingConfigurationData.m_CollapsedSurface;
							if (m_PrefabPlaceholderElements.TryGetBuffer(result, ref placeholderElements))
							{
								AreaUtils.SelectAreaPrefab(placeholderElements, m_PrefabSpawnableObjectData, default(NativeParallelHashMap<Entity, int>), ref random, out result, out var _);
							}
							AreaData areaData = m_AreaData[result];
							Entity val6 = ((EntityCommandBuffer)(ref m_CommandBuffer)).CreateEntity(areaData.m_Archetype);
							((EntityCommandBuffer)(ref m_CommandBuffer)).SetComponent<PrefabRef>(val6, new PrefabRef(result));
							((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<Owner>(val6, new Owner(entity));
							if (m_NativeData.HasComponent(entity))
							{
								((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<Native>(val6, default(Native));
							}
							DynamicBuffer<Node> val7 = ((EntityCommandBuffer)(ref m_CommandBuffer)).SetBuffer<Node>(val6);
							val7.ResizeUninitialized(32);
							float4 val8 = float4.op_Implicit(((Random)(ref random)).NextInt4(int4.op_Implicit(3), int4.op_Implicit(10)));
							float4 val9 = float4.op_Implicit(((Random)(ref random)).NextFloat(-(float)Math.PI, (float)Math.PI));
							float num2 = (float)Math.PI * -2f / (float)val7.Length;
							for (int j = 0; j < val7.Length; j++)
							{
								float num3 = (float)j * num2;
								((float2)(ref val10))..ctor(math.cos(num3), math.sin(num3));
								val10 = math.sign(val10) * math.sqrt(math.abs(val10));
								val10 *= 1f + math.dot(math.sin(num3 * val8 + val9), float4.op_Implicit(0.025f));
								float3 position2 = position + val4 * val10.x + val5 * val10.y;
								val7[j] = new Node(position2, -3.4028235E+38f);
							}
							((EntityCommandBuffer)(ref m_CommandBuffer)).SetComponent<Area>(val6, new Area(AreaFlags.Complete));
							flag = true;
						}
					}
				}
				DynamicBuffer<Game.Areas.SubArea> val11 = default(DynamicBuffer<Game.Areas.SubArea>);
				if (m_SubAreas.TryGetBuffer(entity, ref val11))
				{
					for (int k = 0; k < val11.Length; k++)
					{
						Entity area = val11[k].m_Area;
						if (m_ClipAreas.HasComponent(area) || (m_SpaceAreas.HasComponent(area) && !IsAnyOnGround(m_AreaNodes[area])))
						{
							((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<Deleted>(val11[k].m_Area);
						}
					}
				}
				else if (flag)
				{
					((EntityCommandBuffer)(ref m_CommandBuffer)).AddBuffer<Game.Areas.SubArea>(entity);
				}
			}
			Destroyed destroyed = new Destroyed(destroyEvent.m_Event);
			if (num != 0f)
			{
				destroyed.m_Cleared = 0.5f - math.max(1f, num);
			}
			((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<Destroyed>(entity, destroyed);
			if (num != 0f)
			{
				((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<InterpolatedTransform>(entity, new InterpolatedTransform(m_TransformData[entity]));
			}
			((EntityCommandBuffer)(ref m_CommandBuffer)).AddComponent<Updated>(entity);
			Building building = default(Building);
			if (m_Buildings.TryGetComponent(entity, ref building))
			{
				((EntityCommandBuffer)(ref m_CommandBuffer)).RemoveComponent(entity, ref m_DestroyedBuildingComponents);
				if (building.m_RoadEdge != Entity.Null)
				{
					if (m_ElectricityConsumers.HasComponent(entity))
					{
						m_UpdatedElectricityRoadEdges.Enqueue(building.m_RoadEdge);
					}
					if (m_WaterConsumers.HasComponent(entity))
					{
						m_UpdatedWaterPipeRoadEdges.Enqueue(building.m_RoadEdge);
					}
				}
			}
			DynamicBuffer<SubObject> val12 = default(DynamicBuffer<SubObject>);
			if (!m_SubObjects.TryGetBuffer(entity, ref val12))
			{
				return;
			}
			for (int l = 0; l < val12.Length; l++)
			{
				Entity subObject = val12[l].m_SubObject;
				if (!m_Buildings.HasComponent(subObject))
				{
					DestroyObject(ref random, subObject, destroyEvent);
				}
			}
		}

		private bool IsAnyOnGround(DynamicBuffer<Node> nodes)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				if (nodes[i].m_Elevation == -3.4028235E+38f)
				{
					return true;
				}
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Destroy> __Game_Objects_Destroy_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Clip> __Game_Areas_Clip_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Space> __Game_Areas_Space_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaData> __Game_Prefabs_AreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			__Game_Objects_Destroy_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<Destroy>(true);
			__Game_Objects_Transform_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Transform>(true);
			__Game_Common_Destroyed_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Destroyed>(true);
			__Game_Common_Native_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Native>(true);
			__Game_Buildings_Building_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Building>(true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ElectricityConsumer>(true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<WaterConsumer>(true);
			__Game_Areas_Clip_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Clip>(true);
			__Game_Areas_Space_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<Space>(true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<PrefabRef>(true);
			__Game_Prefabs_AreaData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<AreaData>(true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<MeshData>(true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<ObjectGeometryData>(true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = ((SystemState)(ref state)).GetComponentLookup<SpawnableObjectData>(true);
			__Game_Objects_SubObject_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<SubObject>(true);
			__Game_Areas_SubArea_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Game.Areas.SubArea>(true);
			__Game_Areas_Node_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<Node>(true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<PlaceholderObjectElement>(true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = ((SystemState)(ref state)).GetBufferLookup<SubMesh>(true);
		}
	}

	private ModificationBarrier2 m_ModificationBarrier;

	private ElectricityRoadConnectionGraphSystem m_ElectricityRoadConnectionGraphSystem;

	private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;

	private EntityQuery m_EventQuery;

	private EntityQuery m_BuildingConfigurationQuery;

	private ComponentTypeSet m_DestroyedBuildingComponents;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_ModificationBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_ElectricityRoadConnectionGraphSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ElectricityRoadConnectionGraphSystem>();
		m_WaterPipeRoadConnectionGraphSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<WaterPipeRoadConnectionGraphSystem>();
		m_EventQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Event>(),
			ComponentType.ReadOnly<Destroy>()
		});
		m_BuildingConfigurationQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<BuildingConfigurationData>() });
		m_DestroyedBuildingComponents = new ComponentTypeSet(ComponentType.ReadOnly<ElectricityConsumer>(), ComponentType.ReadOnly<WaterConsumer>(), ComponentType.ReadOnly<GarbageProducer>(), ComponentType.ReadOnly<MailProducer>());
		((ComponentSystemBase)this).RequireForUpdate(m_EventQuery);
		((ComponentSystemBase)this).RequireForUpdate(m_BuildingConfigurationQuery);
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
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		JobHandle deps;
		JobHandle deps2;
		DestroyObjectsJob destroyObjectsJob = new DestroyObjectsJob
		{
			m_DestroyType = InternalCompilerInterface.GetComponentTypeHandle<Destroy>(ref __TypeHandle.__Game_Objects_Destroy_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup<Transform>(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup<Destroyed>(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup<Native>(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup<Building>(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup<ElectricityConsumer>(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_WaterConsumers = InternalCompilerInterface.GetComponentLookup<WaterConsumer>(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ClipAreas = InternalCompilerInterface.GetComponentLookup<Clip>(ref __TypeHandle.__Game_Areas_Clip_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SpaceAreas = InternalCompilerInterface.GetComponentLookup<Space>(ref __TypeHandle.__Game_Areas_Space_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup<PrefabRef>(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_AreaData = InternalCompilerInterface.GetComponentLookup<AreaData>(ref __TypeHandle.__Game_Prefabs_AreaData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_MeshData = InternalCompilerInterface.GetComponentLookup<MeshData>(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup<ObjectGeometryData>(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup<SpawnableObjectData>(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup<SubObject>(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup<Game.Areas.SubArea>(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup<Node>(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup<PlaceholderObjectElement>(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_PrefabSubMeshes = InternalCompilerInterface.GetBufferLookup<SubMesh>(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref ((SystemBase)this).CheckedStateRef),
			m_ProcessedObjects = new NativeHashSet<Entity>(32, AllocatorHandle.op_Implicit((Allocator)3)),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
			m_UpdatedElectricityRoadEdges = m_ElectricityRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps),
			m_UpdatedWaterPipeRoadEdges = m_WaterPipeRoadConnectionGraphSystem.GetEdgeUpdateQueue(out deps2),
			m_RandomSeed = RandomSeed.Next(),
			m_DestroyedBuildingComponents = m_DestroyedBuildingComponents,
			m_BuildingConfigurationData = ((EntityQuery)(ref m_BuildingConfigurationQuery)).GetSingleton<BuildingConfigurationData>()
		};
		((SystemBase)this).Dependency = JobChunkExtensions.Schedule<DestroyObjectsJob>(destroyObjectsJob, m_EventQuery, JobHandle.CombineDependencies(((SystemBase)this).Dependency, deps, deps2));
		destroyObjectsJob.m_ProcessedObjects.Dispose(((SystemBase)this).Dependency);
		((EntityCommandBufferSystem)m_ModificationBarrier).AddJobHandleForProducer(((SystemBase)this).Dependency);
		m_ElectricityRoadConnectionGraphSystem.AddQueueWriter(((SystemBase)this).Dependency);
		m_WaterPipeRoadConnectionGraphSystem.AddQueueWriter(((SystemBase)this).Dependency);
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
	public DestroySystem()
	{
	}
}
