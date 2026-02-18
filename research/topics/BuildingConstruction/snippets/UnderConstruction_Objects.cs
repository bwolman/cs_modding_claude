using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Objects;

[FormerlySerializedAs("Game.Buildings.SetLevel, Game")]
public struct UnderConstruction : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_NewPrefab;

	public byte m_Progress;

	public byte m_Speed;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Entity newPrefab = m_NewPrefab;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(newPrefab);
		byte progress = m_Progress;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(progress);
		byte speed = m_Speed;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(speed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		ref Entity newPrefab = ref m_NewPrefab;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref newPrefab);
		Context context = ((IReader)reader).context;
		if (((Context)(ref context)).version >= Version.constructionProgress)
		{
			ref byte progress = ref m_Progress;
			((IReader)reader/*cast due to .constrained prefix*/).Read(ref progress);
		}
		else
		{
			m_Progress = 255;
		}
		context = ((IReader)reader).context;
		if (((Context)(ref context)).version >= Version.constructionSpeed)
		{
			ref byte speed = ref m_Speed;
			((IReader)reader/*cast due to .constrained prefix*/).Read(ref speed);
		}
		else
		{
			m_Speed = 50;
		}
	}
}
