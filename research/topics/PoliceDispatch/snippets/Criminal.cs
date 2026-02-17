// Decompiled from Game.dll using ilspycmd v9.1
// Game.Citizens.Criminal -- criminal citizen state
// NOTE: This is in Game.Citizens, NOT Game.Events

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct Criminal : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public ushort m_JailTime;

	public CriminalFlags m_Flags;

	public Criminal(Entity _event, CriminalFlags flags)
	{
		m_Event = _event;
		m_JailTime = 0;
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		ushort jailTime = m_JailTime;
		writer.Write(jailTime);
		CriminalFlags flags = m_Flags;
		writer.Write((ushort)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		if (reader.context.version >= Version.policeImprovement2)
		{
			ref ushort jailTime = ref m_JailTime;
			reader.Read(out jailTime);
			reader.Read(out ushort value2);
			m_Flags = (CriminalFlags)value2;
		}
		else
		{
			reader.Read(out byte value3);
			m_Flags = (CriminalFlags)value3;
		}
	}
}
