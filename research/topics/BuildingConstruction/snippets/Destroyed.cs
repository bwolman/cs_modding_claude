using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Common;

public struct Destroyed : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public float m_Cleared;

	public Destroyed(Entity _event)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		m_Event = _event;
		m_Cleared = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Entity val = m_Event;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(val);
		float cleared = m_Cleared;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(cleared);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		ref Entity reference = ref m_Event;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref reference);
		Context context = ((IReader)reader).context;
		if (((Context)(ref context)).version >= Version.destroyedCleared)
		{
			ref float cleared = ref m_Cleared;
			((IReader)reader/*cast due to .constrained prefix*/).Read(ref cleared);
		}
	}
}
