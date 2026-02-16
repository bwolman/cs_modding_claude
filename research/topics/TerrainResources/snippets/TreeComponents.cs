// Source: Game.dll -> Game.Objects.Tree, Game.Objects.TreeState, Game.Prefabs.TreeData
// Decompiled with ILSpy

using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

public struct Tree : IComponentData, IQueryTypeParameter, ISerializable
{
	public TreeState m_State;
	public byte m_Growth;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_State);
		writer.Write(m_Growth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out m_Growth);
		m_State = (TreeState)value;
	}
}

[Flags]
public enum TreeState : byte
{
	// No flags = Child state
	Teen      = 1,    // 0x01
	Adult     = 2,    // 0x02
	Elderly   = 4,    // 0x04
	Dead      = 8,    // 0x08
	Stump     = 0x10, // 0x10
	Collected = 0x20  // 0x20
}

// -- In Game.Prefabs namespace --
namespace Game.Prefabs;

public struct TreeData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_WoodAmount;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_WoodAmount);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_WoodAmount);
	}
}
