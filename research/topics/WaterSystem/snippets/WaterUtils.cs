using Colossal.Mathematics;
using Unity.Mathematics;

namespace Game.Simulation;

public static class WaterUtils
{
	public static float3 ToSurfaceSpace<T>(ref WaterSurfaceData<T> data, float3 worldPosition) where T : struct
	{
		return (worldPosition + data.offset) * data.scale;
	}

	public static Line3.Segment ToSurfaceSpace(ref WaterSurfaceData<SurfaceWater> data, Line3.Segment worldLine)
	{
		return new Line3.Segment(ToSurfaceSpace(ref data, worldLine.a), ToSurfaceSpace(ref data, worldLine.b));
	}

	public static Line3.Segment ToBackdropSpace(ref WaterSurfaceData<SurfaceWater> data, Line3.Segment worldLine)
	{
		return new Line3.Segment(ToBackdropSpace(ref data, worldLine.a), ToBackdropSpace(ref data, worldLine.b));
	}

	public static float3 ToBackdropSpace(ref WaterSurfaceData<SurfaceWater> data, float3 worldPosition)
	{
		return (worldPosition + data.offset * TerrainUtils.BackDropWorldSizeScale) * data.scale / TerrainUtils.BackDropWorldSizeScale;
	}

	public static float3 ToWorldSpace<T>(ref WaterSurfaceData<T> data, float3 surfacePosition) where T : struct
	{
		return surfacePosition / data.scale - data.offset;
	}

	public static float2 ToWorldSpace<T>(ref WaterSurfaceData<T> data, float2 surfaceVelocity) where T : struct
	{
		return surfaceVelocity / data.scale.xz;
	}

	public static float ToWorldSpace<T>(ref WaterSurfaceData<T> data, float surfaceDepth) where T : struct
	{
		return surfaceDepth / data.scale.y - data.offset.y;
	}

	public static float3 ToWorldSpaceFromBackdrop(ref WaterSurfaceData<SurfaceWater> data, float3 backdropSpacePos)
	{
		return backdropSpacePos / (data.scale / TerrainUtils.BackDropWorldSizeScale) - data.offset * TerrainUtils.BackDropWorldSizeScale;
	}

	public static float SampleDepth(ref WaterSurfacesData surfacesData, float3 worldPosition)
	{
		WaterSurfaceData<SurfaceWater> data = surfacesData.depths;
		float2 xz = ToSurfaceSpace(ref data, worldPosition).xz;
		int4 @int = default(int4);
		@int.xy = (int2)math.floor(xz);
		@int.zw = @int.xy + 1;
		if (!math.clamp(@int, 0, data.resolution.xzxz - 1).Equals(@int) && surfacesData.hasBackdrop)
		{
			return SampleDepthBackdrop(ref surfacesData.downscaledDepths, worldPosition);
		}
		return SampleDepth(ref data, worldPosition);
	}

	public static float SampleDepthBackdrop(ref WaterSurfaceData<SurfaceWater> data, float3 worldPosition)
	{
		float2 xz = ToBackdropSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		valueToClamp = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 @int = valueToClamp.yyww * data.resolution.x + valueToClamp.xzxz;
		float4 @float = default(float4);
		@float.x = data.depths[@int.x].m_Depth;
		@float.y = data.depths[@int.y].m_Depth;
		@float.z = data.depths[@int.z].m_Depth;
		@float.w = data.depths[@int.w].m_Depth;
		float2 float2 = math.saturate(xz - valueToClamp.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		return math.max(ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y)), 0f);
	}

	public static float SampleDepth(ref WaterSurfaceData<SurfaceWater> data, float3 worldPosition)
	{
		float2 xz = ToSurfaceSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		valueToClamp = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 @int = valueToClamp.yyww * data.resolution.x + valueToClamp.xzxz;
		float4 @float = default(float4);
		@float.x = data.depths[@int.x].m_Depth;
		@float.y = data.depths[@int.y].m_Depth;
		@float.z = data.depths[@int.z].m_Depth;
		@float.w = data.depths[@int.w].m_Depth;
		float2 float2 = math.saturate(xz - valueToClamp.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		return math.max(ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y)), 0f);
	}

	public static float SamplePolluted(ref WaterSurfaceData<SurfaceWater> data, float3 worldPosition)
	{
		float2 xz = ToSurfaceSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		valueToClamp = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 @int = valueToClamp.yyww * data.resolution.x + valueToClamp.xzxz;
		float4 @float = default(float4);
		@float.x = data.depths[@int.x].m_Polluted;
		@float.y = data.depths[@int.y].m_Polluted;
		@float.z = data.depths[@int.z].m_Polluted;
		@float.w = data.depths[@int.w].m_Polluted;
		float2 float2 = math.saturate(xz - valueToClamp.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		return ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y));
	}

	public static float2 SampleVelocity(ref WaterSurfaceData<SurfaceWater> data, float3 worldPosition)
	{
		float2 xz = ToSurfaceSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		valueToClamp = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 @int = valueToClamp.yyww * data.resolution.x + valueToClamp.xzxz;
		float4 start = default(float4);
		float4 end = default(float4);
		start.xy = data.depths[@int.x].m_Velocity;
		end.xy = data.depths[@int.y].m_Velocity;
		start.zw = data.depths[@int.z].m_Velocity;
		end.zw = data.depths[@int.w].m_Velocity;
		float2 @float = math.saturate(xz - valueToClamp.xy);
		float4 float2 = math.lerp(start, end, @float.x);
		return ToWorldSpace(ref data, math.lerp(float2.xy, float2.zw, @float.y));
	}

	public static float SampleHeight(ref WaterSurfacesData data, ref TerrainHeightData terrainData, float3 worldPosition, out bool hasDepth)
	{
		float num = SampleDepth(ref data, worldPosition);
		hasDepth = num > 0f;
		return num + TerrainUtils.SampleHeight(ref terrainData, worldPosition);
	}

	public static float SampleHeight(ref WaterSurfaceData<SurfaceWater> data, ref TerrainHeightData terrainData, float3 worldPosition, out bool hasDepth)
	{
		float num = SampleDepth(ref data, worldPosition);
		hasDepth = num > 0f;
		return num + TerrainUtils.SampleHeight(ref terrainData, worldPosition);
	}

	public static float SampleHeight(ref WaterSurfaceData<half> maxHeightSurfaceData, ref WaterSurfacesData surfacesData, ref TerrainHeightData terrainData, float3 worldPosition)
	{
		WaterSurfaceData<SurfaceWater> data = surfacesData.depths;
		float2 xz = ToSurfaceSpace(ref data, worldPosition).xz;
		int4 @int = default(int4);
		@int.xy = (int2)math.floor(xz);
		@int.zw = @int.xy + 1;
		if (!math.clamp(@int, 0, data.resolution.xzxz - 1).Equals(@int) && surfacesData.hasBackdrop)
		{
			return SampleDepthBackdrop(ref surfacesData.downscaledDepths, worldPosition) + TerrainUtils.SampleHeightBackdrop(ref terrainData, worldPosition);
		}
		if (maxHeightSurfaceData.hasDepths)
		{
			return SampleMaxHeight(ref maxHeightSurfaceData, worldPosition);
		}
		return SampleHeight(ref surfacesData.depths, ref terrainData, worldPosition);
	}

	public static float SampleMaxHeight(ref WaterSurfaceData<half> data, float3 worldPosition)
	{
		float2 xz = ToSurfaceSpace(ref data, worldPosition).xz;
		int4 valueToClamp = default(int4);
		valueToClamp.xy = (int2)math.floor(xz);
		valueToClamp.zw = valueToClamp.xy + 1;
		valueToClamp = math.clamp(valueToClamp, 0, data.resolution.xzxz - 1);
		int4 @int = valueToClamp.yyww * data.resolution.x + valueToClamp.xzxz;
		float4 @float = default(float4);
		@float.x = data.depths[@int.x];
		@float.y = data.depths[@int.y];
		@float.z = data.depths[@int.z];
		@float.w = data.depths[@int.w];
		float2 float2 = math.saturate(xz - valueToClamp.xy);
		float2 float3 = math.lerp(@float.xz, @float.yw, float2.x);
		return math.max(ToWorldSpace(ref data, math.lerp(float3.x, float3.y, float2.y)), 0f);
	}

	public static float SampleHeight(ref WaterSurfaceData<SurfaceWater> data, ref TerrainHeightData terrainData, float3 worldPosition)
	{
		return SampleDepth(ref data, worldPosition) + TerrainUtils.SampleHeight(ref terrainData, worldPosition);
	}

	public static float SampleHeight(ref WaterSurfaceData<SurfaceWater> data, ref TerrainHeightData terrainData, float3 worldPosition, out float waterDepth)
	{
		waterDepth = SampleDepth(ref data, worldPosition);
		return waterDepth + TerrainUtils.SampleHeight(ref terrainData, worldPosition);
	}

	public static void SampleHeight(ref WaterSurfaceData<SurfaceWater> data, ref TerrainHeightData terrainData, float3 worldPosition, out float terrainHeight, out float waterHeight, out float waterDepth)
	{
		terrainHeight = TerrainUtils.SampleHeight(ref terrainData, worldPosition);
		waterDepth = SampleDepth(ref data, worldPosition);
		waterHeight = terrainHeight + waterDepth;
	}

	public static float GetSurfaceDepth(ref WaterSurfaceData<SurfaceWater> data, int2 surfacePosition)
	{
		return math.max(data.depths[surfacePosition.y * data.resolution.x + surfacePosition.x].m_Depth, 0f);
	}

	public static float3 GetWorldPosition(ref WaterSurfaceData<SurfaceWater> data, int2 surfacePosition)
	{
		return ToWorldSpace(ref data, new float3
		{
			y = GetSurfaceDepth(ref data, surfacePosition),
			xz = surfacePosition
		});
	}

	public static float GetSampleInterval(ref WaterSurfaceData<SurfaceWater> data)
	{
		return math.cmin(1f / data.scale.xz);
	}

	public static bool Raycast(ref WaterSurfacesData watersData, ref TerrainHeightData terrainData, Line3.Segment worldLine, bool outside, out float t, out Bounds3 hitBounds)
	{
		hitBounds = default(Bounds3);
		outside &= watersData.hasBackdrop;
		Line3.Segment terrainLine = TerrainUtils.ToHeightmapSpace(ref terrainData, worldLine);
		Bounds3 terrainBounds = new Bounds3(new float3(0f, -50f, 0f), terrainData.resolution - 1 + new float3(0f, 100f, 0f));
		if (!outside)
		{
			return RaycastInternal(terrainBounds, terrainLine, ref watersData, ref terrainData, worldLine, outside, out t, out hitBounds);
		}
		if (RaycastInternal(terrainBounds, terrainLine, ref watersData, ref terrainData, worldLine, outside: false, out t, out hitBounds))
		{
			return true;
		}
		terrainLine = TerrainUtils.ToBackdropSpace(ref terrainData, worldLine);
		terrainBounds = new Bounds3(new float3(0f, -50f, 0f), terrainData.downScaledResolution - 1 + new float3(0f, 100f, 0f));
		return RaycastInternal(terrainBounds, terrainLine, ref watersData, ref terrainData, worldLine, outside, out t, out hitBounds);
	}

	private static bool RaycastInternal(Bounds3 terrainBounds, Line3.Segment terrainLine, ref WaterSurfacesData watersData, ref TerrainHeightData terrainData, Line3.Segment worldLine, bool outside, out float t, out Bounds3 hitBounds)
	{
		WaterSurfaceData<SurfaceWater> data = (outside ? watersData.downscaledDepths : watersData.depths);
		hitBounds = default(Bounds3);
		float2 t2;
		if (outside)
		{
			if (!MathUtils.Intersect(terrainBounds.y, terrainLine.y, out t2))
			{
				t = 2f;
				return false;
			}
		}
		else if (!MathUtils.Intersect(terrainBounds, terrainLine, out t2))
		{
			t = 2f;
			return false;
		}
		Line3.Segment line = (outside ? ToBackdropSpace(ref data, worldLine) : ToSurfaceSpace(ref data, worldLine));
		line = MathUtils.Cut(line, t2);
		float2 terrainToWaterSpace = new float2(data.scale.y / terrainData.scale.y, (0f - terrainData.offset.y) * data.scale.y);
		int2 waterToTerrainFactor = terrainData.resolution.xz / data.resolution.xz;
		float3 x = line.b - line.a;
		float3 @float = math.abs(x);
		float4 float2 = math.floor(new float4(line.a.xz, line.b.xz));
		int4 @int = new int4((int)float2.x, (int)float2.z, (int)float2.y, (int)float2.w);
		if (math.all(@int.xz == @int.yw))
		{
			if (RaycastCell(ref watersData, ref terrainData, line, @int.xz, terrainToWaterSpace, waterToTerrainFactor, outside, out t, out hitBounds))
			{
				t = math.saturate(math.lerp(t2.x, t2.y, t));
				return true;
			}
		}
		else if (@float.x > @float.z)
		{
			int2 int2 = math.select(1, -1, x.xz < 0f);
			@int.y += int2.x;
			float num = (float)math.select(1, 0, x.x < 0f) - line.a.x;
			float num2 = 1f / x.x;
			int2 pos = default(int2);
			pos.x = @int.x;
			while (pos.x != @int.y)
			{
				float t3 = ((float)pos.x + num) * num2;
				@int.w = (int)math.floor(math.lerp(line.a.z, line.b.z, t3)) + int2.y;
				pos.y = @int.z;
				while (pos.y != @int.w)
				{
					if (RaycastCell(ref watersData, ref terrainData, line, pos, terrainToWaterSpace, waterToTerrainFactor, outside, out t, out hitBounds))
					{
						t = math.saturate(math.lerp(t2.x, t2.y, t));
						return true;
					}
					pos.y += int2.y;
				}
				@int.z = @int.w - int2.y;
				pos.x += int2.x;
			}
		}
		else
		{
			int2 int3 = math.select(1, -1, x.xz < 0f);
			@int.w += int3.y;
			float num3 = (float)math.select(1, 0, x.z < 0f) - line.a.z;
			float num4 = 1f / x.z;
			int2 pos2 = default(int2);
			pos2.y = @int.z;
			while (pos2.y != @int.w)
			{
				float t4 = ((float)pos2.y + num3) * num4;
				@int.y = (int)math.floor(math.lerp(line.a.x, line.b.x, t4)) + int3.x;
				pos2.x = @int.x;
				while (pos2.x != @int.y)
				{
					if (RaycastCell(ref watersData, ref terrainData, line, pos2, terrainToWaterSpace, waterToTerrainFactor, outside, out t, out hitBounds))
					{
						t = math.saturate(math.lerp(t2.x, t2.y, t));
						return true;
					}
					pos2.x += int3.x;
				}
				@int.x = @int.y - int3.x;
				pos2.y += int3.y;
			}
		}
		t = 2f;
		return false;
	}

	private static bool RaycastCell(ref WaterSurfacesData watersData, ref TerrainHeightData terrainData, Line3.Segment localLine, int2 pos, float2 terrainToWaterSpace, int2 waterToTerrainFactor, bool outside, out float t, out Bounds3 hitBounds)
	{
		t = 2f;
		hitBounds = default(Bounds3);
		WaterSurfaceData<SurfaceWater> data = (outside ? watersData.downscaledDepths : watersData.depths);
		int4 @int = math.clamp(new int4(pos, pos + 1), 0, data.resolution.xzxz - 1);
		int4 int2 = @int.yyww * data.resolution.x + @int.xzxz;
		float4 @float = default(float4);
		@float.x = math.max(data.depths[int2.x].m_Depth, 0f);
		@float.y = math.max(data.depths[int2.y].m_Depth, 0f);
		@float.z = math.max(data.depths[int2.z].m_Depth, 0f);
		@float.w = math.max(data.depths[int2.w].m_Depth, 0f);
		float4 float4 = default(float4);
		if (outside)
		{
			float2 float2 = new float2(terrainData.downScaledResolution.xz) / new float2(data.resolution.xz);
			float4 float3 = math.floor(@int * float2.xyxy);
			int4 int3 = math.clamp(new int4((int)float3.x, (int)float3.y, (int)float3.z, (int)float3.w), 0, terrainData.downScaledResolution.xzxz - 1);
			int4 int4 = int3.yyww * terrainData.downScaledResolution.x + int3.xzxz;
			float4.x = (int)terrainData.downscaledHeights[int4.x];
			float4.y = (int)terrainData.downscaledHeights[int4.y];
			float4.z = (int)terrainData.downscaledHeights[int4.z];
			float4.w = (int)terrainData.downscaledHeights[int4.w];
		}
		else
		{
			int4 int5 = math.clamp(@int * waterToTerrainFactor.xyxy, 0, terrainData.resolution.xzxz - 1);
			int4 int6 = int5.yyww * terrainData.resolution.x + int5.xzxz;
			float4.x = (int)terrainData.heights[int6.x];
			float4.y = (int)terrainData.heights[int6.y];
			float4.z = (int)terrainData.heights[int6.z];
			float4.w = (int)terrainData.heights[int6.w];
		}
		float4 x = float4 * terrainToWaterSpace.x + terrainToWaterSpace.y + @float;
		Bounds3 bounds = default(Bounds3);
		@int = math.select(@int, new int4(pos, pos + 1), outside);
		bounds.min = new float3(@int.x, math.cmin(x), @int.y);
		bounds.max = new float3(@int.z, math.cmax(x), @int.w);
		if (MathUtils.Intersect(bounds, localLine, out var _))
		{
			float3 float5 = new float3(bounds.min.x, x.x, bounds.min.z);
			float3 float6 = new float3(bounds.max.x, x.y, bounds.min.z);
			float3 float7 = new float3(bounds.max.x, x.w, bounds.max.z);
			float3 float8 = new float3(bounds.min.x, x.z, bounds.max.z);
			float3 c = MathUtils.Center(bounds);
			if (outside)
			{
				hitBounds.min = ToWorldSpaceFromBackdrop(ref data, bounds.min);
				hitBounds.max = ToWorldSpaceFromBackdrop(ref data, bounds.max);
			}
			else
			{
				hitBounds.min = ToWorldSpace(ref data, bounds.min);
				hitBounds.max = ToWorldSpace(ref data, bounds.max);
			}
			if (MathUtils.Intersect(new Triangle3(float5, float6, c), localLine, out var t3))
			{
				t = math.min(t, t3.z);
			}
			if (MathUtils.Intersect(new Triangle3(float6, float7, c), localLine, out t3))
			{
				t = math.min(t, t3.z);
			}
			if (MathUtils.Intersect(new Triangle3(float7, float8, c), localLine, out t3))
			{
				t = math.min(t, t3.z);
			}
			if (MathUtils.Intersect(new Triangle3(float8, float5, c), localLine, out t3))
			{
				t = math.min(t, t3.z);
			}
			if (t != 2f)
			{
				return true;
			}
		}
		return false;
	}
}
