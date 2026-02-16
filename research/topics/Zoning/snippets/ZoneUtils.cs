using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Zones;

public static class ZoneUtils
{
	public const float CELL_SIZE = 8f;

	public const float CELL_AREA = 64f;

	public const int MAX_ZONE_WIDTH = 10;

	public const int MAX_ZONE_DEPTH = 6;

	public const int MAX_ZONE_TYPES = 339;

	public static float3 GetPosition(Block block, int2 min, int2 max)
	{
		float2 @float = (float2)(block.m_Size - min - max) * 4f;
		float3 position = block.m_Position;
		position.xz += block.m_Direction * @float.y;
		position.xz += MathUtils.Right(block.m_Direction) * @float.x;
		return position;
	}

	public static quaternion GetRotation(Block block)
	{
		return quaternion.LookRotation(new float3(block.m_Direction.x, 0f, block.m_Direction.y), math.up());
	}

	public static Quad2 CalculateCorners(Block block)
	{
		float2 @float = (float2)block.m_Size * 4f;
		float2 float2 = block.m_Direction * @float.y;
		float2 float3 = MathUtils.Right(block.m_Direction) * @float.x;
		float2 float4 = block.m_Position.xz + float2;
		float2 float5 = block.m_Position.xz - float2;
		return new Quad2(float4 + float3, float4 - float3, float5 - float3, float5 + float3);
	}

	public static int2 GetCellIndex(Block block, float2 position)
	{
		float2 y = MathUtils.Right(block.m_Direction);
		float2 x = block.m_Position.xz - position;
		return (int2)math.floor((new float2(math.dot(x, y), math.dot(x, block.m_Direction)) + (float2)block.m_Size * 4f) / 8f);
	}

	public static float3 GetCellPosition(Block block, int2 cellIndex)
	{
		float2 @float = (float2)(block.m_Size - (cellIndex << 1) - 1) * 4f;
		float3 position = block.m_Position;
		position.xz += block.m_Direction * @float.y;
		position.xz += MathUtils.Right(block.m_Direction) * @float.x;
		return position;
	}

	public static int GetCellWidth(float roadWidth)
	{
		return (int)math.ceil(roadWidth / 8f - 0.01f);
	}
}
