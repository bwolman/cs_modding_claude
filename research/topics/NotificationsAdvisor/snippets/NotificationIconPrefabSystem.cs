using System.Runtime.CompilerServices;
using Game.Common;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class NotificationIconPrefabSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<NotificationIconDisplayData> __Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle;

		[MethodImpl((MethodImplOptions)256)]
		public void __AssignHandles(ref SystemState state)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<PrefabData>(true);
			__Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle = ((SystemState)(ref state)).GetComponentTypeHandle<NotificationIconDisplayData>(false);
		}
	}

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_PrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private NotificationIconRenderSystem m_NotificationIconRenderSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		base.OnCreate();
		m_PrefabSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<PrefabSystem>();
		m_NotificationIconRenderSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<NotificationIconRenderSystem>();
		EntityQueryDesc[] array = new EntityQueryDesc[1];
		EntityQueryDesc val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<NotificationIconData>(),
			ComponentType.ReadOnly<PrefabData>()
		};
		val.Any = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Created>(),
			ComponentType.ReadOnly<Deleted>()
		};
		array[0] = val;
		m_UpdatedQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		m_PrefabQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<NotificationIconData>(),
			ComponentType.ReadOnly<PrefabData>(),
			ComponentType.Exclude<Deleted>()
		});
		((ComponentSystemBase)this).RequireForUpdate(m_UpdatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		NativeArray<ArchetypeChunk> val = ((EntityQuery)(ref m_PrefabQuery)).ToArchetypeChunkArray(AllocatorHandle.op_Implicit((Allocator)3));
		int num = 1;
		try
		{
			ComponentTypeHandle<PrefabData> componentTypeHandle = InternalCompilerInterface.GetComponentTypeHandle<PrefabData>(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
			ComponentTypeHandle<NotificationIconDisplayData> componentTypeHandle2 = InternalCompilerInterface.GetComponentTypeHandle<NotificationIconDisplayData>(ref __TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle, ref ((SystemBase)this).CheckedStateRef);
			for (int i = 0; i < val.Length; i++)
			{
				ArchetypeChunk val2 = val[i];
				NativeArray<PrefabData> nativeArray = ((ArchetypeChunk)(ref val2)).GetNativeArray<PrefabData>(ref componentTypeHandle);
				NativeArray<NotificationIconDisplayData> nativeArray2 = ((ArchetypeChunk)(ref val2)).GetNativeArray<NotificationIconDisplayData>(ref componentTypeHandle2);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					NotificationIconPrefab prefab = m_PrefabSystem.GetPrefab<NotificationIconPrefab>(nativeArray[j]);
					NotificationIconDisplayData notificationIconDisplayData = nativeArray2[j];
					notificationIconDisplayData.m_IconIndex = num++;
					notificationIconDisplayData.m_MinParams = new float2(prefab.m_DisplaySize.min, prefab.m_PulsateAmplitude.min);
					notificationIconDisplayData.m_MaxParams = new float2(prefab.m_DisplaySize.max, prefab.m_PulsateAmplitude.max);
					notificationIconDisplayData.m_CategoryMask = math.select(2147483648u, notificationIconDisplayData.m_CategoryMask, notificationIconDisplayData.m_CategoryMask != 0);
					nativeArray2[j] = notificationIconDisplayData;
				}
			}
		}
		finally
		{
			val.Dispose();
		}
		m_NotificationIconRenderSystem.DisplayDataUpdated();
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
	public NotificationIconPrefabSystem()
	{
	}
}
