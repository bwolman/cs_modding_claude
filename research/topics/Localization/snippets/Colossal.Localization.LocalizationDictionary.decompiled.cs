using System;
using System.Collections.Generic;
using Colossal.Logging;
using UnityEngine;

namespace Colossal.Localization;

public class LocalizationDictionary
{
	private readonly struct Entry
	{
		public string value { get; }

		public bool fallback { get; }

		public Entry(string value, bool fallback)
		{
			this.value = value;
			this.fallback = fallback;
		}
	}

	private ILog log = LogManager.GetLogger("Localization");

	private readonly Dictionary<string, Entry> m_Dict;

	public string localeID { get; }

	public Dictionary<string, int> indexCounts { get; }

	public int entryCount => m_Dict.Count;

	public IEnumerable<string> entryIDs => m_Dict.Keys;

	public IEnumerable<KeyValuePair<string, string>> entries
	{
		get
		{
			foreach (KeyValuePair<string, Entry> item in m_Dict)
			{
				yield return new KeyValuePair<string, string>(item.Key, item.Value.value);
			}
		}
	}

	public LocalizationDictionary(string localeID)
	{
		this.localeID = localeID ?? throw new ArgumentNullException("localeID");
		m_Dict = new Dictionary<string, Entry>(StringComparer.Ordinal);
		indexCounts = new Dictionary<string, int>(StringComparer.Ordinal);
	}

	public bool TryGetValue(string entryID, out string value)
	{
		if (m_Dict.TryGetValue(entryID, out var value2))
		{
			value = value2.value;
			return true;
		}
		value = null;
		return false;
	}

	public bool ContainsID(string entryID, bool ignoreFallbackEntries = false)
	{
		if (!ignoreFallbackEntries)
		{
			return m_Dict.ContainsKey(entryID);
		}
		if (m_Dict.TryGetValue(entryID, out var value))
		{
			return !value.fallback;
		}
		return false;
	}

	public void Add(string entryID, string value, bool fallback = false)
	{
		if (string.IsNullOrWhiteSpace(entryID))
		{
			throw new ArgumentException("ID cannot be empty or null");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		m_Dict[entryID] = new Entry(value, fallback);
	}

	public void Clear()
	{
		m_Dict.Clear();
	}

	public string[] GetIndexedLocaleIDs(string localeID)
	{
		if (!indexCounts.TryGetValue(localeID, out var value))
		{
			Debug.LogError("No index count for locale ID " + localeID);
			return new string[0];
		}
		string[] array = new string[value];
		for (int i = 0; i < value; i++)
		{
			string text = localeID + ":" + i;
			if (!m_Dict.ContainsKey(text))
			{
				log.InfoFormat("Inconsistently indexed locale ID '{0}': There are {1} IDs, but {2} was not found.", localeID, indexCounts[localeID], text);
			}
			array[i] = text;
		}
		return array;
	}

	public void MergeFrom(LocalizationDictionary other, bool fallback)
	{
		foreach (KeyValuePair<string, Entry> item in other.m_Dict)
		{
			m_Dict.TryAdd(item.Key, new Entry(item.Value.value, fallback));
		}
	}
}
