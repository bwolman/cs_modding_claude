using System;
using Colossal;
using Colossal.Serialization.Entities;

namespace Game.Prefabs;

public struct PrefabID : IEquatable<PrefabID>, ISerializable
{
	private string m_Type;

	private string m_Name;

	private Hash128 m_Hash;

	public PrefabID(PrefabBase prefab, Hash128 overrideHash = default(Hash128))
	{
		m_Type = prefab.GetType().Name;
		m_Name = prefab.name;
		m_Hash = default(Hash128);
		if (overrideHash.isValid)
		{
			m_Hash = overrideHash;
		}
		else if (prefab.asset != null)
		{
			int data = prefab.asset.GetMeta().platformID;
			if (data > 0)
			{
				m_Hash.Calculate(in data);
			}
			else
			{
				m_Hash = prefab.asset.id.guid;
			}
		}
	}

	public PrefabID(string type, string name, Hash128 hash = default(Hash128))
	{
		m_Type = type;
		m_Name = name;
		m_Hash = hash;
	}

	public bool Equals(PrefabID other)
	{
		if (m_Type.Equals(other.m_Type) && m_Name.Equals(other.m_Name))
		{
			return m_Hash.Equals(other.m_Hash);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Name.GetHashCode() ^ m_Hash.GetHashCode();
	}

	public string ToUrlSegment()
	{
		string text = Uri.EscapeDataString(m_Type);
		string text2 = Uri.EscapeDataString(m_Name);
		if (!m_Hash.isValid)
		{
			return text + "/" + text2;
		}
		return $"{text}/{text2}/{m_Hash}";
	}

	public override string ToString()
	{
		if (m_Hash.isValid)
		{
			return $"{m_Type}:{m_Name} ({m_Hash})";
		}
		return $"{m_Type}:{m_Name}";
	}

	public string GetName()
	{
		return m_Name;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		string type = m_Type;
		writer.Write(type);
		string name = m_Name;
		writer.Write(name);
		Hash128 hash = m_Hash;
		writer.Write(hash);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref string type = ref m_Type;
		reader.Read(out type);
		ref string name = ref m_Name;
		reader.Read(out name);
		if (reader.context.format.Has(FormatTags.PrefabIDHash))
		{
			ref Hash128 hash = ref m_Hash;
			reader.Read(out hash);
		}
	}
}
