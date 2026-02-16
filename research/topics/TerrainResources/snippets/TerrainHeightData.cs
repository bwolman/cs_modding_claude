// Source: Game.dll -> Game.Simulation.TerrainHeightData
// Decompiled with ILSpy

using Unity.Collections;
using Unity.Mathematics;

namespace Game.Simulation;

public struct TerrainHeightData
{
	public NativeArray<ushort> heights { get; private set; }

	public NativeArray<ushort> downscaledHeights { get; private set; }

	public int3 resolution { get; private set; }

	public int3 downScaledResolution { get; private set; }

	public float3 scale { get; private set; }

	public float3 offset { get; private set; }

	public bool hasBackdrop { get; private set; }

	public bool isCreated => heights.IsCreated;

	public TerrainHeightData(NativeArray<ushort> _heights, NativeArray<ushort> _downscaledHeights, int3 _resolution, float3 _scale, float3 _offset, bool _hasBackdrop)
	{
		heights = _heights;
		resolution = _resolution;
		scale = _scale;
		offset = _offset;
		downscaledHeights = _downscaledHeights;
		downScaledResolution = _resolution / new int3(TerrainSystem.kDownScaledHeightmapScale, 1, TerrainSystem.kDownScaledHeightmapScale);
		hasBackdrop = _hasBackdrop;
	}
}
