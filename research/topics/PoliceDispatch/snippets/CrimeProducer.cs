// Decompiled from Game.dll using ilspycmd v9.1
// Game.Buildings.CrimeProducer -- building crime state for patrol dispatch

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct CrimeProducer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_PatrolRequest;

	public float m_Crime;

	public byte m_DispatchIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity patrolRequest = m_PatrolRequest;
		writer.Write(patrolRequest);
		float crime = m_Crime;
		writer.Write(crime);
		byte dispatchIndex = m_DispatchIndex;
		writer.Write(dispatchIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity patrolRequest = ref m_PatrolRequest;
		reader.Read(out patrolRequest);
		ref float crime = ref m_Crime;
		reader.Read(out crime);
		if (reader.context.version >= Version.requestDispatchIndex)
		{
			ref byte dispatchIndex = ref m_DispatchIndex;
			reader.Read(out dispatchIndex);
		}
	}
}
