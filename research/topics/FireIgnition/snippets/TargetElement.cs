using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

[InternalBufferCapacity(0)]
public struct TargetElement : IBufferElementData, IEquatable<TargetElement>, ISerializable
{
	public Entity m_Entity;

	public TargetElement(Entity entity)
	{
		m_Entity = entity;
	}

	public bool Equals(TargetElement other)
	{
		return m_Entity.Equals(other.m_Entity);
	}

	public override int GetHashCode()
	{
		return m_Entity.GetHashCode();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Entity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Entity);
	}
}
