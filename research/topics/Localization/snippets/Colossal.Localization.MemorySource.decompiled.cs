using System.Collections.Generic;

namespace Colossal.Localization;

public class MemorySource : IDictionarySource
{
	private readonly Dictionary<string, string> m_Dict;

	public MemorySource(Dictionary<string, string> dict)
	{
		m_Dict = dict;
	}

	public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
	{
		foreach (KeyValuePair<string, string> item in m_Dict)
		{
			LocalizationEntry localizationEntry = LocalizationValidation.ParseEntry(item);
			if (localizationEntry != null && (localizationEntry.type == LocalizationEntry.IdentifierType.HashedIndexed || localizationEntry.type == LocalizationEntry.IdentifierType.Indexed))
			{
				string identifierWithoutIndex = localizationEntry.identifierWithoutIndex;
				if (!indexCounts.TryGetValue(identifierWithoutIndex, out var value) || value < localizationEntry.index + 1)
				{
					indexCounts[identifierWithoutIndex] = localizationEntry.index + 1;
				}
			}
		}
		return m_Dict;
	}

	public void Unload()
	{
	}
}
