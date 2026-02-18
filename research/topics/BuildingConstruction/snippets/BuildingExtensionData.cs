using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct BuildingExtensionData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Position;

	public int2 m_LotSize;

	public bool m_External;

	public bool m_HasUndergroundElements;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		float3 position = m_Position;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(position);
		int2 lotSize = m_LotSize;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(lotSize);
		bool external = m_External;
		((IWriter)writer/*cast due to .constrained prefix*/).Write(external);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref position);
		ref int2 lotSize = ref m_LotSize;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref lotSize);
		ref bool external = ref m_External;
		((IReader)reader/*cast due to .constrained prefix*/).Read(ref external);
	}
}
