using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct Building : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_RoadEdge;

	public float m_CurvePosition;

	public uint m_OptionMask;

	public BuildingFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity roadEdge = m_RoadEdge;
		writer.Write(roadEdge);
		float curvePosition = m_CurvePosition;
		writer.Write(curvePosition);
		uint optionMask = m_OptionMask;
		writer.Write(optionMask);
		BuildingFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity roadEdge = ref m_RoadEdge;
		reader.Read(out roadEdge);
		ref float curvePosition = ref m_CurvePosition;
		reader.Read(out curvePosition);
		if (reader.context.version >= Version.buildingOptions)
		{
			ref uint optionMask = ref m_OptionMask;
			reader.Read(out optionMask);
		}
		if (reader.context.version >= Version.companyNotifications)
		{
			reader.Read(out byte value);
			m_Flags = (BuildingFlags)value;
		}
	}
}
