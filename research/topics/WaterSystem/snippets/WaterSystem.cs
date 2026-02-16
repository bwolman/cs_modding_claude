using System;
using System.Runtime.CompilerServices;
using Colossal.AssetPipeline.Native;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Rendering;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.Serialization;
using Game.Tools;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.IO.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Simulation;

[FormerlySerializedAs("Colossal.Terrain.WaterSystem, Game")]
[CompilerGenerated]
public class WaterSystem : GameSystemBase, IDefaultSerializable, ISerializable, IGPUSystem, IPostDeserialize
{
	[Serializable]
	public class WaterMaterialParams : ISerializable
	{
		public float m_foamAmount;

		public float m_ripplesWindSpeed;

		public float m_foamFadeStart;

		public float m_wavesMultiplier;

		public float m_absorbtionDistance;

		public float m_foamFadeDistance;

		public float m_lakeWavesMultiplier;

		public float m_causticsPlaneDistance;

		public float m_causticsIntensity;

		public float m_unused3;

		public UnityEngine.Color m_waterColor;

		public UnityEngine.Color m_waterScatteringColor;

		public float m_unused4;

		public float m_minWaterAmountForWaves;

		public void Init(WaterSurface surface)
		{
			m_foamAmount = surface.simulationFoamAmount;
			m_wavesMultiplier = surface.largeBand0Multiplier;
			m_causticsPlaneDistance = surface.causticsPlaneBlendDistance;
			m_causticsIntensity = surface.causticsIntensity;
			m_absorbtionDistance = surface.absorptionDistance;
			m_ripplesWindSpeed = surface.ripplesWindSpeed;
			m_waterColor = surface.refractionColor;
			m_waterScatteringColor = surface.scatteringColor;
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			ref float value = ref m_foamAmount;
			reader.Read(out value);
			ref float value2 = ref m_wavesMultiplier;
			reader.Read(out value2);
			ref float value3 = ref m_causticsPlaneDistance;
			reader.Read(out value3);
			ref float value4 = ref m_causticsIntensity;
			reader.Read(out value4);
			ref float value5 = ref m_minWaterAmountForWaves;
			reader.Read(out value5);
			ref float value6 = ref m_absorbtionDistance;
			reader.Read(out value6);
			ref float value7 = ref m_ripplesWindSpeed;
			reader.Read(out value7);
			ref UnityEngine.Color value8 = ref m_waterColor;
			reader.Read(out value8);
			ref UnityEngine.Color value9 = ref m_waterScatteringColor;
			reader.Read(out value9);
			ref float value10 = ref m_lakeWavesMultiplier;
			reader.Read(out value10);
			ref float value11 = ref m_foamFadeStart;
			reader.Read(out value11);
			ref float value12 = ref m_foamFadeDistance;
			reader.Read(out value12);
			ref float value13 = ref m_unused3;
			reader.Read(out value13);
			ref float value14 = ref m_unused4;
			reader.Read(out value14);
		}

		public WaterMaterialParams()
		{
			SetDefaults();
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			float value = m_foamAmount;
			writer.Write(value);
			float value2 = m_wavesMultiplier;
			writer.Write(value2);
			float value3 = m_causticsPlaneDistance;
			writer.Write(value3);
			float value4 = m_causticsIntensity;
			writer.Write(value4);
			float value5 = m_minWaterAmountForWaves;
			writer.Write(value5);
			float value6 = m_absorbtionDistance;
			writer.Write(value6);
			float value7 = m_ripplesWindSpeed;
			writer.Write(value7);
			UnityEngine.Color value8 = m_waterColor;
			writer.Write(value8);
			UnityEngine.Color value9 = m_waterScatteringColor;
			writer.Write(value9);
			float value10 = m_lakeWavesMultiplier;
			writer.Write(value10);
			float value11 = m_foamFadeStart;
			writer.Write(value11);
			float value12 = m_foamFadeDistance;
			writer.Write(value12);
			float value13 = m_unused3;
			writer.Write(value13);
			float value14 = m_unused4;
			writer.Write(value14);
		}

		internal void SetDefaults()
		{
			m_waterColor = new UnityEngine.Color(0.14f, 0.62f, 0.62f);
			m_waterScatteringColor = new UnityEngine.Color(0.19f, 0.33f, 0.39f);
			m_foamAmount = 0.3f;
			m_wavesMultiplier = 1f;
			m_causticsIntensity = 1f;
			m_causticsPlaneDistance = 4f;
			m_minWaterAmountForWaves = 10f;
			m_ripplesWindSpeed = 5f;
			m_absorbtionDistance = 8f;
			m_lakeWavesMultiplier = 1f;
			m_foamFadeStart = 500f;
			m_foamFadeDistance = 1500f;
		}
	}

	[Serializable]
	public struct WaterSource
	{
		public int constantDepth;

		public float amount;

		public float2 position;

		public float radius;

		public float pollution;

		public float floodheight;
	}

	public struct QuadWaterBuffer
	{
		public RenderTexture[] waterTextures;

		public RenderTexture[] downdScaledFlowTextures;

		public RenderTexture[] waterBackdropTextures;

		public RenderTexture[] downdScaledBackdropFlowTextures;

		public RenderTexture seaPropagationTexture;

		public RenderTexture maxHeightDownscaled;

		public RenderTexture seaPropagationDownscaled;

		private RenderTexture CreateRenderTexture(string name, int2 size, GraphicsFormat format)
		{
			RenderTexture renderTexture = new RenderTexture(size.x, size.y, 0, format);
			renderTexture.name = name;
			renderTexture.hideFlags = HideFlags.DontSave;
			renderTexture.enableRandomWrite = true;
			renderTexture.wrapMode = TextureWrapMode.Clamp;
			renderTexture.filterMode = FilterMode.Bilinear;
			renderTexture.Create();
			return renderTexture;
		}

		public void Init(int2 size)
		{
			waterTextures = new RenderTexture[2];
			waterTextures[0] = CreateRenderTexture("WaterRT0", size, GraphicsFormat.R32G32B32A32_SFloat);
			waterTextures[1] = CreateRenderTexture("WaterRT1", size, GraphicsFormat.R32G32B32A32_SFloat);
			seaPropagationTexture = CreateRenderTexture("SeaPropagationTexture", size, GraphicsFormat.R16_SFloat);
			waterBackdropTextures = new RenderTexture[2];
			waterBackdropTextures[0] = CreateRenderTexture("WaterBackdropRT0", size / 2, GraphicsFormat.R32G32B32A32_SFloat);
			waterBackdropTextures[1] = CreateRenderTexture("WaterBackdropRT1", size / 2, GraphicsFormat.R32G32B32A32_SFloat);
			downdScaledBackdropFlowTextures = new RenderTexture[3];
			int2 size2 = waterBackdropTextures[0].width / 2;
			for (int i = 0; i < 3; i++)
			{
				downdScaledBackdropFlowTextures[i] = CreateRenderTexture($"BackdropFlowTextureDownScaled{i}", size2, GraphicsFormat.R16G16_SFloat);
			}
			maxHeightDownscaled = CreateRenderTexture("MaxHeightDownscaled", size / 2, GraphicsFormat.R16_SFloat);
			seaPropagationDownscaled = CreateRenderTexture("SeaPropagationDownscaled", size / 4, GraphicsFormat.R16G16B16A16_SFloat);
			downdScaledFlowTextures = new RenderTexture[4];
			for (int j = 0; j < 4; j++)
			{
				size /= 2;
				downdScaledFlowTextures[j] = CreateRenderTexture($"FlowTextureDownScaled{j}", size, GraphicsFormat.R16G16B16A16_SFloat);
				if (j == 2)
				{
					downdScaledFlowTextures[++j] = CreateRenderTexture($"FlowTextureDownScaled{j}", size, GraphicsFormat.R16G16B16A16_SFloat);
				}
			}
		}

		public void Dispose()
		{
			if (waterTextures != null)
			{
				RenderTexture[] array = waterTextures;
				for (int i = 0; i < array.Length; i++)
				{
					CoreUtils.Destroy(array[i]);
				}
			}
			if (waterBackdropTextures != null)
			{
				RenderTexture[] array = waterBackdropTextures;
				for (int i = 0; i < array.Length; i++)
				{
					CoreUtils.Destroy(array[i]);
				}
			}
			if (downdScaledFlowTextures != null)
			{
				RenderTexture[] array = downdScaledFlowTextures;
				for (int i = 0; i < array.Length; i++)
				{
					CoreUtils.Destroy(array[i]);
				}
			}
			if (downdScaledBackdropFlowTextures != null)
			{
				RenderTexture[] array = downdScaledBackdropFlowTextures;
				for (int i = 0; i < array.Length; i++)
				{
					CoreUtils.Destroy(array[i]);
				}
			}
			if (seaPropagationTexture != null)
			{
				CoreUtils.Destroy(seaPropagationTexture);
			}
			if (maxHeightDownscaled != null)
			{
				CoreUtils.Destroy(maxHeightDownscaled);
			}
			if (seaPropagationDownscaled != null)
			{
				CoreUtils.Destroy(seaPropagationDownscaled);
			}
		}

		public RenderTexture FlowDownScaled(int index)
		{
			return downdScaledFlowTextures[index];
		}

		public RenderTexture BackDropFlowDownScaled(int index)
		{
			return downdScaledBackdropFlowTextures[index];
		}
	}

	private struct ReadCommandHelper
	{
		private long m_Position;

		public long currentPosition => m_Position;

		public ReadCommandHelper(int position = 0)
		{
			m_Position = position;
		}

		public unsafe ReadCommand CreateReadCmd(long size, void* buffer = null)
		{
			ReadCommand result = default(ReadCommand);
			result.Offset = m_Position;
			result.Size = size;
			if (buffer == null)
			{
				result.Buffer = UnsafeUtility.Malloc(result.Size, 16, Allocator.Temp);
			}
			else
			{
				result.Buffer = buffer;
			}
			m_Position += size;
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<WaterLevelChange> __Game_Events_WaterLevelChange_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterLevelChangeData> __Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_WaterLevelChange_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterLevelChange>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterSourceData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup = state.GetComponentLookup<WaterLevelChangeData>(isReadOnly: true);
		}
	}

	public static readonly int kMapSize = 14336;

	public const float kMaxSourceHeight = 250f;

	public const float kMaxSourceRadius = 2500f;

	public const float kMaxSeaLevel = 2000f;

	public static readonly float kDefaultMinWaterToRestoreHeight = 0f;

	public const float kDefaultSeaLevel = 511.7f;

	private float m_SeaLevel;

	private ActiveWaterTilesHelper m_waterSimActiveTilesHelper;

	private ActiveWaterTilesHelper m_waterBackdropSimActiveTilesHelper;

	private WaterSimulation m_waterSim;

	private WaterSimulationLegacy m_waterSimLegacy;

	private bool m_seaLevelChanged;

	private bool m_Loaded;

	public const int MAX_FLOW_DOWNSCALE = 3;

	private int m_NumFlowDownsample = 3;

	private bool m_UseLegacyWaterSources = true;

	private bool m_SimulateBackdrop;

	private int m_NextSourceId;

	public float MaxFlowlengthForRender = 0.4f;

	public float PostFlowspeedMultiplier = 2f;

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private SoilWaterSystem m_SoilWaterSystem;

	private SnowSystem m_SnowSystem;

	private EntityQuery m_SourceGroup;

	private EntityQuery m_SoilWaterParameterGroup;

	private EntityQuery m_WaterLevelChangeGroup;

	private NativeList<WaterSourceCache> m_SourceCache1;

	private NativeList<WaterSourceCache> m_SourceCache2;

	private JobHandle m_SourceHandle;

	private int m_SourceCacheIndex;

	private bool m_FlipSourceCache;

	private PrefabSystem m_PrefabSystem;

	private int m_lastFrameGridSize;

	private const float kGravity = 9.81f;

	private const int kGridSize = 32;

	public static readonly float kCellSize = 7f;

	private Bounds2 m_terrainChangeArea;

	private bool m_terrainChangeAreaSet;

	private float m_lastFrameTimeStep;

	internal const int kBackdropWaterScale = 2;

	private QuadWaterBuffer m_Water;

	private SurfaceDataReader m_depthsReader;

	private SurfaceDataReader m_velocitiesReader;

	private JobHandle m_ActiveReaders;

	private uint m_LastReadyFrame;

	private uint m_PreviousReadyFrame;

	private int m_ID_WavesMutiplier;

	private int m_SubFrame;

	private int m_ID_LakeWavesMutiplier;

	private SurfaceDataReader m_depthsBackdropReader;

	private HeightDataReader m_maxHeightReader;

	private int m_ID_MinWaterAmountForWaves;

	private int m_ID_WaterTexture;

	private int m_ID_WaterTexture_TexelSize;

	private int m_ID_WaterRenderTexture;

	private int m_ID_WateRenderrTexture_TexelSize;

	private int m_ID_FlowTexture;

	private int m_ID_FlowTexture_TexelSize;

	private int m_ID_FlowUnprocessTexture;

	private int m_ID_SeaPropagationTexture;

	private int m_ID_SeaPropagationTexture_TexelSize;

	private int m_ID_WaterBackdropTexture;

	private int m_ID_WaterBackdropTexture_TexelSize;

	private int m_ID_FlowTextureBackdrop;

	private int m_ID_SeaPropagationDownscaledBlurTexture;

	private int2 m_TexSize;

	private int m_NewMap;

	private int m_terrainChangeCounter;

	private float m_restoreHeightMinWaterHeight = kDefaultMinWaterToRestoreHeight;

	private int m_terrainReady;

	private int m_FailCount;

	public WaterMaterialParams m_WaterMaterialParams;

	private WindTextureSystem m_windTextureSystem;

	private ulong m_NextSimulationFrame;

	private ulong m_LastReadbackRequest;

	private ulong m_LastDepthReadbackRequest;

	private CommandBuffer m_CommandBuffer;

	private WaterRenderSystem m_WaterRenderSystem;

	private AsyncGPUReadbackHelper m_SaveAsyncGPUReadback;

	private static ProfilerMarker m_DepthUpdate = new ProfilerMarker("UpdateDepthMap");

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1000286787_0;

	public float SeaLevel
	{
		get
		{
			return m_SeaLevel;
		}
		set
		{
			m_SeaLevel = value;
			UpdateSeaLevel();
		}
	}

	public float SeaFlowDirection { get; set; }

	public float SeaFlowStrength { get; set; }

	internal ActiveWaterTilesHelper WaterSimActiveTilesHelper => m_waterSimActiveTilesHelper;

	internal ActiveWaterTilesHelper WaterBackdropSimActiveTilesHelper => m_waterBackdropSimActiveTilesHelper;

	public IWaterSimulation WaterSimulation
	{
		get
		{
			if (!m_UseLegacyWaterSources)
			{
				return m_waterSim;
			}
			return m_waterSimLegacy;
		}
	}

	public bool ForceUpdateFlow { get; set; }

	public float MaxVelocity
	{
		get
		{
			return WaterSimulation.MaxVelocity;
		}
		set
		{
			WaterSimulation.MaxVelocity = value;
		}
	}

	public bool SeaLevelChanged => m_seaLevelChanged;

	public int WaterSimSpeed { get; set; }

	public float TimeStepOverride { get; set; }

	public bool Loaded => m_Loaded;

	public bool UseActiveCellsCulling { get; set; } = true;

	public int2 TextureSize => m_TexSize;

	public RenderTexture WaterTexture => m_Water.waterTextures[0];

	public RenderTexture WaterBackdropTexture => m_Water.waterBackdropTextures[0];

	public RenderTexture WaterBackdropRenderTexture => m_Water.waterBackdropTextures[1];

	public RenderTexture WaterRenderTexture => m_Water.waterTextures[1];

	public RenderTexture MaxHeightDownscaled => m_Water.maxHeightDownscaled;

	public RenderTexture SeaPropagationDownscaled => m_Water.seaPropagationDownscaled;

	public RenderTexture SeaPropagationTexture => m_Water.seaPropagationTexture;

	public bool BlurFlowMap { get; set; } = true;

	public bool FlowPostProcess { get; set; } = true;

	public bool UseLegacyWaterSources
	{
		get
		{
			return m_UseLegacyWaterSources;
		}
		set
		{
			m_UseLegacyWaterSources = value;
			if (!m_UseLegacyWaterSources)
			{
				UpgradeToNewWaterSystem();
			}
		}
	}

	public bool simulateBackdrop
	{
		get
		{
			return m_SimulateBackdrop;
		}
		set
		{
			m_SimulateBackdrop = value;
			OnBackdropActiveChanged();
		}
	}

	public int FlowMapNumDownscale
	{
		get
		{
			return m_NumFlowDownsample;
		}
		set
		{
			m_NumFlowDownsample = value;
			Shader.SetGlobalTexture("colossal_FlowTexture", FlowTextureUpdated);
			Shader.SetGlobalVector("colossal_FlowTexture_TexelSize", new Vector4(FlowTextureUpdated.width, FlowTextureUpdated.height, 1f / (float)FlowTextureUpdated.width));
		}
	}

	public bool EnableFlowDownscale
	{
		get
		{
			return m_NumFlowDownsample > 1;
		}
		set
		{
			if (value)
			{
				FlowMapNumDownscale = 3;
			}
			else
			{
				FlowMapNumDownscale = 0;
			}
		}
	}

	public Texture FlowTextureUpdated
	{
		get
		{
			if (FlowMapNumDownscale > 0)
			{
				return m_Water.FlowDownScaled(FlowMapNumDownscale - 1);
			}
			return WaterRenderTexture;
		}
	}

	public Texture FlowTextureUnprocess
	{
		get
		{
			if (FlowMapNumDownscale > 0)
			{
				return m_Water.FlowDownScaled(FlowMapNumDownscale);
			}
			return WaterRenderTexture;
		}
	}

	public float CellSize => kCellSize;

	public float BackdropCellSize => kCellSize * 2f * 4f;

	public float2 MapSize => kCellSize * new float2(m_TexSize.x, m_TexSize.y);

	public int GridSizeMultiplier { get; set; } = 3;

	public int GridSize => 32 * (1 << GridSizeMultiplier);

	public int MaxSpeed { get; set; }

	public int SimulationCycleSteps => 3;

	private int ReadbackRequestInterval => 8;

	private int DepthReadbackRequestInterval => 30;

	public static float WaveSpeed => kCellSize / 30f;

	public int BackdropGridCellSize => m_waterBackdropSimActiveTilesHelper.GridCellSize;

	public bool IsNewMap => m_NewMap > 0;

	public bool IsAsync { get; set; }

	private NativeList<WaterSourceCache> LastFrameSourceCache
	{
		get
		{
			if (m_SourceCacheIndex != 0)
			{
				return m_SourceCache2;
			}
			return m_SourceCache1;
		}
	}

	private NativeList<WaterSourceCache> CurrentJobSourceCache
	{
		get
		{
			if (m_SourceCacheIndex != 1)
			{
				return m_SourceCache2;
			}
			return m_SourceCache1;
		}
	}

	public RenderTexture FlowDownScaled(int index)
	{
		return m_Water.FlowDownScaled(index);
	}

	public RenderTexture BackDropFlowDownScaled(int index)
	{
		return m_Water.BackDropFlowDownScaled(index);
	}

	public int GetNextSourceId()
	{
		return m_NextSourceId++;
	}

	public NativeArray<SurfaceWater> GetDepths(out JobHandle deps)
	{
		deps = m_depthsReader.JobWriters;
		return m_depthsReader.WaterSurfaceCPUArray;
	}

	public WaterSurfaceData<SurfaceWater> GetSurfaceData(out JobHandle deps)
	{
		return m_depthsReader.GetSurfaceData(out deps);
	}

	public WaterSurfacesData GetSurfacesData(out JobHandle deps)
	{
		WaterSurfaceData<SurfaceWater> surfaceData = m_depthsReader.GetSurfaceData(out deps);
		WaterSurfaceData<SurfaceWater> surfaceData2 = m_depthsBackdropReader.GetSurfaceData(out deps);
		return new WaterSurfacesData(surfaceData, surfaceData2, m_SimulateBackdrop);
	}

	public WaterSurfaceData<SurfaceWater> GetVelocitiesSurfaceData(out JobHandle deps)
	{
		return m_velocitiesReader.GetSurfaceData(out deps);
	}

	public void AddSurfaceReader(JobHandle handle)
	{
		m_depthsReader.JobReaders = JobHandle.CombineDependencies(m_depthsReader.JobReaders, handle);
	}

	public void AddVelocitySurfaceReader(JobHandle handle)
	{
		m_velocitiesReader.JobReaders = JobHandle.CombineDependencies(m_velocitiesReader.JobReaders, handle);
	}

	public WaterSurfaceData<half> GetMexHeightSurfaceData(out JobHandle deps)
	{
		return m_maxHeightReader.GetSurfaceData(out deps, !m_UseLegacyWaterSources);
	}

	public void AddMaxHeightSurfaceReader(JobHandle handle)
	{
		m_maxHeightReader.JobReaders = JobHandle.CombineDependencies(m_maxHeightReader.JobReaders, handle);
	}

	public void AddActiveReader(JobHandle handle)
	{
		m_ActiveReaders = JobHandle.CombineDependencies(m_ActiveReaders, handle);
	}

	public NativeArray<int> GetActive()
	{
		return m_waterSimActiveTilesHelper.GetActive();
	}

	private static float2 GetCellCoords(float3 position, int mapSize, int2 textureSize)
	{
		float2 @float = (float)mapSize / (float2)textureSize;
		return new float2(((float)(mapSize / 2) + position.x) / @float.x, ((float)(mapSize / 2) + position.z) / @float.y);
	}

	public static int2 GetCell(float3 position, int mapSize, int2 textureSize)
	{
		float2 cellCoords = GetCellCoords(position, mapSize, textureSize);
		return new int2(Mathf.FloorToInt(cellCoords.x), Mathf.FloorToInt(cellCoords.y));
	}

	public NativeArray<int> GetActiveBackdrop()
	{
		return m_waterBackdropSimActiveTilesHelper.GetActive();
	}

	private void OnBackdropActiveChanged()
	{
		InitBackdropTexture();
		m_waterSim.ResetBackdropWaterToSeaLevel();
		m_NewMap = 16;
		for (int i = 0; i < 16; i++)
		{
			Simulate(m_CommandBuffer);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_SoilWaterSystem = base.World.GetOrCreateSystemManaged<SoilWaterSystem>();
		m_SnowSystem = base.World.GetOrCreateSystemManaged<SnowSystem>();
		m_WaterRenderSystem = base.World.GetOrCreateSystemManaged<WaterRenderSystem>();
		m_windTextureSystem = base.World.GetOrCreateSystemManaged<WindTextureSystem>();
		m_waterSim = new WaterSimulation(this, m_TerrainSystem);
		m_waterSimLegacy = new WaterSimulationLegacy(this, m_TerrainSystem);
		InitShader();
		RequireForUpdate<TerrainPropertiesData>();
		m_SourceGroup = GetEntityQuery(ComponentType.ReadOnly<WaterSourceData>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_SoilWaterParameterGroup = GetEntityQuery(ComponentType.ReadOnly<SoilWaterParameterData>());
		m_WaterLevelChangeGroup = GetEntityQuery(ComponentType.ReadOnly<WaterLevelChange>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>());
		WaterSimSpeed = 1;
		m_Loaded = false;
		m_CommandBuffer = new CommandBuffer();
		m_CommandBuffer.name = "WaterSystemCommandBuffer";
		m_SourceCache1 = new NativeList<WaterSourceCache>(Allocator.Persistent);
		m_SourceCache2 = new NativeList<WaterSourceCache>(Allocator.Persistent);
		InitTextures();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_WaterMaterialParams = new WaterMaterialParams();
	}

	private bool HasWater(float3 position)
	{
		float2 @float = (float)kMapSize / (float2)m_TexSize;
		int2 cell = GetCell(position - new float3(@float.x / 2f, 0f, @float.y / 2f), kMapSize, m_TexSize);
		_ = GetCellCoords(position, kMapSize, m_TexSize) - new float2(0.5f, 0.5f) - cell;
		cell.x = math.max(0, cell.x);
		cell.x = math.min(m_TexSize.x - 2, cell.x);
		cell.y = math.max(0, cell.y);
		cell.y = math.min(m_TexSize.y - 2, cell.y);
		if (m_depthsReader.GetSurface(cell).m_Depth > 0f)
		{
			return true;
		}
		return false;
	}

	public void TerrainWillChangeFromBrush(Bounds2 area)
	{
		if (!GameManager.instance.isGameLoading)
		{
			if (m_terrainChangeAreaSet)
			{
				m_terrainChangeArea.Merge(area);
			}
			else
			{
				m_terrainChangeArea = area;
			}
			m_terrainChangeAreaSet = true;
			m_terrainChangeCounter = 15;
			WaterSimSpeed = 0;
		}
	}

	public void TerrainWillChange()
	{
		m_terrainChangeCounter = 1;
		WaterSimSpeed = 0;
	}

	private void InitTextures()
	{
		m_TexSize = new int2(2048, 2048);
		m_Water = default(QuadWaterBuffer);
		m_Water.Init(m_TexSize);
		int2 gridSize = m_TexSize / GridSize;
		m_waterSimActiveTilesHelper = new ActiveWaterTilesHelper(gridSize, m_TexSize.x, m_ActiveReaders);
		m_waterBackdropSimActiveTilesHelper = new ActiveWaterTilesHelper(gridSize, m_TexSize.x / 2, m_ActiveReaders);
		if (m_depthsReader != null)
		{
			m_depthsReader.Dispose();
		}
		if (m_velocitiesReader != null)
		{
			m_velocitiesReader.Dispose();
		}
		if (m_maxHeightReader != null)
		{
			m_maxHeightReader.Dispose();
		}
		m_depthsReader = new SurfaceDataReader(WaterTexture, kMapSize, GraphicsFormat.R32G32B32A32_SFloat);
		m_velocitiesReader = new SurfaceDataReader(m_Water.FlowDownScaled(0), kMapSize, GraphicsFormat.R32G32B32A32_SFloat);
		m_maxHeightReader = new HeightDataReader(MaxHeightDownscaled, kMapSize, GraphicsFormat.R16_SFloat);
		if (m_depthsBackdropReader != null)
		{
			m_depthsBackdropReader.Dispose();
		}
		m_depthsBackdropReader = new SurfaceDataReader();
		m_NewMap = 5;
	}

	private void InitShader()
	{
		m_ID_WavesMutiplier = Shader.PropertyToID("_WavesMultiplier");
		m_ID_LakeWavesMutiplier = Shader.PropertyToID("_LakeWavesMultiplier");
		m_ID_WaterTexture = Shader.PropertyToID("colossal_WaterTexture");
		m_ID_WaterTexture_TexelSize = Shader.PropertyToID("colossal_WaterTexture_TexelSize");
		m_ID_WaterRenderTexture = Shader.PropertyToID("colossal_WaterRenderTexture");
		m_ID_WateRenderrTexture_TexelSize = Shader.PropertyToID("colossal_WateRenderrTexture_TexelSize");
		m_ID_FlowTexture = Shader.PropertyToID("colossal_FlowTexture");
		m_ID_FlowTexture_TexelSize = Shader.PropertyToID("colossal_FlowTexture_TexelSize");
		m_ID_FlowUnprocessTexture = Shader.PropertyToID("colossal_FlowUnprocessTexture");
		m_ID_SeaPropagationTexture = Shader.PropertyToID("colossal_SeaPropagationTexture");
		m_ID_SeaPropagationTexture_TexelSize = Shader.PropertyToID("colossal_SeaPropagationTexture_TexelSize");
		m_ID_WaterBackdropTexture = Shader.PropertyToID("colossal_WaterBackdropTexture");
		m_ID_WaterBackdropTexture_TexelSize = Shader.PropertyToID("colossal_WaterBackdropTexture_TexelSize");
		m_ID_FlowTextureBackdrop = Shader.PropertyToID("colossal_FlowTextureBackdrop");
		m_ID_SeaPropagationDownscaledBlurTexture = Shader.PropertyToID("colossal_SeaPropagationDownscaledBlur");
		Shader.SetGlobalTexture("colossal_WaterTexture", Texture2D.whiteTexture);
		Shader.SetGlobalVector("colossal_WaterTexture_TexelSize", Vector4.one);
		m_waterSim.InitShader();
		m_waterSimLegacy.InitShader();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Loaded = false;
		Shader.SetGlobalVector("colossal_WaterParams", new Vector4(0f, 0f, 0f, 0f));
		m_SeaLevel = 0f;
		if (m_velocitiesReader != null)
		{
			m_velocitiesReader.Dispose();
		}
		if (m_maxHeightReader != null)
		{
			m_maxHeightReader.Dispose();
		}
		if (m_depthsReader != null)
		{
			m_depthsReader.Dispose();
		}
		if (m_depthsBackdropReader != null)
		{
			m_depthsBackdropReader.Dispose();
		}
		m_SourceCache2.Dispose();
		m_SourceCache1.Dispose();
		m_waterSimActiveTilesHelper.Dispose();
		m_waterBackdropSimActiveTilesHelper.Dispose();
		m_ActiveReaders.Complete();
		m_CommandBuffer.Release();
		m_Water.Dispose();
		m_waterSim.OnDestroy();
		m_waterSimLegacy.OnDestroy();
		base.OnDestroy();
	}

	public float4 GetSeaFlowParams()
	{
		float num = math.radians(SeaFlowDirection);
		float2 xy = new float2((float)Math.Cos(num), (float)Math.Sin(num));
		return new float4(xy, SeaFlowStrength, PostFlowspeedMultiplier);
	}

	public unsafe void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint value = m_PreviousReadyFrame;
		writer.Write(value);
		uint value2 = m_LastReadyFrame;
		writer.Write(value2);
		ulong value3 = m_NextSimulationFrame;
		writer.Write(value3);
		int x = m_TexSize.x;
		writer.Write(x);
		int y = m_TexSize.y;
		writer.Write(y);
		NativeArray<float4> output = new NativeArray<float4>(WaterTexture.width * WaterTexture.height, Allocator.Persistent);
		AsyncGPUReadback.RequestIntoNativeArray(ref output, WaterTexture).WaitForCompletion();
		NativeArray<byte> nativeArray = new NativeArray<byte>(output.Length * UnsafeUtility.SizeOf(typeof(float4)), Allocator.Temp);
		NativeCompression.FilterDataBeforeWrite((IntPtr)output.GetUnsafeReadOnlyPtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray.Length, UnsafeUtility.SizeOf(typeof(float4)));
		output.Dispose();
		NativeArray<byte> value4 = nativeArray;
		writer.Write(value4);
		nativeArray.Dispose();
		float value5 = m_SeaLevel;
		writer.Write(value5);
		bool value6 = m_SimulateBackdrop;
		writer.Write(value6);
		int value7 = m_NextSourceId;
		writer.Write(value7);
		int width = WaterBackdropRenderTexture.width;
		writer.Write(width);
		int height = WaterBackdropRenderTexture.height;
		writer.Write(height);
		bool value8 = m_UseLegacyWaterSources;
		writer.Write(value8);
		output = new NativeArray<float4>(WaterBackdropRenderTexture.width * WaterBackdropRenderTexture.height, Allocator.Persistent);
		AsyncGPUReadback.RequestIntoNativeArray(ref output, WaterBackdropRenderTexture).WaitForCompletion();
		nativeArray = new NativeArray<byte>(output.Length * sizeof(float4), Allocator.Temp);
		NativeCompression.FilterDataBeforeWrite((IntPtr)output.GetUnsafeReadOnlyPtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray.Length, sizeof(float4));
		output.Dispose();
		NativeArray<byte> value9 = nativeArray;
		writer.Write(value9);
		nativeArray.Dispose();
	}

	public static bool SourceMatchesDirection(WaterSourceData source, Game.Objects.Transform transform, float2 direction)
	{
		if (math.abs(transform.m_Position.x) > math.abs(transform.m_Position.z))
		{
			return math.sign(transform.m_Position.x) != math.sign(direction.x);
		}
		return math.sign(transform.m_Position.z) != math.sign(direction.y);
	}

	private void InitBackdropTexture()
	{
		if (m_depthsBackdropReader != null)
		{
			m_depthsBackdropReader.Dispose();
		}
		if (m_SimulateBackdrop)
		{
			m_depthsBackdropReader = new SurfaceDataReader(WaterBackdropTexture, kMapSize, GraphicsFormat.R32G32B32A32_SFloat);
		}
		else
		{
			m_depthsBackdropReader = new SurfaceDataReader();
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.waterInterpolationFix)
		{
			ref uint value = ref m_PreviousReadyFrame;
			reader.Read(out value);
			ref uint value2 = ref m_LastReadyFrame;
			reader.Read(out value2);
		}
		else
		{
			reader.Read(out float value3);
			reader.Read(out float value4);
			m_PreviousReadyFrame = (uint)Mathf.RoundToInt(value3);
			m_LastReadyFrame = (uint)Mathf.RoundToInt(value4);
		}
		if (reader.context.version >= Version.waterElectricityID)
		{
			if (reader.context.version < Version.waterOverflowFix)
			{
				reader.Read(out uint value5);
				m_NextSimulationFrame = value5;
			}
			else
			{
				ref ulong value6 = ref m_NextSimulationFrame;
				reader.Read(out value6);
			}
			reader.Read(out int value7);
			reader.Read(out int value8);
			if (value7 != m_TexSize.x)
			{
				UnityEngine.Debug.LogWarning("Saved water width = " + value7 + ", water tex width = " + m_TexSize.x);
			}
			if (value8 != m_TexSize.y)
			{
				UnityEngine.Debug.LogWarning("Saved water height = " + value8 + ", water tex height = " + m_TexSize.y);
			}
			int num = 0;
			if (reader.context.version < Version.waterGridNotNeeded)
			{
				reader.Read(out int value9);
				if (value9 > 0)
				{
					num = value7 * value8 / (value9 * value9);
					NativeArray<int> nativeArray = new NativeArray<int>(num, Allocator.Temp);
					NativeArray<int> value10 = nativeArray;
					reader.Read(value10);
					nativeArray.Dispose();
				}
			}
			m_waterSim.LoadWaterData(reader, value7, value8, WaterTexture, m_depthsReader);
			CurrentJobSourceCache.Clear();
			LastFrameSourceCache.Clear();
			m_CommandBuffer.SetRenderTarget(m_Water.seaPropagationTexture);
			m_CommandBuffer.ClearRenderTarget(clearDepth: false, clearColor: true, UnityEngine.Color.black);
			m_CommandBuffer.SetRenderTarget(m_Water.FlowDownScaled(0));
			m_CommandBuffer.ClearRenderTarget(clearDepth: false, clearColor: true, UnityEngine.Color.black);
			m_CommandBuffer.SetRenderTarget(m_Water.BackDropFlowDownScaled(0));
			m_CommandBuffer.ClearRenderTarget(clearDepth: false, clearColor: true, UnityEngine.Color.black);
			m_waterSim.ResetActive(m_CommandBuffer);
			Graphics.ExecuteCommandBuffer(m_CommandBuffer);
			m_waterSimActiveTilesHelper.RequestReadback();
			m_waterBackdropSimActiveTilesHelper.RequestReadback();
			m_LastReadbackRequest = 0uL;
			m_LastDepthReadbackRequest = 0uL;
			m_NextSimulationFrame = 0uL;
			m_PreviousReadyFrame = 0u;
			m_CommandBuffer.Clear();
			BindTextures();
			m_depthsReader.ExecuteReadBack();
			m_velocitiesReader.ExecuteReadBack();
			m_maxHeightReader.ExecuteReadBack();
			m_SimulateBackdrop = false;
			m_SeaLevel = 0f;
			m_NextSourceId = 0;
			m_UseLegacyWaterSources = true;
			if (reader.context.format.Has(FormatTags.NewWaterSources))
			{
				ref float value11 = ref m_SeaLevel;
				reader.Read(out value11);
				ref bool value12 = ref m_SimulateBackdrop;
				reader.Read(out value12);
				ref int value13 = ref m_NextSourceId;
				reader.Read(out value13);
				reader.Read(out value7);
				reader.Read(out value8);
				ref bool value14 = ref m_UseLegacyWaterSources;
				reader.Read(out value14);
				InitBackdropTexture();
				m_waterSim.LoadWaterData(reader, value7, value8, WaterBackdropTexture, m_depthsBackdropReader);
				if (m_SimulateBackdrop)
				{
					m_depthsBackdropReader.ExecuteReadBack();
				}
			}
			m_NewMap = 5;
		}
		m_terrainReady = int.MaxValue;
		Shader.SetGlobalVector("colossal_WaterParams", new Vector4(m_SeaLevel, m_SimulateBackdrop ? 1f : 0f, 0f, 0f));
	}

	public void SetDefaults(Context context)
	{
		m_LastReadbackRequest = 0uL;
		m_LastDepthReadbackRequest = 0uL;
		m_PreviousReadyFrame = 0u;
		m_LastReadyFrame = 0u;
		m_NextSimulationFrame = 0uL;
		m_SeaLevel = 511.7f;
		m_UseLegacyWaterSources = false;
		Reset();
		BindTextures();
		m_NewMap = 5;
		m_Loaded = true;
		m_WaterMaterialParams.SetDefaults();
		Shader.SetGlobalVector("colossal_WaterParams", new Vector4(511.7f, 1f, 0f, 0f));
	}

	public void Reset()
	{
		m_waterSim.Reset();
	}

	public void UpdateSeaLevel()
	{
		UseActiveCellsCulling = false;
		m_waterSim.ResetBackdropWaterToSeaLevel();
		m_seaLevelChanged = true;
	}

	private void BindTextures()
	{
		Shader.SetGlobalTexture(m_ID_WaterTexture, WaterTexture);
		Shader.SetGlobalVector(m_ID_WaterTexture_TexelSize, new Vector4(WaterRenderTexture.width, WaterRenderTexture.height, 1f / (float)WaterRenderTexture.width, 1f / (float)WaterRenderTexture.height));
		Shader.SetGlobalTexture(m_ID_WaterRenderTexture, WaterRenderTexture);
		Shader.SetGlobalVector(m_ID_WateRenderrTexture_TexelSize, new Vector4(WaterRenderTexture.width, WaterRenderTexture.height, 1f / (float)WaterRenderTexture.width, 1f / (float)WaterRenderTexture.height));
		Shader.SetGlobalTexture(m_ID_FlowTexture, FlowTextureUpdated);
		Shader.SetGlobalVector(m_ID_FlowTexture_TexelSize, new Vector4(FlowTextureUpdated.width, FlowTextureUpdated.height, 1f / (float)FlowTextureUpdated.width));
		Shader.SetGlobalTexture(m_ID_FlowUnprocessTexture, FlowTextureUnprocess);
		Shader.SetGlobalTexture(m_ID_SeaPropagationTexture, m_Water.seaPropagationTexture);
		Shader.SetGlobalVector(m_ID_SeaPropagationTexture_TexelSize, new Vector4(m_Water.seaPropagationTexture.width, m_Water.seaPropagationTexture.height, 1f / (float)m_Water.seaPropagationTexture.width));
		Shader.SetGlobalTexture(m_ID_WaterBackdropTexture, WaterBackdropTexture);
		Shader.SetGlobalVector(m_ID_WaterBackdropTexture_TexelSize, new Vector4(WaterBackdropTexture.width, WaterBackdropTexture.height, 1f / (float)WaterBackdropTexture.width, 1f / (float)WaterBackdropTexture.height));
		Shader.SetGlobalTexture(m_ID_FlowTextureBackdrop, m_Water.downdScaledBackdropFlowTextures[2]);
		Shader.SetGlobalTexture(m_ID_SeaPropagationDownscaledBlurTexture, m_Water.seaPropagationDownscaled);
	}

	public void UpgradeToNewWaterSystem()
	{
		WaterSimSpeed = 0;
		m_SourceGroup.CompleteDependency();
		NativeArray<Entity> nativeArray = m_SourceGroup.ToEntityArray(Allocator.TempJob);
		EntityManager entityManager = base.World.EntityManager;
		float num = float.MaxValue;
		bool flag = false;
		JobHandle deps;
		WaterSurfaceData<SurfaceWater> data = GetSurfaceData(out deps);
		m_TerrainSystem.AddCPUHeightReader(deps);
		TerrainHeightData data2 = m_TerrainSystem.GetHeightData();
		deps.Complete();
		if (data2.isCreated)
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				WaterSourceData componentData = entityManager.GetComponentData<WaterSourceData>(entity);
				if (componentData.m_ConstantDepth == 3)
				{
					num = math.min(num, componentData.m_Height);
					entityManager.DestroyEntity(entity);
					flag = true;
					continue;
				}
				if (componentData.m_ConstantDepth == 2)
				{
					num = math.min(num, componentData.m_Height);
					flag = true;
				}
				Game.Objects.Transform componentData2 = entityManager.GetComponentData<Game.Objects.Transform>(entity);
				Bounds3 bounds = TerrainUtils.GetBounds(ref data2);
				if (!MathUtils.Intersect(bounds.xz, componentData2.m_Position.xz))
				{
					componentData2.m_Position.xz = MathUtils.Clamp(componentData2.m_Position.xz, bounds.xz);
				}
				float num2 = WaterUtils.SampleHeight(ref data, ref data2, componentData2.m_Position);
				componentData2.m_Position.y = num2;
				float num3 = TerrainUtils.SampleHeight(ref data2, componentData2.m_Position);
				componentData.m_Height = num2 - num3;
				componentData.m_Id = GetNextSourceId();
				entityManager.SetComponentData(entity, componentData2);
				entityManager.SetComponentData(entity, componentData);
			}
		}
		nativeArray.Dispose();
		if (flag)
		{
			m_SeaLevel = num;
		}
		m_NewMap = 3;
		WaterSimSpeed = 1;
	}

	private void UpdateWaterSourcesY()
	{
		m_SourceGroup.CompleteDependency();
		NativeArray<Entity> nativeArray = m_SourceGroup.ToEntityArray(Allocator.TempJob);
		EntityManager entityManager = base.World.EntityManager;
		GetSurfaceData(out var deps);
		m_TerrainSystem.AddCPUHeightReader(deps);
		m_TerrainSystem.AddCPUDownsampleHeightReader(deps);
		m_TerrainSystem.TriggerAsyncChange();
		TerrainHeightData data = m_TerrainSystem.GetHeightData(waitForPending: true);
		deps.Complete();
		if (data.isCreated)
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Game.Objects.Transform componentData = entityManager.GetComponentData<Game.Objects.Transform>(entity);
				float y = TerrainUtils.SampleHeight(ref data, componentData.m_Position);
				componentData.m_Position.y = y;
				entityManager.SetComponentData(entity, componentData);
			}
		}
		nativeArray.Dispose();
	}

	private void UpdateWaterSourcesHeights()
	{
		m_SourceGroup.CompleteDependency();
		NativeArray<Entity> nativeArray = m_SourceGroup.ToEntityArray(Allocator.TempJob);
		EntityManager entityManager = base.World.EntityManager;
		GetSurfaceData(out var deps);
		m_TerrainSystem.AddCPUHeightReader(deps);
		m_TerrainSystem.AddCPUDownsampleHeightReader(deps);
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		deps.Complete();
		if (data.isCreated)
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Game.Objects.Transform componentData = entityManager.GetComponentData<Game.Objects.Transform>(entity);
				WaterSourceData componentData2 = entityManager.GetComponentData<WaterSourceData>(entity);
				float num = TerrainUtils.SampleHeight(ref data, componentData.m_Position);
				float num2 = componentData.m_Position.y + componentData2.m_Height;
				componentData2.m_Height = Mathf.Max(num2 - num, 1f);
				entityManager.SetComponentData(entity, componentData2);
				componentData.m_Position.y = num;
				entityManager.SetComponentData(entity, componentData);
			}
		}
		nativeArray.Dispose();
	}

	public static float CalculateSourceMultiplier(WaterSourceData source, float3 pos)
	{
		if (source.m_Radius < 0.01f)
		{
			return 0f;
		}
		pos.y = 0f;
		int num = Mathf.CeilToInt(source.m_Radius / kCellSize);
		float num2 = 0f;
		float num3 = source.m_Radius * source.m_Radius;
		int num4 = Mathf.FloorToInt(pos.x / kCellSize) - num;
		int num5 = Mathf.FloorToInt(pos.z / kCellSize) - num;
		for (int i = num4; i <= num4 + 2 * num + 1; i++)
		{
			for (int j = num5; j <= num5 + 2 * num + 1; j++)
			{
				float3 x = new float3((float)i * kCellSize, 0f, (float)j * kCellSize);
				num2 += 1f - math.smoothstep(0f, 1f, math.distancesq(x, pos) / num3);
			}
		}
		if (num2 < 0.001f)
		{
			UnityEngine.Debug.LogWarning($"Warning: water source at {pos} has too small radius to work");
			return 1f;
		}
		return 1f / num2;
	}

	public bool HasActiveGridSizeChanged(CommandBuffer cmd)
	{
		bool result = false;
		int2 gridCount = m_TexSize / GridSize;
		if (m_lastFrameGridSize != GridSize)
		{
			m_ActiveReaders.Complete();
			m_waterSimActiveTilesHelper.Reset(gridCount);
			m_waterBackdropSimActiveTilesHelper.Reset(gridCount);
			m_waterSim.ResetActive(cmd);
			m_lastFrameGridSize = GridSize;
			result = true;
		}
		return result;
	}

	public void ResetToSealevel()
	{
		m_waterSim.ResetToLevel(m_SeaLevel, WaterTexture, isBackdrop: false, checkSeaPropataion: true);
		m_waterSim.ResetBackdropWaterToSeaLevel();
		UseActiveCellsCulling = false;
	}

	private void ResetSeaPropgagtion(CommandBuffer cmd)
	{
		cmd.SetRenderTarget(m_Water.seaPropagationTexture);
		cmd.ClearRenderTarget(clearDepth: false, clearColor: true, UnityEngine.Color.black);
	}

	public float GetTimeStep()
	{
		if (IsNewMap)
		{
			return 1f;
		}
		if (m_SimulationSystem.selectedSpeed == 0f)
		{
			return 0f;
		}
		float num = Math.Min(UnityEngine.Time.smoothDeltaTime * 30f, 1f);
		float num2 = m_SimulationSystem.selectedSpeed * 0.25f;
		if (TimeStepOverride > 0f)
		{
			return TimeStepOverride;
		}
		float end = math.min(1f, num2 * num);
		m_lastFrameTimeStep = math.lerp(m_lastFrameTimeStep, end, UnityEngine.Time.smoothDeltaTime * 0.2f);
		return m_lastFrameTimeStep;
	}

	private void CheckReadbacks()
	{
		m_PreviousReadyFrame = m_LastReadyFrame;
		m_LastReadyFrame = (uint)(m_NextSimulationFrame / (ulong)MaxSpeed);
		if (m_NextSimulationFrame >= (ulong)((long)m_LastReadbackRequest + (long)ReadbackRequestInterval) && m_waterSimActiveTilesHelper.RequestReadbackIfNotPending())
		{
			m_waterBackdropSimActiveTilesHelper.RequestReadbackIfNotPending();
			m_LastReadbackRequest = m_NextSimulationFrame;
		}
		if (m_NextSimulationFrame >= (ulong)((long)m_LastDepthReadbackRequest + (long)DepthReadbackRequestInterval))
		{
			m_LastDepthReadbackRequest = m_NextSimulationFrame;
			m_depthsReader.ExecuteReadBack();
			m_velocitiesReader.ExecuteReadBack();
			m_maxHeightReader.ExecuteReadBack();
			if (m_SimulateBackdrop)
			{
				m_depthsBackdropReader.ExecuteReadBack();
			}
		}
	}

	private void UpdateSaveReadback()
	{
		if (m_SaveAsyncGPUReadback.isPending && !m_SaveAsyncGPUReadback.hasError)
		{
			if (m_SaveAsyncGPUReadback.done)
			{
				JobSaveToFile(m_SaveAsyncGPUReadback.GetData<float4>());
			}
			m_SaveAsyncGPUReadback.IncrementFrame();
		}
	}

	private void Simulate(CommandBuffer cmd)
	{
		m_terrainReady--;
		if (!__query_1000286787_0.HasSingleton<TerrainPropertiesData>() || m_terrainReady > 0)
		{
			return;
		}
		cmd.name = "WaterSimulation";
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.SimulateWater)))
		{
			if (m_terrainChangeCounter > 0)
			{
				if (IsNewMap)
				{
					m_terrainChangeCounter = 0;
					WaterSimSpeed = 1;
					if (!m_UseLegacyWaterSources)
					{
						m_waterSim.EvaluateSeaPropagation(cmd);
					}
				}
				else
				{
					m_terrainChangeCounter--;
					m_waterSim.RestoreHeightFromHeightmap(m_restoreHeightMinWaterHeight);
					if (!m_UseLegacyWaterSources)
					{
						ResetSeaPropgagtion(cmd);
						m_waterSim.EvaluateSeaPropagation(cmd);
						m_waterSim.MaxHeightStep(cmd);
						if (m_terrainChangeAreaSet && MathUtils.Area(m_terrainChangeArea) > 0f)
						{
							m_maxHeightReader.SetReadbackArea(m_terrainChangeArea);
							m_maxHeightReader.ExecuteReadBack();
						}
					}
					if (m_terrainChangeCounter == 0)
					{
						WaterSimSpeed = 1;
						m_terrainChangeAreaSet = false;
						if (!UseLegacyWaterSources && GameManager.instance.gameMode.IsEditor())
						{
							UpdateWaterSourcesHeights();
						}
					}
					m_restoreHeightMinWaterHeight = kDefaultMinWaterToRestoreHeight;
					if (WaterSimSpeed == 0)
					{
						m_waterSim.CopyToHeightmapStep(cmd);
					}
				}
			}
			if (!m_UseLegacyWaterSources && m_seaLevelChanged)
			{
				m_waterSim.RemoveWaterInSea(cmd);
				ResetSeaPropgagtion(cmd);
				m_waterSim.EvaluateSeaPropagation(cmd);
				m_waterSim.RestoreWaterInSea(cmd);
			}
			if (WaterSimSpeed > 0)
			{
				for (int i = 0; i < WaterSimSpeed; i++)
				{
					m_ActiveReaders.Complete();
					WaterSimulation.EvaporateStep(cmd);
					WaterSimulation.SourceStep(cmd, LastFrameSourceCache);
					WaterSimulation.VelocityStep(cmd);
					WaterSimulation.DepthStep(cmd);
					CheckReadbacks();
					if (m_SimulateBackdrop && !m_UseLegacyWaterSources)
					{
						m_waterSim.SourceStepBackdrop(cmd, LastFrameSourceCache);
						m_waterSim.VelocityStepBackdrop(cmd);
						m_waterSim.DepthStepBackdrop(cmd);
					}
					m_NextSimulationFrame += (uint)(4 * MaxSpeed / WaterSimSpeed);
					if (m_SimulationSystem.selectedSpeed == 0f)
					{
						break;
					}
				}
				m_waterSim.CopyToHeightmapStep(cmd);
				if (IsNewMap)
				{
					m_NewMap--;
					if (!m_UseLegacyWaterSources)
					{
						m_waterSim.MaxHeightStep(cmd);
						m_maxHeightReader.FullReadback = true;
						m_maxHeightReader.ExecuteReadBack();
					}
				}
			}
			else
			{
				m_NextSimulationFrame += (uint)MaxSpeed;
				m_PreviousReadyFrame = (uint)(m_NextSimulationFrame / (ulong)MaxSpeed);
				m_LastReadyFrame = (uint)(m_NextSimulationFrame / (ulong)MaxSpeed);
			}
		}
		LastFrameSourceCache.Clear();
		m_waterSimActiveTilesHelper.UpdateGPUReadback();
		m_waterBackdropSimActiveTilesHelper.UpdateGPUReadback();
		ForceUpdateFlow = false;
		if (m_seaLevelChanged)
		{
			m_seaLevelChanged = false;
		}
	}

	public void OnSimulateGPU(CommandBuffer cmd)
	{
		if (Loaded && m_TexSize.x > 0)
		{
			Simulate(cmd);
		}
	}

	public void Save()
	{
		WaterSimSpeed = 0;
		m_SaveAsyncGPUReadback.Request(WaterTexture, 0, GraphicsFormat.R32G32B32A32_SFloat);
	}

	private WaterSimulationLegacy.SourceJobLegacy GetSourceJobLegacy(out JobHandle sourceHandle, out JobHandle eventHandle)
	{
		return new WaterSimulationLegacy.SourceJobLegacy
		{
			m_SourceChunks = m_SourceGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out sourceHandle),
			m_EventChunks = m_WaterLevelChangeGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out eventHandle),
			m_ChangeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_WaterLevelChange_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ChangePrefabDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainOffset = m_TerrainSystem.positionOffset,
			m_Cache = CurrentJobSourceCache
		};
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_SourceHandle.Complete();
		m_SourceCacheIndex = 1 - m_SourceCacheIndex;
		CurrentJobSourceCache.Clear();
		Shader.SetGlobalVector("colossal_WaterParams", new Vector4(m_SeaLevel, m_SimulateBackdrop ? 1f : 0f, 0f, 0f));
		JobHandle sourceHandle;
		JobHandle eventHandle;
		if (m_UseLegacyWaterSources)
		{
			WaterSimulationLegacy.SourceJobLegacy sourceJobLegacy = GetSourceJobLegacy(out sourceHandle, out eventHandle);
			m_SourceHandle = IJobExtensions.Schedule(sourceJobLegacy, JobUtils.CombineDependencies(base.Dependency, m_SourceHandle, sourceHandle, eventHandle));
		}
		else
		{
			WaterSimulation.SourceJob sourceJob = GetSourceJob(out sourceHandle, out eventHandle);
			m_SourceHandle = IJobExtensions.Schedule(sourceJob, JobUtils.CombineDependencies(base.Dependency, m_SourceHandle, sourceHandle, eventHandle));
		}
		base.Dependency = m_SourceHandle;
		UpdateSaveReadback();
		m_ActiveReaders.Complete();
	}

	public void Restart()
	{
		m_waterSim.Restart();
	}

	public void PostDeserialize(Context context)
	{
		m_TerrainSystem.DownSampleHeightMap();
		Simulate(m_CommandBuffer);
		Graphics.ExecuteCommandBuffer(m_CommandBuffer);
		m_CommandBuffer.Clear();
		m_Loaded = true;
		if (!context.format.Has(FormatTags.WaterSourceYPositionFix))
		{
			UpdateWaterSourcesY();
		}
	}

	public void JobLoad()
	{
		throw new NotImplementedException();
	}

	public unsafe byte[] CreateByteArray<T>(NativeArray<T> src) where T : struct
	{
		int num = UnsafeUtility.SizeOf<T>() * src.Length;
		byte* unsafeReadOnlyPtr = (byte*)src.GetUnsafeReadOnlyPtr();
		byte[] array = new byte[num];
		fixed (byte* ptr = array)
		{
			UnsafeUtility.MemCpy(ptr, unsafeReadOnlyPtr, num);
		}
		return array;
	}

	public void OnTerrainCascadesUpdated()
	{
		if (m_terrainReady > 30)
		{
			m_terrainReady = 29;
		}
	}

	private void JobSaveToFile(NativeArray<float4> buffer)
	{
		throw new NotImplementedException();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
	}

	internal int2 ActiveGridSize()
	{
		return m_waterSimActiveTilesHelper.GridSize;
	}

	internal int2 ActiveBackdropGridSize()
	{
		return m_waterBackdropSimActiveTilesHelper.GridSize;
	}

	private WaterSimulation.SourceJob GetSourceJob(out JobHandle sourceHandle, out JobHandle eventHandle)
	{
		return new WaterSimulation.SourceJob
		{
			m_SourceChunks = m_SourceGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out sourceHandle),
			m_EventChunks = m_WaterLevelChangeGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out eventHandle),
			m_ChangeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_WaterLevelChange_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ChangePrefabDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainOffset = m_TerrainSystem.positionOffset,
			m_Cache = CurrentJobSourceCache
		};
	}

	internal void UpdateWaterArea(Rect viewportRect)
	{
		if (!UseLegacyWaterSources)
		{
			m_waterSim.UpdateWaterArea(viewportRect);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<TerrainPropertiesData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1000286787_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public WaterSystem()
	{
	}

	bool IGPUSystem.get_Enabled()
	{
		return base.Enabled;
	}
}
