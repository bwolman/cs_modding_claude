using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

[InternalBufferCapacity(0)]
public struct PathElement : IBufferElementData, ISerializable
{
	public Entity m_Target;

	public float2 m_TargetDelta;

	public PathElementFlags m_Flags;

	public PathElement(Entity target, float2 targetDelta, PathElementFlags flags = ~(PathElementFlags.Secondary | PathElementFlags.PathStart | PathElementFlags.Action | PathElementFlags.Return | PathElementFlags.Reverse | PathElementFlags.WaitPosition | PathElementFlags.Leader | PathElementFlags.Hangaround))
	{
		m_Target = target;
		m_TargetDelta = targetDelta;
		m_Flags = flags;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		int2 @int = math.select(math.select(new int2(2), new int2(0), m_TargetDelta == 0f), new int2(1), m_TargetDelta == 1f);
		byte value = (byte)(@int.x | (@int.y << 4));
		writer.Write(value);
		if (@int.x == 2)
		{
			float x = m_TargetDelta.x;
			writer.Write(x);
		}
		if (@int.y == 2)
		{
			float y = m_TargetDelta.y;
			writer.Write(y);
		}
		PathElementFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		reader.Read(out byte value);
		int2 @int = new int2(value & 0xF, value >> 4);
		m_TargetDelta = math.select(0f, 1f, @int == 1);
		if (@int.x == 2)
		{
			ref float x = ref m_TargetDelta.x;
			reader.Read(out x);
		}
		if (@int.y == 2)
		{
			ref float y = ref m_TargetDelta.y;
			reader.Read(out y);
		}
		if (reader.context.version >= Version.taxiDispatchCenter)
		{
			reader.Read(out byte value2);
			m_Flags = (PathElementFlags)value2;
		}
	}
}
