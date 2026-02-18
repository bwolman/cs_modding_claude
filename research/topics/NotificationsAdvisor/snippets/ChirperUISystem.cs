using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.PSI;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ChirperUISystem : UISystemBase
{
	private struct ChirpComparer : IComparer<Entity>
	{
		private EntityManager m_EntityManager;

		public ChirpComparer(EntityManager entityManager)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			m_EntityManager = entityManager;
		}

		public int Compare(Entity a, Entity b)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			int num = -((EntityManager)(ref m_EntityManager)).GetComponentData<Game.Triggers.Chirp>(a).m_CreationFrame.CompareTo(((EntityManager)(ref m_EntityManager)).GetComponentData<Game.Triggers.Chirp>(b).m_CreationFrame);
			if (num == 0)
			{
				return ((Entity)(ref a)).CompareTo(b);
			}
			return num;
		}
	}

	private const string kGroup = "chirper";

	private const int kBrandIconSize = 32;

	private PrefabSystem m_PrefabSystem;

	private SelectedInfoUISystem m_SelectedInfoUISystem;

	private InfoviewsUISystem m_InfoviewsUISystem;

	private ChirpLinkSystem m_ChirpLinkSystem;

	private NameSystem m_NameSystem;

	private EntityQuery m_ChirpQuery;

	private EntityQuery m_ModifiedChirpQuery;

	private EntityQuery m_CreatedChirpQuery;

	private EntityQuery m_TimeDataQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private RawValueBinding m_ChirpsBinding;

	private RawEventBinding m_ChirpAddedBinding;

	[Preserve]
	protected override void OnCreate()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected O, but got Unknown
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Expected O, but got Unknown
		//IL_01f5: Expected O, but got Unknown
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Expected O, but got Unknown
		//IL_0213: Expected O, but got Unknown
		base.OnCreate();
		m_PrefabSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<PrefabSystem>();
		m_SelectedInfoUISystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
		m_InfoviewsUISystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<InfoviewsUISystem>();
		m_ChirpLinkSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<ChirpLinkSystem>();
		m_NameSystem = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<NameSystem>();
		EntityQueryDesc[] array = new EntityQueryDesc[1];
		EntityQueryDesc val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[3]
		{
			ComponentType.ReadOnly<Game.Triggers.Chirp>(),
			ComponentType.ReadOnly<RandomLocalizationIndex>(),
			ComponentType.ReadOnly<PrefabRef>()
		};
		val.None = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<Temp>()
		};
		array[0] = val;
		m_ChirpQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array);
		EntityQueryDesc[] array2 = new EntityQueryDesc[1];
		val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Game.Triggers.Chirp>(),
			ComponentType.ReadOnly<PrefabRef>()
		};
		array2[0] = val;
		m_ModifiedChirpQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array2);
		((EntityQuery)(ref m_ModifiedChirpQuery)).AddOrderVersionFilter();
		((EntityQuery)(ref m_ModifiedChirpQuery)).AddChangedVersionFilter(ComponentType.ReadOnly<Game.Triggers.Chirp>());
		EntityQueryDesc[] array3 = new EntityQueryDesc[1];
		val = new EntityQueryDesc();
		val.All = (ComponentType[])(object)new ComponentType[4]
		{
			ComponentType.ReadOnly<Game.Triggers.Chirp>(),
			ComponentType.ReadOnly<RandomLocalizationIndex>(),
			ComponentType.ReadOnly<PrefabRef>(),
			ComponentType.ReadOnly<Created>()
		};
		val.None = (ComponentType[])(object)new ComponentType[2]
		{
			ComponentType.ReadOnly<Deleted>(),
			ComponentType.ReadOnly<Temp>()
		};
		array3[0] = val;
		m_CreatedChirpQuery = ((ComponentSystemBase)this).GetEntityQuery((EntityQueryDesc[])(object)array3);
		m_TimeDataQuery = ((ComponentSystemBase)this).GetEntityQuery((ComponentType[])(object)new ComponentType[1] { ComponentType.ReadOnly<TimeData>() });
		m_EndFrameBarrier = ((ComponentSystemBase)this).World.GetOrCreateSystemManaged<EndFrameBarrier>();
		RawValueBinding val2 = new RawValueBinding("chirper", "chirps", (Action<IJsonWriter>)UpdateChirps);
		RawValueBinding binding = val2;
		m_ChirpsBinding = val2;
		AddBinding((IBinding)(object)binding);
		RawEventBinding val3 = new RawEventBinding("chirper", "chirpAdded");
		RawEventBinding binding2 = val3;
		m_ChirpAddedBinding = val3;
		AddBinding((IBinding)(object)binding2);
		AddBinding((IBinding)(object)new TriggerBinding<Entity>("chirper", "addLike", (Action<Entity>)AddLike, (IReader<Entity>)null));
		AddBinding((IBinding)(object)new TriggerBinding<Entity>("chirper", "removeLike", (Action<Entity>)RemoveLike, (IReader<Entity>)null));
		AddBinding((IBinding)(object)new TriggerBinding<string>("chirper", "selectLink", (Action<string>)SelectLink, (IReader<string>)null));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!((EntityQuery)(ref m_ModifiedChirpQuery)).IsEmpty)
		{
			m_ChirpsBinding.Update();
		}
		if (((EventBindingBase)m_ChirpAddedBinding).active && !((EntityQuery)(ref m_CreatedChirpQuery)).IsEmptyIgnoreFilter)
		{
			PublishAddedChirps();
		}
	}

	private void AddLike(Entity entity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		Game.Triggers.Chirp componentData = ((EntityManager)(ref entityManager)).GetComponentData<Game.Triggers.Chirp>(entity);
		componentData.m_Flags |= ChirpFlags.Liked;
		EntityCommandBuffer val = m_EndFrameBarrier.CreateCommandBuffer();
		((EntityCommandBuffer)(ref val)).SetComponent<Game.Triggers.Chirp>(entity, componentData);
		((EntityCommandBuffer)(ref val)).AddComponent<Updated>(entity, default(Updated));
	}

	private void RemoveLike(Entity entity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		Game.Triggers.Chirp componentData = ((EntityManager)(ref entityManager)).GetComponentData<Game.Triggers.Chirp>(entity);
		componentData.m_Flags &= ~ChirpFlags.Liked;
		EntityCommandBuffer val = m_EndFrameBarrier.CreateCommandBuffer();
		((EntityCommandBuffer)(ref val)).SetComponent<Game.Triggers.Chirp>(entity, componentData);
		((EntityCommandBuffer)(ref val)).AddComponent<Updated>(entity, default(Updated));
	}

	private void SelectLink(string target)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager;
		if (URI.TryParseEntity(target, out var entity))
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (((EntityManager)(ref entityManager)).Exists(entity))
			{
				m_SelectedInfoUISystem.SetSelection(entity);
				m_SelectedInfoUISystem.Focus(entity);
			}
		}
		if (URI.TryParseInfoview(target, out var entity2))
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (((EntityManager)(ref entityManager)).Exists(entity2))
			{
				m_InfoviewsUISystem.SetActiveInfoview(entity2);
			}
		}
	}

	private void UpdateChirps(IJsonWriter binder)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		NativeArray<Entity> sortedChirps = GetSortedChirps(m_ChirpQuery);
		JsonWriterExtensions.ArrayBegin(binder, sortedChirps.Length);
		for (int i = 0; i < sortedChirps.Length; i++)
		{
			BindChirp(binder, sortedChirps[i]);
		}
		binder.ArrayEnd();
	}

	private void PublishAddedChirps()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		NativeArray<Entity> sortedChirps = GetSortedChirps(m_CreatedChirpQuery);
		int length = sortedChirps.Length;
		for (int i = 0; i < length; i++)
		{
			IJsonWriter binder = m_ChirpAddedBinding.EventBegin();
			BindChirp(binder, sortedChirps[i], newChirp: true);
			m_ChirpAddedBinding.EventEnd();
		}
	}

	private NativeArray<Entity> GetSortedChirps(EntityQuery chirpQuery)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		NativeArray<Entity> val = ((EntityQuery)(ref chirpQuery)).ToEntityArray(AllocatorHandle.op_Implicit((Allocator)2));
		NativeSortExtension.Sort<Entity, ChirpComparer>(val, new ChirpComparer(((ComponentSystemBase)this).EntityManager));
		return val;
	}

	public void BindChirp(IJsonWriter binder, Entity chirpEntity, bool newChirp = false)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		string messageID = GetMessageID(chirpEntity);
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		Game.Triggers.Chirp componentData = ((EntityManager)(ref entityManager)).GetComponentData<Game.Triggers.Chirp>(chirpEntity);
		binder.TypeBegin("chirper.Chirp");
		binder.PropertyName("entity");
		UnityWriters.Write(binder, chirpEntity);
		binder.PropertyName("sender");
		BindChirpSender(binder, chirpEntity);
		if (newChirp)
		{
			entityManager = ((ComponentSystemBase)this).EntityManager;
			if (((EntityManager)(ref entityManager)).HasComponent<ChirperAccountData>(componentData.m_Sender))
			{
				entityManager = ((ComponentSystemBase)this).EntityManager;
				Telemetry.Chirp(((EntityManager)(ref entityManager)).GetComponentData<PrefabRef>(chirpEntity).m_Prefab, componentData.m_Likes);
			}
		}
		binder.PropertyName("date");
		binder.Write(GetTicks(componentData.m_CreationFrame));
		binder.PropertyName("messageId");
		binder.Write(messageID);
		binder.PropertyName("links");
		DynamicBuffer<ChirpEntity> val = default(DynamicBuffer<ChirpEntity>);
		if (EntitiesExtensions.TryGetBuffer<ChirpEntity>(((ComponentSystemBase)this).EntityManager, chirpEntity, true, ref val))
		{
			int length = val.Length;
			JsonWriterExtensions.ArrayBegin(binder, length);
			for (int i = 0; i < length; i++)
			{
				BindChirpLink(binder, chirpEntity, i);
			}
			binder.ArrayEnd();
		}
		else
		{
			JsonWriterExtensions.WriteEmptyArray(binder);
		}
		binder.PropertyName("likes");
		binder.Write(componentData.m_Likes);
		binder.PropertyName("liked");
		binder.Write((componentData.m_Flags & ChirpFlags.Liked) != 0);
		binder.TypeEnd();
	}

	public string GetMessageID(Entity chirp)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		PrefabRef prefabRef = default(PrefabRef);
		DynamicBuffer<RandomLocalizationIndex> val = default(DynamicBuffer<RandomLocalizationIndex>);
		if (EntitiesExtensions.TryGetComponent<PrefabRef>(((ComponentSystemBase)this).EntityManager, chirp, ref prefabRef) && EntitiesExtensions.TryGetBuffer<RandomLocalizationIndex>(((ComponentSystemBase)this).EntityManager, chirp, true, ref val) && val.Length > 0 && m_PrefabSystem.TryGetPrefab<PrefabBase>(prefabRef.m_Prefab, out var prefab) && prefab.TryGet<RandomLocalization>(out var component))
		{
			return LocalizationUtils.AppendIndex(component.m_LocalizationID, val[0]);
		}
		return string.Empty;
	}

	private void BindChirpSender(IJsonWriter binder, Entity entity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		Game.Triggers.Chirp componentData = ((EntityManager)(ref entityManager)).GetComponentData<Game.Triggers.Chirp>(entity);
		binder.TypeBegin("chirper.ChirpSender");
		entityManager = ((ComponentSystemBase)this).EntityManager;
		ChirpLinkSystem.CachedChirpData data;
		if (((EntityManager)(ref entityManager)).Exists(componentData.m_Sender))
		{
			binder.PropertyName("entity");
			UnityWriters.Write(binder, componentData.m_Sender);
			binder.PropertyName("link");
			BindChirpLink(binder, componentData.m_Sender, m_NameSystem.GetName(componentData.m_Sender));
		}
		else if (m_ChirpLinkSystem.TryGetData(entity, out data))
		{
			binder.PropertyName("entity");
			UnityWriters.Write(binder, data.m_Sender.m_Entity);
			binder.PropertyName("link");
			BindChirpLink(binder, Entity.Null, data.m_Sender.m_Name);
		}
		else
		{
			binder.PropertyName("entity");
			UnityWriters.Write(binder, Entity.Null);
			binder.PropertyName("link");
			BindChirpLink(binder, Entity.Null, NameSystem.Name.CustomName(string.Empty));
		}
		binder.TypeEnd();
	}

	private void BindChirpLink(IJsonWriter binder, Entity entity, int linkIndex)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		DynamicBuffer<ChirpEntity> buffer = ((EntityManager)(ref entityManager)).GetBuffer<ChirpEntity>(entity, true);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		ChirpLinkSystem.CachedChirpData data;
		if (((EntityManager)(ref entityManager)).Exists(buffer[linkIndex].m_Entity))
		{
			BindChirpLink(binder, buffer[linkIndex].m_Entity, m_NameSystem.GetName(buffer[linkIndex].m_Entity, omitBrand: true));
		}
		else if (m_ChirpLinkSystem.TryGetData(entity, out data) && data.m_Links.Length > linkIndex)
		{
			BindChirpLink(binder, Entity.Null, data.m_Links[linkIndex].m_Name);
		}
		else
		{
			BindChirpLink(binder, Entity.Null, NameSystem.Name.CustomName(string.Empty));
		}
	}

	public void BindChirpLink(IJsonWriter binder, Entity entity, NameSystem.Name name)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		binder.TypeBegin("chirper.ChirpLink");
		binder.PropertyName("name");
		JsonWriterExtensions.Write<NameSystem.Name>(binder, name);
		binder.PropertyName("target");
		EntityManager entityManager = ((ComponentSystemBase)this).EntityManager;
		PropertyRenter propertyRenter = default(PropertyRenter);
		if (((EntityManager)(ref entityManager)).HasComponent<CompanyData>(entity) && EntitiesExtensions.TryGetComponent<PropertyRenter>(((ComponentSystemBase)this).EntityManager, entity, ref propertyRenter))
		{
			entity = propertyRenter.m_Property;
		}
		string text = ((entity != Entity.Null) ? URI.FromEntity(entity) : string.Empty);
		entityManager = ((ComponentSystemBase)this).EntityManager;
		if (((EntityManager)(ref entityManager)).HasComponent<ChirperAccountData>(entity))
		{
			ChirperAccount prefab = m_PrefabSystem.GetPrefab<ChirperAccount>(entity);
			text = URI.FromInfoView(m_PrefabSystem.GetEntity(prefab.m_InfoView));
		}
		binder.Write(text);
		binder.TypeEnd();
	}

	[CanBeNull]
	private string GetAvatar(Entity chirpEntity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Game.Triggers.Chirp chirp = default(Game.Triggers.Chirp);
		if (EntitiesExtensions.TryGetComponent<Game.Triggers.Chirp>(((ComponentSystemBase)this).EntityManager, chirpEntity, ref chirp))
		{
			Entity val = chirp.m_Sender;
			CompanyData companyData = default(CompanyData);
			if (EntitiesExtensions.TryGetComponent<CompanyData>(((ComponentSystemBase)this).EntityManager, val, ref companyData))
			{
				val = companyData.m_Brand;
			}
			PrefabData prefabData = default(PrefabData);
			if (EntitiesExtensions.TryGetComponent<PrefabData>(((ComponentSystemBase)this).EntityManager, val, ref prefabData) && m_PrefabSystem.TryGetPrefab<PrefabBase>(prefabData, out var prefab))
			{
				string icon = ImageSystem.GetIcon(prefab);
				if (icon != null)
				{
					return icon;
				}
				if (prefab is ChirperAccount chirperAccount && (Object)(object)chirperAccount.m_InfoView != (Object)null)
				{
					return chirperAccount.m_InfoView.m_IconPath;
				}
				if (prefab is BrandPrefab brandPrefab)
				{
					return $"{brandPrefab.thumbnailUrl}?width={32}&height={32}";
				}
			}
		}
		return null;
	}

	private int GetRandomIndex(Entity chirpEntity)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		Game.Triggers.Chirp chirp = default(Game.Triggers.Chirp);
		DynamicBuffer<RandomLocalizationIndex> val = default(DynamicBuffer<RandomLocalizationIndex>);
		if (EntitiesExtensions.TryGetComponent<Game.Triggers.Chirp>(((ComponentSystemBase)this).EntityManager, chirpEntity, ref chirp) && EntitiesExtensions.TryGetBuffer<RandomLocalizationIndex>(((ComponentSystemBase)this).EntityManager, chirp.m_Sender, true, ref val) && val.Length > 0)
		{
			return val[0].m_Index;
		}
		return 0;
	}

	private uint GetTicks(uint frameIndex)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return frameIndex - TimeData.GetSingleton(m_TimeDataQuery).m_FirstFrame;
	}

	[Preserve]
	public ChirperUISystem()
	{
	}
}
