using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using UnityEngine;

namespace Colossal.Localization;

public class LocalizationManager
{
	private class LocaleInfo
	{
		public List<IDictionarySource> m_Sources = new List<IDictionarySource>();
	}

	private static readonly ILog log = LogManager.GetLogger("Localization");

	public const string kOsLanguage = "os";

	private bool m_SuppressEvents;

	private readonly Dictionary<string, LocaleInfo> m_LocaleInfos;

	private readonly Dictionary<string, string> m_LocaleIdToLocalizedName;

	private readonly Dictionary<SystemLanguage, string> m_SystemLanguageToLocaleId;

	private readonly LocalizationDictionary m_FallbackDictionary;

	private List<(string, IDictionarySource)> m_UserSources = new List<(string, IDictionarySource)>();

	public string fallbackLocaleId { get; private set; }

	public LocalizationDictionary activeDictionary { get; private set; }

	public string activeLocaleId => activeDictionary.localeID;

	public event Action onActiveDictionaryChanged;

	public event Action onSupportedLocalesChanged;

	public LocalizationManager(string fallbackLocaleId, SystemLanguage fallbackSystemLanguage, string fallbackLocalizedName)
	{
		m_LocaleInfos = new Dictionary<string, LocaleInfo>(StringComparer.Ordinal);
		m_LocaleIdToLocalizedName = new Dictionary<string, string>(StringComparer.Ordinal);
		m_SystemLanguageToLocaleId = new Dictionary<SystemLanguage, string>();
		m_FallbackDictionary = new LocalizationDictionary(fallbackLocaleId);
		this.fallbackLocaleId = fallbackLocaleId;
		activeDictionary = m_FallbackDictionary;
		AddLocale(fallbackLocaleId, fallbackSystemLanguage, fallbackLocalizedName);
		AssetDatabase.global.onAssetDatabaseChanged.Subscribe<LocaleAsset>(UpdateSource, AssetChangedEventArgs.Default);
	}

	public async Task PreloadAllLocales(int maxParallel = 8)
	{
		IEnumerable<LocaleAsset> assets = AssetDatabase.global.GetAssets(default(SearchFilter<LocaleAsset>));
		using SemaphoreSlim sem = new SemaphoreSlim(maxParallel);
		await Task.WhenAll(EnumerateTasks(assets, sem));
	}

	private static IEnumerable<Task> EnumerateTasks(IEnumerable<LocaleAsset> locales, SemaphoreSlim sem)
	{
		foreach (LocaleAsset locale in locales)
		{
			if (locale.state == LoadState.NotLoaded)
			{
				yield return LoadLocale(locale, sem);
			}
		}
	}

	private static async Task LoadLocale(LocaleAsset locale, SemaphoreSlim sem)
	{
		await sem.WaitAsync().ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await locale.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			sem.Release();
		}
	}

	public void UnloadUnusedLocales()
	{
		foreach (LocaleAsset asset in AssetDatabase.global.GetAssets(default(SearchFilter<LocaleAsset>)))
		{
			if (asset.localeId != activeLocaleId && asset.localeId != fallbackLocaleId)
			{
				asset.Unload();
			}
		}
	}

	private void NotifyActiveDictionaryChanged()
	{
		if (!m_SuppressEvents)
		{
			MainThreadDispatcher.RunOnMainThread(delegate
			{
				this.onActiveDictionaryChanged?.Invoke();
			});
		}
	}

	private void NotifySupportedLocalesChanged()
	{
		if (!m_SuppressEvents)
		{
			MainThreadDispatcher.RunOnMainThread(delegate
			{
				this.onSupportedLocalesChanged?.Invoke();
			});
		}
	}

	private void PerformBulkOperation(Action operation)
	{
		m_SuppressEvents = true;
		operation();
		m_SuppressEvents = false;
		NotifyActiveDictionaryChanged();
		NotifySupportedLocalesChanged();
	}

	public void LoadAvailableLocales()
	{
		foreach (LocaleAsset item in from e in AssetDatabase.global.GetAssets(default(SearchFilter<LocaleAsset>))
			orderby e.localeId == m_FallbackDictionary.localeID descending, e.localeId
			select e)
		{
			AddLocale(item);
			AddSourceInternal(item.localeId, item);
		}
		foreach (var (localeId, source) in m_UserSources)
		{
			AddSourceInternal(localeId, source);
		}
	}

	private void ReloadAvailableLocales()
	{
		Clear();
		LoadAvailableLocales();
	}

	private void Clear()
	{
		m_LocaleInfos.Clear();
		m_LocaleIdToLocalizedName.Clear();
		m_SystemLanguageToLocaleId.Clear();
		activeDictionary.Clear();
		m_FallbackDictionary.Clear();
	}

	private void UpdateSource(AssetChangedEventArgs args)
	{
		try
		{
			if (args.change == ChangeType.BulkAssetsChange)
			{
				PerformBulkOperation(ReloadAvailableLocales);
			}
			else if (args.asset is LocaleAsset { localeId: not null } localeAsset)
			{
				RemoveSource(localeAsset.localeId, localeAsset);
				if (args.change != ChangeType.AssetDeleted && !localeAsset.transient)
				{
					AddSourceInternal(localeAsset.localeId, localeAsset);
				}
			}
		}
		finally
		{
		}
	}

	private string GetSystemLocaleId()
	{
		SystemLanguage systemLanguage = Application.systemLanguage;
		if (m_SystemLanguageToLocaleId.TryGetValue(systemLanguage, out var value))
		{
			return value;
		}
		return m_FallbackDictionary.localeID;
	}

	public SystemLanguage LocaleIdToSystemLanguage(string localeId)
	{
		foreach (SystemLanguage key in m_SystemLanguageToLocaleId.Keys)
		{
			if (m_SystemLanguageToLocaleId[key] == localeId)
			{
				return key;
			}
		}
		return SystemLanguage.Unknown;
	}

	public void SetActiveLocale(string localeId)
	{
		if (localeId == null)
		{
			throw new ArgumentNullException("localeId");
		}
		if (localeId == "os")
		{
			localeId = GetSystemLocaleId();
		}
		if (activeDictionary.localeID == localeId)
		{
			return;
		}
		if (localeId != m_FallbackDictionary.localeID)
		{
			if (!m_LocaleInfos.TryGetValue(localeId, out var value))
			{
				return;
			}
			activeDictionary = new LocalizationDictionary(localeId);
			LoadLocale(value, activeDictionary);
		}
		else
		{
			activeDictionary = m_FallbackDictionary;
		}
		NotifyActiveDictionaryChanged();
	}

	public void ReloadActiveLocale()
	{
		if (!m_LocaleInfos.TryGetValue(activeDictionary.localeID, out var value))
		{
			return;
		}
		foreach (IDictionarySource source in value.m_Sources)
		{
			source.Unload();
		}
		activeDictionary.Clear();
		LoadLocale(value, activeDictionary);
		NotifyActiveDictionaryChanged();
	}

	public bool SupportsLocale(string localeId)
	{
		if (localeId == null)
		{
			return false;
		}
		return m_LocaleInfos.ContainsKey(localeId);
	}

	public string[] GetSupportedLocales()
	{
		List<string> list = new List<string>(m_LocaleInfos.Keys);
		list.Sort();
		return list.ToArray();
	}

	public string GetLocalizedName(string localeId)
	{
		return m_LocaleIdToLocalizedName.GetValueOrDefault(localeId, localeId);
	}

	public void AddLocale(LocaleAsset asset)
	{
		AddLocale(asset.localeId, asset.systemLanguage, asset.localizedName);
	}

	public void AddLocale(string localeId, SystemLanguage systemLanguage, string localizedName)
	{
		if (localeId == null)
		{
			throw new ArgumentNullException("localeId");
		}
		if (!m_LocaleInfos.ContainsKey(localeId))
		{
			m_LocaleInfos.Add(localeId, new LocaleInfo());
			m_LocaleIdToLocalizedName.Add(localeId, localizedName);
			m_SystemLanguageToLocaleId.Add(systemLanguage, localeId);
			NotifySupportedLocalesChanged();
		}
	}

	public void RemoveLocale(string localeId)
	{
		if (localeId == null)
		{
			throw new ArgumentNullException("localeId");
		}
		if (localeId == m_FallbackDictionary.localeID)
		{
			throw new ArgumentException("Can't remove fallback locale!");
		}
		if (m_LocaleInfos.ContainsKey(localeId))
		{
			if (activeDictionary.localeID == localeId)
			{
				SetActiveLocale(m_FallbackDictionary.localeID);
			}
			m_LocaleInfos.Remove(localeId);
			NotifySupportedLocalesChanged();
		}
	}

	public void AddSource(string localeId, IDictionarySource source)
	{
		if (localeId == null)
		{
			throw new ArgumentNullException("localeId");
		}
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (!m_UserSources.Contains((localeId, source)))
		{
			m_UserSources.Add((localeId, source));
			AddSourceInternal(localeId, source);
		}
	}

	private void AddSourceInternal(string localeId, IDictionarySource source)
	{
		if (localeId == null)
		{
			throw new ArgumentNullException("localeId");
		}
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (m_LocaleInfos.TryGetValue(localeId, out var value) && !value.m_Sources.Contains(source))
		{
			value.m_Sources.Add(source);
			log.Debug($"Added localization source '{source}' to {localeId}");
			if (localeId == activeDictionary.localeID)
			{
				LoadLocaleSource(source, activeDictionary);
				NotifyActiveDictionaryChanged();
			}
			else if (localeId == m_FallbackDictionary.localeID)
			{
				LoadLocaleSource(source, m_FallbackDictionary);
				AddMissingEntriesFromFallback(activeDictionary);
			}
		}
	}

	public void RemoveSource(string localeId, IDictionarySource source)
	{
		if (localeId == null)
		{
			throw new ArgumentNullException("localeId");
		}
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (m_LocaleInfos.TryGetValue(localeId, out var value) && value.m_Sources.Remove(source))
		{
			log.Debug($"Removed localization source '{source}' from {localeId}");
			if (localeId == activeDictionary.localeID)
			{
				activeDictionary.Clear();
				LoadLocale(value, activeDictionary);
				NotifyActiveDictionaryChanged();
			}
			else if (localeId == m_FallbackDictionary.localeID)
			{
				m_FallbackDictionary.Clear();
				LoadLocale(value, m_FallbackDictionary);
			}
		}
	}

	private void LoadLocale(LocaleInfo info, LocalizationDictionary target)
	{
		foreach (IDictionarySource source in info.m_Sources)
		{
			LoadLocaleSource(source, target);
		}
		AddMissingEntriesFromFallback(target);
	}

	private void LoadLocaleSource(IDictionarySource source, LocalizationDictionary target)
	{
		List<IDictionaryEntryError> list = new List<IDictionaryEntryError>();
		try
		{
			foreach (KeyValuePair<string, string> item in source.ReadEntries(list, target.indexCounts))
			{
				target.Add(item.Key, item.Value);
			}
		}
		catch (Exception exception)
		{
			log.Error(exception, $"Error while importing localization source '{source}'");
		}
		if (list.Count <= 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append($"Error(s) while importing localization entries from '{source}':");
		foreach (IDictionaryEntryError item2 in list)
		{
			stringBuilder.Append('\n');
			stringBuilder.Append(item2);
		}
		log.Warn(stringBuilder.ToString());
	}

	private void AddMissingEntriesFromFallback(LocalizationDictionary target)
	{
		if (target != m_FallbackDictionary)
		{
			target.MergeFrom(m_FallbackDictionary, fallback: true);
		}
	}
}
