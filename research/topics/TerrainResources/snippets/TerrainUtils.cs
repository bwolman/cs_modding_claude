// Source: Game.dll -> Game.Simulation.TerrainUtils
// Decompiled with ILSpy

using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Simulation;

public static class TerrainUtils
{
	public static readonly float3 BackDropWorldSizeScale = new float3(4f, 1f, 4f);

	public static float3 ToHeightmapSpace(ref TerrainHeightData data, float3 worldPosition)
	{
		return (worldPosition + data.offset) * data.scale;
	}

	public static float3 ToWorldSpace(ref TerrainHeightData data, float3 heightmapSpacePos)
	{
		return heightmapSpacePos / data.scale - data.offset;
	}

	public static float ToWorldSpace(ref TerrainHeightData data, float heightmapHeight)
	{
		return heightmapHeight / data.scale.y - data.offset.y;
	}

	public static Bounds3 GetBounds(ref TerrainHeightData data)
	{
		return new Bounds3(-data.offset, (data.resolution - 1) / data.scale - data.offset);
	}

	public static float SampleHeight(ref TerrainHeightData data, float3 worldPosition)
	{
		float2 xz = ToHeightmapSpace(ref data, worldPosition).xz;
		int4 @int = default(int4);
		@int.xy = (int2)math.floor(xz);
		@int.zw = @int.xy + 1;
		if (!math.clamp(@int, 0, data.resolution.xzxz - 1).Equals(@int) && data.hasBackdrop)
		{
			return SampleHeightBackdrop(ref data, worldPosition);
		}
		return SampleHeightInternal(ref data, worldPosition);
	}

	private static float SampleHeightInternal(ref TerrainHeightData data, float3 worldPosition)
	{
		float2 xz = ToHeightmapSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		int4 @int = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 int2 = @int.yyww * data.resolution.x + @int.xzxz;
		float4 @float = default(float4);
		@float.x = (int)data.heights[int2.x];
		@float.y = (int)data.heights[int2.y];
		@float.z = (int)data.heights[int2.z];
		@float.w = (int)data.heights[int2.w];
		float2 float2 = math.saturate(xz - @int.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		return ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y));
	}

	public static float SampleHeight(ref TerrainHeightData data, float3 worldPosition, out float3 normal)
	{
		float2 xz = ToHeightmapSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		valueToClamp = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 @int = valueToClamp.yyww * data.resolution.x + valueToClamp.xzxz;
		float4 @float = default(float4);
		@float.x = (int)data.heights[@int.x];
		@float.y = (int)data.heights[@int.y];
		@float.z = (int)data.heights[@int.z];
		@float.w = (int)data.heights[@int.w];
		float2 float2 = math.saturate(xz - valueToClamp.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		float2 float4 = @float.xz - @float.yw;
		normal = math.normalizesafe(new float3(math.lerp(float4.x, float4.y, float2.y), 1f, float3.x - float3.y));
		return ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y));
	}

	// Raycast methods omitted for brevity -- see full decompilation
}
