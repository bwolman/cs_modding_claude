using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs.Climate;
using Game.Prefabs.Terrain;
using Game.Prefabs.Water;
using Game.Rendering;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class PrefabSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	private struct ObsoleteData
	{
		public Entity m_Entity;

		public PrefabID m_ID;
	}

	private struct LoadedIndexData
	{
		public Entity m_Entity;

		public int m_Index;
	}

	private ILog m_UnlockingLog;

	private UpdateSystem m_UpdateSystem;

	private List<PrefabBase> m_Prefabs;

	private List<ObsoleteData> m_ObsoleteIDs;

	private List<LoadedIndexData> m_LoadedIndexData;

	private Dictionary<PrefabBase, Entity> m_UpdateMap;

	private Dictionary<PrefabBase, Entity> m_Entities;

	private Dictionary<PrefabBase, bool> m_IsUnlockable;

	private Dictionary<ContentPrefab, bool> m_IsAvailable;

	private Dictionary<int, ContentPrefab> m_ModsContentPrefabs;

	private Dictionary<int, PrefabID> m_LoadedObsoleteIDs;

	private Dictionary<PrefabID, int> m_PrefabIndices;

	private ComponentTypeSet m_UnlockableTypes;

	internal IEnumerable<PrefabBase> prefabs => m_Prefabs;

	public event Action onContentAvailabilityChanged;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_Prefabs = new List<PrefabBase>();
		m_ObsoleteIDs = new List<ObsoleteData>();
		m_LoadedIndexData = new List<LoadedIndexData>();
		m_UpdateMap = new Dictionary<PrefabBase, Entity>();
		m_Entities = new Dictionary<PrefabBase, Entity>();
		m_IsUnlockable = new Dictionary<PrefabBase, bool>();
		m_IsAvailable = new Dictionary<ContentPrefab, bool>();
		m_ModsContentPrefabs = new Dictionary<int, ContentPrefab>();
		m_LoadedObsoleteIDs = new Dictionary<int, PrefabID>();
		m_PrefabIndices = new Dictionary<PrefabID, int>();
		m_UnlockingLog = LogManager.GetLogger("Unlocking");
	}

	public bool AddPrefab(PrefabBase prefab, string parentName = null, PrefabBase parentPrefab = null, ComponentBase parentComponent = null)
	{
		if (prefab == null)
		{
			if (parentName != null)
			{
				COSystemBase.baseLog.WarnFormat("Trying to add null prefab in {0}", parentName);
			}
			else if (parentPrefab != null && parentComponent != null)
			{
				COSystemBase.baseLog.WarnFormat("Trying to add null prefab in {0}/{1}", parentPrefab.name, parentComponent.name);
			}
			else if (parentPrefab != null)
			{
				COSystemBase.baseLog.WarnFormat("Trying to add null prefab in {0}", parentPrefab.name);
			}
			else
			{
				COSystemBase.baseLog.WarnFormat("Trying to add null prefab");
			}
			return false;
		}
		try
		{
			if (!m_Entities.ContainsKey(prefab))
			{
				if (!IsAvailable(prefab))
				{
					if (parentPrefab != null)
					{
						if (parentComponent != null)
						{
							COSystemBase.baseLog.ErrorFormat(prefab, "Dependency not available in {0}/{1}: {2}", parentPrefab.name, parentComponent.name, prefab.name);
						}
						else
						{
							COSystemBase.baseLog.ErrorFormat(prefab, "Dependency not available in {0}: {1}", parentPrefab.name, prefab.name);
						}
					}
					return false;
				}
				COSystemBase.baseLog.VerboseFormat(prefab, "Adding prefab '{0}'", prefab.name);
				List<ComponentBase> list = new List<ComponentBase>();
				prefab.GetComponents(list);
				HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
				for (int i = 0; i < list.Count; i++)
				{
					list[i].GetPrefabComponents(hashSet);
				}
				if (IsUnlockable(prefab))
				{
					hashSet.Add(ComponentType.ReadWrite<UnlockRequirement>());
					hashSet.Add(ComponentType.ReadWrite<Locked>());
					m_UnlockingLog.DebugFormat("Prefab locked: {0}", prefab);
				}
				hashSet.Add(ComponentType.ReadWrite<Created>());
				hashSet.Add(ComponentType.ReadWrite<Updated>());
				Entity entity = base.EntityManager.CreateEntity(PrefabUtils.ToArray(hashSet));
				PrefabData componentData = new PrefabData
				{
					m_Index = m_Prefabs.Count
				};
				base.EntityManager.SetComponentData(entity, componentData);
				PrefabID prefabID = prefab.GetPrefabID();
				if (m_PrefabIndices.ContainsKey(prefabID))
				{
					COSystemBase.baseLog.WarnFormat(prefab, "Duplicate prefab ID: {0}", prefabID);
				}
				else
				{
					m_PrefabIndices.Add(prefabID, m_Prefabs.Count);
				}
				if (prefab.TryGet<ObsoleteIdentifiers>(out var component) && component.m_PrefabIdentifiers != null)
				{
					for (int j = 0; j < component.m_PrefabIdentifiers.Length; j++)
					{
						prefabID = component.m_PrefabIdentifiers[j].GetPrefabID();
						if (m_PrefabIndices.ContainsKey(prefabID))
						{
							COSystemBase.baseLog.WarnFormat(prefab, "Duplicate prefab ID: {0} ({1})", prefabID, (prefab.asset != null) ? ((object)prefab.asset) : ((object)prefab.name));
						}
						else
						{
							m_PrefabIndices.Add(prefabID, m_Prefabs.Count);
						}
					}
				}
				m_Prefabs.Add(prefab);
				m_Entities.Add(prefab, entity);
				return true;
			}
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.ErrorFormat(prefab, exception, "Error when adding prefab: {0}", prefab.name);
		}
		return false;
	}

	public bool RemovePrefab(PrefabBase prefab)
	{
		if (prefab == null)
		{
			COSystemBase.baseLog.WarnFormat("Trying to remove null prefab");
			return false;
		}
		try
		{
			if (m_Entities.TryGetValue(prefab, out var value))
			{
				COSystemBase.baseLog.VerboseFormat(prefab, "Removing prefab '{0}'", prefab.name);
				base.EntityManager.AddComponent<Deleted>(value);
				PrefabData componentData = base.EntityManager.GetComponentData<PrefabData>(value);
				PrefabID prefabID = prefab.GetPrefabID();
				if (m_PrefabIndices.TryGetValue(prefabID, out var value2) && value2 == componentData.m_Index)
				{
					m_PrefabIndices.Remove(prefabID);
				}
				if (prefab.TryGet<ObsoleteIdentifiers>(out var component) && component.m_PrefabIdentifiers != null)
				{
					for (int i = 0; i < component.m_PrefabIdentifiers.Length; i++)
					{
						prefabID = component.m_PrefabIdentifiers[i].GetPrefabID();
						if (m_PrefabIndices.TryGetValue(prefabID, out value2) && value2 == componentData.m_Index)
						{
							m_PrefabIndices.Remove(prefabID);
						}
					}
				}
				if (componentData.m_Index != m_Prefabs.Count - 1)
				{
					PrefabBase prefabBase = m_Prefabs[m_Prefabs.Count - 1];
					Entity entity = m_Entities[prefabBase];
					PrefabData componentData2 = base.EntityManager.GetComponentData<PrefabData>(entity);
					PrefabID prefabID2 = prefabBase.GetPrefabID();
					if (m_PrefabIndices.TryGetValue(prefabID2, out var value3) && value3 == componentData2.m_Index)
					{
						m_PrefabIndices[prefabID2] = componentData.m_Index;
					}
					if (prefabBase.TryGet<ObsoleteIdentifiers>(out var component2) && component2.m_PrefabIdentifiers != null)
					{
						for (int j = 0; j < component2.m_PrefabIdentifiers.Length; j++)
						{
							prefabID2 = component2.m_PrefabIdentifiers[j].GetPrefabID();
							if (m_PrefabIndices.TryGetValue(prefabID2, out value3) && value3 == componentData2.m_Index)
							{
								m_PrefabIndices[prefabID2] = componentData.m_Index;
							}
						}
					}
					componentData2.m_Index = componentData.m_Index;
					base.EntityManager.SetComponentData(entity, componentData2);
					m_Prefabs[componentData.m_Index] = prefabBase;
				}
				componentData.m_Index = -1000000000;
				base.EntityManager.SetComponentData(value, componentData);
				m_Prefabs.RemoveAt(m_Prefabs.Count - 1);
				m_Entities.Remove(prefab);
				m_IsUnlockable.Remove(prefab);
				return true;
			}
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.ErrorFormat(prefab, exception, "Error when removing prefab: {0}", prefab.name);
		}
		return false;
	}

	public PrefabBase DuplicatePrefab(PrefabBase template, string name = null)
	{
		PrefabBase prefabBase = template.Clone(name);
		prefabBase.Remove<ObsoleteIdentifiers>();
		AddPrefab(prefabBase);
		return prefabBase;
	}

	public void AddOrUpdatePrefab(PrefabBase prefab)
	{
		if (m_Entities.ContainsKey(prefab))
		{
			UpdatePrefab(prefab);
		}
		else
		{
			AddPrefab(prefab);
		}
	}

	public void UpdatePrefab(PrefabBase prefab, Entity sourceInstance = default(Entity))
	{
		m_UpdateMap[prefab] = sourceInstance;
	}

	public string[] GetAvailablePrerequisitesNames()
	{
		string[] array = (from entry in m_IsAvailable
			where entry.Value
			select entry.Key.name).ToArray();
		if (array.Length == 0)
		{
			return null;
		}
		return array;
	}

	public IEnumerable<ContentPrefab> GetAvailableContentPrefabs()
	{
		return from entry in m_IsAvailable
			where entry.Value
			select entry.Key;
	}

	public ContentPrefab GetOrCreateContentPrefab(int modId)
	{
		if (m_ModsContentPrefabs.TryGetValue(modId, out var value))
		{
			return value;
		}
		ContentPrefab contentPrefab = PrefabBase.Create<ContentPrefab>($"Mod:{modId}");
		contentPrefab.AddComponent<ModRequirement>().m_ModId = modId;
		m_ModsContentPrefabs.Add(modId, contentPrefab);
		return contentPrefab;
	}

	private void SaveTextureDependencies(ILocalAssetDatabase database, PrefabBase prefab)
	{
		foreach (FieldInfo item in from f in prefab.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
			where f.FieldType == typeof(AssetReference<TextureAsset>)
			select f)
		{
			AssetReference<TextureAsset> assetReference = item.GetValue(prefab) as AssetReference<TextureAsset>;
			if (!(assetReference == null) && !(((TextureAsset)assetReference).database is AssetDatabase<Colossal.IO.AssetDatabase.Game>))
			{
				using TextureAsset textureAsset = TextureAssetExtensions.AddAsset(database, item.Name.Nicify(), assetReference);
				textureAsset.SaveCompressed();
				item.SetValue(prefab, (AssetReference<TextureAsset>)textureAsset);
			}
		}
	}

	private PrefabAsset SavePrefab(ILocalAssetDatabase database, PrefabBase prefab, bool force)
	{
		if (!force && (prefab.isReadOnly || (prefab.asset != null && prefab.asset.isPersistent)))
		{
			return null;
		}
		PrefabBase prefabBase = prefab.Clone();
		prefabBase.name = prefab.name;
		prefabBase.asset = database.AddAsset(prefabBase.name, prefabBase);
		SaveTextureDependencies(database, prefabBase);
		prefabBase.asset.Save(ContentType.Binary);
		AddPrefab(prefabBase);
		return prefabBase.asset;
	}

	internal PrefabAsset[] SavePrefabAssets(ILocalAssetDatabase database, PrefabAsset[] inputPrefabs, ref Context context)
	{
		List<PrefabAsset> list = new List<PrefabAsset>();
		PrefabAsset prefabAsset = SaveClimate(database, inputPrefabs, ref context);
		if (prefabAsset != null)
		{
			list.Add(prefabAsset);
		}
		PrefabAsset prefabAsset2 = SaveWaterRenderSettings(database, inputPrefabs, ref context);
		if (prefabAsset2 != null)
		{
			list.Add(prefabAsset2);
		}
		PrefabAsset prefabAsset3 = SaveTerrainRenderSettings(database, inputPrefabs, ref context);
		if (prefabAsset3 != null)
		{
			list.Add(prefabAsset3);
		}
		return list.ToArray();
	}

	private PrefabAsset SaveClimate(ILocalAssetDatabase database, PrefabAsset[] references, ref Context context)
	{
		ClimateSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		ClimatePrefab climate = GetPrefab<ClimatePrefab>(orCreateSystemManaged.currentClimate);
		PrefabAsset prefabAsset = SavePrefab(database, climate, references?.Select((PrefabAsset x) => x.Load() as ClimatePrefab).Any((ClimatePrefab p) => p == climate) ?? false);
		if (prefabAsset != null)
		{
			context.RemapEntityGuid(orCreateSystemManaged.currentClimate, prefabAsset.id);
		}
		return prefabAsset;
	}

	private PrefabAsset SaveWaterRenderSettings(ILocalAssetDatabase database, PrefabAsset[] references, ref Context context)
	{
		WaterRenderSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<WaterRenderSystem>();
		WaterRenderSettingsPrefab wrsp = GetPrefab<WaterRenderSettingsPrefab>(orCreateSystemManaged.currentWaterRenderSettings);
		PrefabAsset prefabAsset = SavePrefab(database, wrsp, references?.Select((PrefabAsset x) => x.Load() is WaterRenderSettingsPrefab).Any((bool p) => p == (bool)wrsp) ?? false);
		if (prefabAsset != null)
		{
			context.RemapEntityGuid(orCreateSystemManaged.currentWaterRenderSettings, prefabAsset.id);
		}
		return prefabAsset;
	}

	private PrefabAsset SaveTerrainRenderSettings(ILocalAssetDatabase database, PrefabAsset[] references, ref Context context)
	{
		TerrainMaterialSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<TerrainMaterialSystem>();
		TerrainRenderSettingsPrefab trsp = GetPrefab<TerrainRenderSettingsPrefab>(orCreateSystemManaged.currentTerrainRenderSettings);
		PrefabAsset prefabAsset = SavePrefab(database, trsp, references?.Select((PrefabAsset x) => x.Load() is TerrainRenderSettingsPrefab).Any((bool p) => p == (bool)trsp) ?? false);
		if (prefabAsset != null)
		{
			context.RemapEntityGuid(orCreateSystemManaged.currentTerrainRenderSettings, prefabAsset.id);
		}
		return prefabAsset;
	}

	public void UpdateAvailable()
	{
		bool flag = false;
		foreach (KeyValuePair<ContentPrefab, bool> item in m_IsAvailable.ToList())
		{
			bool value = item.Value;
			bool flag2 = item.Key.IsAvailable();
			m_IsAvailable[item.Key] = flag2;
			if (flag2 != value)
			{
				flag = true;
			}
		}
		if (flag)
		{
			this.onContentAvailabilityChanged?.Invoke();
		}
	}

	public bool IsAvailable(PrefabBase prefab)
	{
		bool flag = false;
		bool flag2 = true;
		bool flag3 = true;
		if (prefab.asset != null)
		{
			int platformID = prefab.asset.GetMeta().platformID;
			if (platformID > 0)
			{
				ContentPrefab orCreateContentPrefab = GetOrCreateContentPrefab(platformID);
				bool flag4 = orCreateContentPrefab.IsAvailable();
				if (!m_IsAvailable.TryGetValue(orCreateContentPrefab, out var value) || value != flag4)
				{
					m_IsAvailable[orCreateContentPrefab] = flag4;
					flag = true;
				}
				flag2 = flag4;
			}
		}
		if (prefab.TryGet<ContentPrerequisite>(out var component))
		{
			ContentPrefab contentPrerequisite = component.m_ContentPrerequisite;
			if (!m_IsAvailable.TryGetValue(contentPrerequisite, out var value2))
			{
				value2 = contentPrerequisite.IsAvailable();
				m_IsAvailable.Add(contentPrerequisite, value2);
				flag = true;
			}
			flag3 = value2;
		}
		if (flag)
		{
			this.onContentAvailabilityChanged?.Invoke();
		}
		return flag2 && flag3;
	}

	public bool IsUnlockable(PrefabBase prefab)
	{
		if (m_IsUnlockable.TryGetValue(prefab, out var value))
		{
			return value;
		}
		if (prefab is UnlockRequirementPrefab)
		{
			m_IsUnlockable.Add(prefab, value: true);
			return true;
		}
		List<PrefabBase> dependencies = new List<PrefabBase>();
		List<ComponentBase> components = new List<ComponentBase>();
		value = IsUnlockableImpl(prefab, dependencies, components);
		m_IsUnlockable.Add(prefab, value);
		return value;
	}

	private bool IsUnlockableImpl(PrefabBase prefab, List<PrefabBase> dependencies, List<ComponentBase> components)
	{
		int count = dependencies.Count;
		try
		{
			try
			{
				bool canIgnoreUnlockDependencies = prefab.canIgnoreUnlockDependencies;
				prefab.GetComponents(components);
				for (int i = 0; i < components.Count; i++)
				{
					ComponentBase componentBase = components[i];
					if (componentBase is UnlockableBase)
					{
						return true;
					}
					if (!canIgnoreUnlockDependencies || !componentBase.ignoreUnlockDependencies)
					{
						componentBase.GetDependencies(dependencies);
					}
				}
			}
			finally
			{
				components.Clear();
			}
			for (int j = count; j < dependencies.Count; j++)
			{
				PrefabBase prefabBase = dependencies[j];
				if (prefabBase == null)
				{
					continue;
				}
				if (m_IsUnlockable.TryGetValue(prefabBase, out var value))
				{
					if (value)
					{
						return true;
					}
					continue;
				}
				if (prefabBase is UnlockRequirementPrefab)
				{
					m_IsUnlockable.Add(prefabBase, value: true);
					return true;
				}
				value = IsUnlockableImpl(prefabBase, dependencies, components);
				m_IsUnlockable.Add(prefabBase, value);
				if (value)
				{
					return true;
				}
			}
			return false;
		}
		finally
		{
			dependencies.RemoveRange(count, dependencies.Count - count);
		}
	}

	public void AddUnlockRequirement(PrefabBase unlocker, PrefabBase unlocked)
	{
		if (IsUnlockable(unlocker))
		{
			if (IsUnlockable(unlocked))
			{
				GetBuffer<UnlockRequirement>(unlocked, isReadOnly: false).Add(new UnlockRequirement(GetEntity(unlocker), UnlockFlags.RequireAll));
			}
			else
			{
				COSystemBase.baseLog.WarnFormat(unlocked, "{0} is trying to add unlock requirement to non-unlockable prefab {1}", unlocker.name, unlocked.name);
			}
		}
		else
		{
			COSystemBase.baseLog.WarnFormat(unlocker, "{0} is trying to add unlock requirements, but is non-unlockable", unlocker.name);
		}
	}

	public void AddUnlockRequirement(PrefabBase unlocker, PrefabBase[] unlocked)
	{
		if (IsUnlockable(unlocker))
		{
			Entity entity = GetEntity(unlocker);
			for (int i = 0; i < unlocked.Length; i++)
			{
				if (IsUnlockable(unlocked[i]))
				{
					GetBuffer<UnlockRequirement>(unlocked[i], isReadOnly: false).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
				}
				else
				{
					COSystemBase.baseLog.WarnFormat(unlocked[i], "{0} is trying to add unlock requirement to non-unlockable prefab {1}", unlocker.name, unlocked[i].name);
				}
			}
		}
		else
		{
			COSystemBase.baseLog.WarnFormat(unlocker, "{0} is trying to add unlock requirements, but is non-unlockable", unlocker.name);
		}
	}

	public T GetPrefab<T>(PrefabData prefabData) where T : PrefabBase
	{
		return m_Prefabs[prefabData.m_Index] as T;
	}

	public T GetPrefab<T>(Entity entity) where T : PrefabBase
	{
		return GetPrefab<T>(base.EntityManager.GetComponentData<PrefabData>(entity));
	}

	public T GetPrefab<T>(PrefabRef refData) where T : PrefabBase
	{
		return GetPrefab<T>(base.EntityManager.GetComponentData<PrefabData>(refData.m_Prefab));
	}

	public bool TryGetPrefab<T>(PrefabData prefabData, out T prefab) where T : PrefabBase
	{
		if (prefabData.m_Index >= 0)
		{
			prefab = m_Prefabs[prefabData.m_Index] as T;
			return true;
		}
		prefab = null;
		return false;
	}

	public bool TryGetPrefab<T>(Entity entity, out T prefab) where T : PrefabBase
	{
		if (base.EntityManager.TryGetComponent<PrefabData>(entity, out var component))
		{
			return TryGetPrefab<T>(component, out prefab);
		}
		prefab = null;
		return false;
	}

	public bool TryGetPrefab<T>(PrefabRef refData, out T prefab) where T : PrefabBase
	{
		if (base.EntityManager.TryGetComponent<PrefabData>(refData.m_Prefab, out var component))
		{
			return TryGetPrefab<T>(component, out prefab);
		}
		prefab = null;
		return false;
	}

	public T GetSingletonPrefab<T>(EntityQuery group) where T : PrefabBase
	{
		return GetPrefab<T>(group.GetSingletonEntity());
	}

	public bool TryGetSingletonPrefab<T>(EntityQuery group, out T prefab) where T : PrefabBase
	{
		if (!group.IsEmptyIgnoreFilter)
		{
			prefab = GetPrefab<T>(group.GetSingletonEntity());
			return true;
		}
		prefab = null;
		return false;
	}

	public Entity GetEntity(PrefabBase prefab)
	{
		return m_Entities[prefab];
	}

	public bool TryGetEntity(PrefabBase prefab, out Entity entity)
	{
		return m_Entities.TryGetValue(prefab, out entity);
	}

	public bool HasComponent<T>(PrefabBase prefab) where T : unmanaged
	{
		return base.EntityManager.HasComponent<T>(m_Entities[prefab]);
	}

	public bool HasEnabledComponent<T>(PrefabBase prefab) where T : unmanaged, IEnableableComponent
	{
		return base.EntityManager.HasEnabledComponent<T>(m_Entities[prefab]);
	}

	public T GetComponentData<T>(PrefabBase prefab) where T : unmanaged, IComponentData
	{
		return base.EntityManager.GetComponentData<T>(m_Entities[prefab]);
	}

	public bool TryGetComponentData<T>(PrefabBase prefab, out T component) where T : unmanaged, IComponentData
	{
		return base.EntityManager.TryGetComponent<T>(m_Entities[prefab], out component);
	}

	public DynamicBuffer<T> GetBuffer<T>(PrefabBase prefab, bool isReadOnly) where T : unmanaged, IBufferElementData
	{
		return base.EntityManager.GetBuffer<T>(m_Entities[prefab], isReadOnly);
	}

	public bool TryGetBuffer<T>(PrefabBase prefab, bool isReadOnly, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElementData
	{
		return base.EntityManager.TryGetBuffer(m_Entities[prefab], isReadOnly, out buffer);
	}

	public void AddComponentData<T>(PrefabBase prefab, T componentData) where T : unmanaged, IComponentData
	{
		base.EntityManager.AddComponentData(m_Entities[prefab], componentData);
	}

	public void RemoveComponent<T>(PrefabBase prefab) where T : unmanaged, IComponentData
	{
		base.EntityManager.RemoveComponent<T>(m_Entities[prefab]);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool num = UpdatePrefabs();
		m_UpdateSystem.Update(SystemUpdatePhase.PrefabUpdate);
		if (num)
		{
			base.World.GetOrCreateSystemManaged<ReplacePrefabSystem>().FinalizeReplaces();
		}
	}

	private bool UpdatePrefabs()
	{
		if (m_UpdateMap.Count == 0)
		{
			return false;
		}
		try
		{
			foreach (KeyValuePair<PrefabBase, Entity> item in m_UpdateMap)
			{
				PrefabBase key = item.Key;
				Entity value = item.Value;
				try
				{
					if (m_Entities.TryGetValue(key, out var value2))
					{
						base.EntityManager.AddComponent<Deleted>(value2);
						List<ComponentBase> list = new List<ComponentBase>();
						key.GetComponents(list);
						HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
						for (int i = 0; i < list.Count; i++)
						{
							list[i].GetPrefabComponents(hashSet);
						}
						bool num = IsUnlockable(key);
						if (num)
						{
							hashSet.Add(ComponentType.ReadWrite<UnlockRequirement>());
							hashSet.Add(ComponentType.ReadWrite<Locked>());
						}
						hashSet.Add(ComponentType.ReadWrite<Created>());
						hashSet.Add(ComponentType.ReadWrite<Updated>());
						Entity entity = base.EntityManager.CreateEntity(PrefabUtils.ToArray(hashSet));
						base.EntityManager.SetComponentData(entity, base.EntityManager.GetComponentData<PrefabData>(value2));
						if (num && !base.EntityManager.HasEnabledComponent<Locked>(value2))
						{
							base.EntityManager.SetComponentEnabled<Locked>(entity, value: false);
						}
						m_Entities[key] = entity;
						base.World.GetOrCreateSystemManaged<ReplacePrefabSystem>().ReplacePrefab(value2, entity, value);
					}
				}
				catch (Exception exception)
				{
					bool showsErrorsInUI = COSystemBase.baseLog.showsErrorsInUI;
					if (GameManager.instance.gameMode.IsEditor())
					{
						COSystemBase.baseLog.showsErrorsInUI = false;
					}
					COSystemBase.baseLog.ErrorFormat(key, exception, "Error when updating prefab: {0}", key.name);
					COSystemBase.baseLog.showsErrorsInUI = showsErrorsInUI;
				}
			}
		}
		finally
		{
			m_UpdateMap.Clear();
		}
		return true;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int count = m_Prefabs.Count;
		int count2 = m_ObsoleteIDs.Count;
		List<PrefabID> list = new List<PrefabID>(10000);
		List<PrefabID> list2 = new List<PrefabID>(100);
		for (int i = 0; i < count; i++)
		{
			PrefabBase prefabBase = m_Prefabs[i];
			Entity entity = GetEntity(prefabBase);
			if (base.EntityManager.IsComponentEnabled<PrefabData>(entity))
			{
				writer.context.TryGetRemapEntityGuid(entity, out var guid);
				list.Add(prefabBase.GetPrefabID(guid));
			}
		}
		for (int j = 0; j < count2; j++)
		{
			ObsoleteData obsoleteData = m_ObsoleteIDs[j];
			if (base.EntityManager.IsComponentEnabled<PrefabData>(obsoleteData.m_Entity))
			{
				list2.Add(obsoleteData.m_ID);
			}
		}
		count = list.Count;
		count2 = list2.Count;
		writer.Write(count);
		writer.Write(count2);
		for (int k = 0; k < count; k++)
		{
			PrefabID value = list[k];
			writer.Write(value);
		}
		for (int l = 0; l < count2; l++)
		{
			PrefabID value2 = list2[l];
			writer.Write(value2);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_ObsoleteIDs.Clear();
		m_LoadedObsoleteIDs.Clear();
		m_LoadedIndexData.Clear();
		reader.Read(out int value);
		int value2 = 0;
		if (reader.context.version >= Version.obsoletePrefabFix)
		{
			reader.Read(out value2);
		}
		for (int i = 0; i < value; i++)
		{
			reader.Read(out PrefabID value3);
			if (TryGetPrefab(value3, out var prefab))
			{
				m_LoadedIndexData.Add(new LoadedIndexData
				{
					m_Entity = GetEntity(prefab),
					m_Index = i
				});
			}
			else
			{
				m_LoadedObsoleteIDs.Add(i, value3);
			}
		}
		for (int j = 0; j < value2; j++)
		{
			int num = -1 - j;
			reader.Read(out PrefabID value4);
			if (TryGetPrefab(value4, out var prefab2))
			{
				m_LoadedIndexData.Add(new LoadedIndexData
				{
					m_Entity = GetEntity(prefab2),
					m_Index = num
				});
			}
			else
			{
				m_LoadedObsoleteIDs.Add(num, value4);
			}
		}
	}

	public void SetDefaults(Context context)
	{
		m_ObsoleteIDs.Clear();
		m_LoadedObsoleteIDs.Clear();
		m_LoadedIndexData.Clear();
	}

	public void UpdateLoadedIndices()
	{
		int count = m_Prefabs.Count;
		for (int i = 0; i < count; i++)
		{
			PrefabBase prefab = m_Prefabs[i];
			base.EntityManager.GetBuffer<LoadedIndex>(GetEntity(prefab)).Clear();
		}
		int count2 = m_LoadedIndexData.Count;
		for (int j = 0; j < count2; j++)
		{
			LoadedIndexData loadedIndexData = m_LoadedIndexData[j];
			base.EntityManager.GetBuffer<LoadedIndex>(loadedIndexData.m_Entity).Add(new LoadedIndex
			{
				m_Index = loadedIndexData.m_Index
			});
		}
	}

	public bool TryGetPrefab(PrefabID id, out PrefabBase prefab)
	{
		if (m_PrefabIndices.TryGetValue(id, out var value))
		{
			prefab = m_Prefabs[value];
			return true;
		}
		prefab = null;
		return false;
	}

	public PrefabID GetLoadedObsoleteID(int loadedIndex)
	{
		if (!m_LoadedObsoleteIDs.TryGetValue(loadedIndex, out var value))
		{
			value = new PrefabID("[Missing]", "[Missing]");
		}
		return value;
	}

	public void AddObsoleteID(Entity entity, PrefabID id)
	{
		m_ObsoleteIDs.Add(new ObsoleteData
		{
			m_Entity = entity,
			m_ID = id
		});
		COSystemBase.baseLog.WarnFormat("Unknown prefab ID: {0}", id);
	}

	public PrefabID GetObsoleteID(PrefabData prefabData)
	{
		return m_ObsoleteIDs[-1 - prefabData.m_Index].m_ID;
	}

	public PrefabID GetObsoleteID(Entity entity)
	{
		return GetObsoleteID(base.EntityManager.GetComponentData<PrefabData>(entity));
	}

	public string GetPrefabName(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<PrefabData>(entity, out var component))
		{
			if (component.m_Index >= 0)
			{
				return m_Prefabs[component.m_Index].name;
			}
			return GetObsoleteID(component).GetName();
		}
		return entity.ToString();
	}

	[Preserve]
	public PrefabSystem()
	{
	}
}
