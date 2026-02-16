using System;
using Colossal.AssetPipeline.Native;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Game.Simulation;

public class WaterSimulation : IWaterSimulation
{
	[BurstCompile]
	internal struct SourceJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_SourceChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EventChunks;

		[ReadOnly]
		public ComponentTypeHandle<WaterLevelChange> m_ChangeType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<WaterSourceData> m_SourceType;

		[ReadOnly]
		public ComponentLookup<WaterLevelChangeData> m_ChangePrefabDatas;

		public NativeList<WaterSourceCache> m_Cache;

		public float3 m_TerrainOffset;

		private void HandleSource(WaterSourceData source, Game.Objects.Transform transform)
		{
			float3 position = transform.m_Position - m_TerrainOffset;
			WaterSourceCache value = new WaterSourceCache
			{
				m_ConstantDepth = source.m_ConstantDepth,
				m_Polluted = source.m_Polluted,
				m_Radius = source.m_Radius * source.m_Modifier,
				m_Position = position,
				m_Height = source.m_Height
			};
			m_Cache.Add(in value);
		}

		public void Execute()
		{
			m_Cache.Clear();
			for (int i = 0; i < m_SourceChunks.Length; i++)
			{
				NativeArray<WaterSourceData> nativeArray = m_SourceChunks[i].GetNativeArray(ref m_SourceType);
				NativeArray<Game.Objects.Transform> nativeArray2 = m_SourceChunks[i].GetNativeArray(ref m_TransformType);
				if (nativeArray2.Length > 0)
				{
					for (int j = 0; j < nativeArray.Length; j++)
					{
						HandleSource(nativeArray[j], nativeArray2[j]);
					}
				}
			}
		}
	}

	public float m_TimeStep = 0.03f;

	private ComputeShader m_UpdateShader;

	private int m_VelocityKernel;

	private int m_DownsampleKernel;

	private int m_VerticalBlurKernel;

	private int m_HorizontalBlurKernel;

	private int m_HorizontalBlurAreaKernel;

	private int m_VerticalBlurAreaKernel;

	private int m_VerticalBlurKernel2;

	private int m_HorizontalBlurKernel2;

	private int m_BlurSeaPropagationKernel;

	private int m_FlowPostProcessKernel;

	private int m_FlowPostProcessKernel2;

	private int m_DepthKernel;

	private int m_DepthBackdropKernel;

	private int m_CopyToHeightmapKernel;

	private int m_RestoreHeightFromHeightmapKernel;

	private int m_AddKernel;

	private int m_AddConstantKernel;

	private int m_AddConstantBackdropKernel;

	private int m_VelocityBackdropKernel;

	private int m_EvaporateKernel;

	private int m_ResetKernel;

	private int m_ResetActiveKernel;

	private int m_ResetToLevelKernel;

	private int m_UpdateToSeaLevelKernel;

	private int m_LoadKernel;

	private int m_EvaluateSeaPropagationKernel;

	private int m_RemoveWaterInSeaKernel;

	private int m_RestoreWaterInSeaKernel;

	private int m_MaxHeightKernel;

	private int m_ID_AddPosition;

	private int m_ID_AddRadius;

	private int m_ID_AddAmount;

	private int m_ID_AddPolluted;

	private int m_ID_CellsPerArea;

	private int m_ID_AreaCountX;

	private int m_ID_AreaCountY;

	private int m_ID_Evaporation;

	private int m_ID_CurrentActiveIndices;

	private int m_ID_RainConstant;

	private int m_ID_TerrainScale;

	private int m_ID_Timestep;

	private int m_ID_Fluidness;

	private int m_ID_Damping;

	private int m_ID_FlowInterpolationFatcor;

	private int m_ID_CellSize;

	private int m_ID_SeaLevel;

	private int m_ID_SeaPropagationTexture;

	private int m_ID_WindVelocityScale;

	private int m_ID_WaterSourceSpeed;

	private int m_ID_PollutionDecayRate;

	private int m_ID_SeaFlowParams;

	private int m_ID_MaxHeightDownscaled;

	private int m_ID_DownScaledResults;

	private int m_ID_Previous;

	private int m_ID_Result;

	private int m_ID_Water;

	private int m_ID_Previous2;

	private int m_ID_Result2;

	private int m_ID_Terrain;

	private int m_ID_TerrainObjectsLayers;

	private int m_ID_TerrainDownScaled;

	private int m_ID_TerrainLod;

	private int m_ID_MaxVelocity;

	private int m_ID_FlowDownscaled;

	private int m_ID_RestoreHeightMinWaterHeight;

	private int m_ID_Active;

	private CommandBuffer m_CommandBuffer;

	private WaterSystem m_WaterSystem;

	private TerrainSystem m_TerrainSystem;

	private const int TEMP_BLUR_TEXTURE_SIZE = 256;

	private RenderTexture m_blurTempTexture;

	private bool m_needAreaUpdate;

	private Rect m_areaUpdateViewport;

	public float MaxVelocity { get; set; } = 12f;

	public float Damping { get; set; } = 0.995f;

	public float Evaporation { get; set; } = 0.001f;

	public float RainConstant { get; set; } = 5E-05f;

	public float PollutionDecayRate { get; set; } = 0.001f;

	public float Fluidness { get; set; } = 0.15f;

	public float WindVelocityScale { get; set; } = 0.08f;

	public float WaterSourceSpeed { get; set; } = 2f;

	private ActiveWaterTilesHelper ActiveTilesHelper => m_WaterSystem.WaterSimActiveTilesHelper;

	private ActiveWaterTilesHelper BackdropActiveTilesHelper => m_WaterSystem.WaterBackdropSimActiveTilesHelper;

	public WaterSimulation(WaterSystem waterSystem, TerrainSystem terrainSystem)
	{
		m_WaterSystem = waterSystem;
		m_TerrainSystem = terrainSystem;
		m_CommandBuffer = new CommandBuffer();
	}

	public void OnDestroy()
	{
		if (m_blurTempTexture.IsCreated())
		{
			CoreUtils.Destroy(m_blurTempTexture);
		}
	}

	public void InitShader()
	{
		m_UpdateShader = AssetDatabase.global.resources.shaders.waterUpdate;
		m_VelocityKernel = m_UpdateShader.FindKernel("VelocityUpdate");
		m_DownsampleKernel = m_UpdateShader.FindKernel("CSDownsample");
		m_VerticalBlurKernel = m_UpdateShader.FindKernel("CSVerticalBlur");
		m_HorizontalBlurKernel = m_UpdateShader.FindKernel("CSHorizontalBlur");
		m_HorizontalBlurAreaKernel = m_UpdateShader.FindKernel("CSHorizontalBlurArea");
		m_VerticalBlurAreaKernel = m_UpdateShader.FindKernel("CSVerticalBlurArea");
		m_VerticalBlurKernel2 = m_UpdateShader.FindKernel("CSVerticalBlur2");
		m_HorizontalBlurKernel2 = m_UpdateShader.FindKernel("CSHorizontalBlur2");
		m_FlowPostProcessKernel = m_UpdateShader.FindKernel("CSFlowPostProcess");
		m_FlowPostProcessKernel2 = m_UpdateShader.FindKernel("CSFlowPostProcess2");
		m_BlurSeaPropagationKernel = m_UpdateShader.FindKernel("CSBlurSeaPropagation");
		m_DepthKernel = m_UpdateShader.FindKernel("DepthUpdate");
		m_CopyToHeightmapKernel = m_UpdateShader.FindKernel("CopyToHeightmap");
		m_RestoreHeightFromHeightmapKernel = m_UpdateShader.FindKernel("RestoreHeightFromHeightmap");
		m_AddConstantKernel = m_UpdateShader.FindKernel("AddConstant");
		m_AddConstantBackdropKernel = m_UpdateShader.FindKernel("AddConstantBackdrop");
		m_VelocityBackdropKernel = m_UpdateShader.FindKernel("VelocityBackdrop");
		m_DepthBackdropKernel = m_UpdateShader.FindKernel("DepthBackdrop");
		m_AddKernel = m_UpdateShader.FindKernel("Add");
		m_EvaporateKernel = m_UpdateShader.FindKernel("Evaporate");
		m_ResetKernel = m_UpdateShader.FindKernel("Reset");
		m_ResetActiveKernel = m_UpdateShader.FindKernel("ResetActive");
		m_ResetToLevelKernel = m_UpdateShader.FindKernel("ResetToLevel");
		m_UpdateToSeaLevelKernel = m_UpdateShader.FindKernel("UpdateToSeaLevel");
		m_LoadKernel = m_UpdateShader.FindKernel("Load");
		m_EvaluateSeaPropagationKernel = m_UpdateShader.FindKernel("EvaluateSeaPropagation");
		m_RemoveWaterInSeaKernel = m_UpdateShader.FindKernel("RemoveSeaWater");
		m_RestoreWaterInSeaKernel = m_UpdateShader.FindKernel("RestoreSeaWater");
		m_MaxHeightKernel = m_UpdateShader.FindKernel("MaxHeightUpdate");
		m_ID_AddAmount = Shader.PropertyToID("addAmount");
		m_ID_AddPolluted = Shader.PropertyToID("addPolluted");
		m_ID_AddPosition = Shader.PropertyToID("addPosition");
		m_ID_AddRadius = Shader.PropertyToID("addRadius");
		m_ID_CellsPerArea = Shader.PropertyToID("cellsPerArea");
		m_ID_AreaCountX = Shader.PropertyToID("areaCountX");
		m_ID_AreaCountY = Shader.PropertyToID("areaCountY");
		m_ID_Evaporation = Shader.PropertyToID("evaporation");
		m_ID_CurrentActiveIndices = Shader.PropertyToID("_CurrentActiveIndices");
		m_ID_RainConstant = Shader.PropertyToID("rainConstant");
		m_ID_TerrainScale = Shader.PropertyToID("terrainScale");
		m_ID_Timestep = Shader.PropertyToID("timestep");
		m_ID_Fluidness = Shader.PropertyToID("fluidness");
		m_ID_Damping = Shader.PropertyToID("damping");
		m_ID_CellSize = Shader.PropertyToID("cellSize");
		m_ID_SeaLevel = Shader.PropertyToID("seaLevel");
		m_ID_FlowInterpolationFatcor = Shader.PropertyToID("flowInterpolationFatcor");
		m_ID_PollutionDecayRate = Shader.PropertyToID("pollutionDecayRate");
		m_ID_WaterSourceSpeed = Shader.PropertyToID("waterSourceSpeed");
		m_ID_WindVelocityScale = Shader.PropertyToID("windVelocityScale");
		m_ID_SeaPropagationTexture = Shader.PropertyToID("_SeaPropagationTexture");
		m_ID_RestoreHeightMinWaterHeight = Shader.PropertyToID("restoreHeightMinWaterHeight");
		m_ID_SeaFlowParams = Shader.PropertyToID("seaFlowParams");
		m_ID_Previous = Shader.PropertyToID("_Previous");
		m_ID_Result = Shader.PropertyToID("_Result");
		m_ID_Water = Shader.PropertyToID("_Water");
		m_ID_Previous2 = Shader.PropertyToID("_Previous2Channels");
		m_ID_Result2 = Shader.PropertyToID("_Result2Channels");
		m_ID_Terrain = Shader.PropertyToID("_Terrain");
		m_ID_TerrainObjectsLayers = Shader.PropertyToID("_TerrainObjectsLayer");
		m_ID_TerrainDownScaled = Shader.PropertyToID("_TerrainDownScaled");
		m_ID_TerrainLod = Shader.PropertyToID("_TerrainLod");
		m_ID_Active = Shader.PropertyToID("_Active");
		m_ID_MaxVelocity = Shader.PropertyToID("maxVelo");
		m_ID_FlowDownscaled = Shader.PropertyToID("_FlowDownscaled");
		m_ID_MaxHeightDownscaled = Shader.PropertyToID("_MaxHeightDownscaled");
		m_ID_DownScaledResults = Shader.PropertyToID("_DownscaledResult");
		Shader.SetGlobalTexture("colossal_WaterTexture", Texture2D.whiteTexture);
		Shader.SetGlobalVector("colossal_WaterTexture_TexelSize", Vector4.one);
		m_blurTempTexture = CreateRenderTexture("TempBlurTexture", new int2(256, 256), GraphicsFormat.R32G32B32A32_SFloat);
	}

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

	public void Reset()
	{
		m_UpdateShader.SetTexture(m_ResetKernel, m_ID_Result, m_WaterSystem.WaterTexture);
		m_UpdateShader.Dispatch(m_ResetKernel, m_WaterSystem.TextureSize.x / 16, m_WaterSystem.TextureSize.y / 16, 1);
	}

	public void UpdateToSeaLevel(float level)
	{
		m_CommandBuffer.Clear();
		using (new ProfilingScope(m_CommandBuffer, ProfilingSampler.Get(ProfileId.WaterResetToLevel)))
		{
			int2 @int = m_WaterSystem.TextureSize / 16;
			m_CommandBuffer.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, level);
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_UpdateToSeaLevelKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_UpdateToSeaLevelKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_UpdateToSeaLevelKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			m_CommandBuffer.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.positionOffset.xy, 0f));
			m_CommandBuffer.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_UpdateToSeaLevelKernel, m_ID_Result, m_WaterSystem.WaterTexture);
			m_CommandBuffer.DispatchCompute(m_UpdateShader, m_UpdateToSeaLevelKernel, @int.x, @int.y, 1);
		}
		Graphics.ExecuteCommandBuffer(m_CommandBuffer);
	}

	public void RemoveWaterInSea(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.RemoveSeaWater)))
		{
			int2 @int = m_WaterSystem.TextureSize / 16;
			cmd.SetComputeTextureParam(m_UpdateShader, m_RemoveWaterInSeaKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_RemoveWaterInSeaKernel, m_ID_Result, m_WaterSystem.WaterTexture);
			cmd.DispatchCompute(m_UpdateShader, m_RemoveWaterInSeaKernel, @int.x, @int.y, 1);
		}
	}

	public void RestoreWaterInSea(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.RestoreSeaWater)))
		{
			int2 @int = m_WaterSystem.TextureSize / 16;
			cmd.SetComputeTextureParam(m_UpdateShader, m_RestoreWaterInSeaKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_RestoreWaterInSeaKernel, m_ID_Result, m_WaterSystem.WaterTexture);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_SeaLevel, m_WaterSystem.SeaLevel);
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.positionOffset.xy, 0f));
			cmd.SetComputeTextureParam(m_UpdateShader, m_RestoreWaterInSeaKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.DispatchCompute(m_UpdateShader, m_RestoreWaterInSeaKernel, @int.x, @int.y, 1);
		}
	}

	public void EvaluateSeaPropagation(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.EvaluateSeaPropagation)))
		{
			int2 @int = m_WaterSystem.TextureSize / 16;
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaluateSeaPropagationKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaluateSeaPropagationKernel, m_ID_Previous, m_WaterSystem.WaterTexture);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_SeaLevel, m_WaterSystem.SeaLevel);
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.positionOffset.xy, 0f));
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaluateSeaPropagationKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaluateSeaPropagationKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			for (int i = 0; i < 12; i++)
			{
				cmd.DispatchCompute(m_UpdateShader, m_EvaluateSeaPropagationKernel, @int.x, @int.y, 1);
			}
		}
	}

	public void ResetToLevel(float level, RenderTexture target, bool isBackdrop, bool checkSeaPropataion = false)
	{
		m_CommandBuffer.Clear();
		using (new ProfilingScope(m_CommandBuffer, ProfilingSampler.Get(ProfileId.WaterResetToLevel)))
		{
			int num = m_WaterSystem.TextureSize.x / target.width;
			int2 @int = m_WaterSystem.TextureSize / 16 / num;
			m_CommandBuffer.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, level);
			m_CommandBuffer.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize / (float)num);
			m_CommandBuffer.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_WaterSystem.GridSize / num);
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_ResetToLevelKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_ResetToLevelKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_ResetToLevelKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			m_CommandBuffer.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.positionOffset.xy, 0f));
			m_CommandBuffer.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, (!isBackdrop) ? TerrainSystem.baseLod : 0);
			m_CommandBuffer.SetComputeIntParam(m_UpdateShader, "checkSeaPropagagtion", checkSeaPropataion ? 1 : 0);
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_ResetToLevelKernel, m_ID_Result, target);
			m_CommandBuffer.DispatchCompute(m_UpdateShader, m_ResetToLevelKernel, @int.x, @int.y, 1);
		}
		Graphics.ExecuteCommandBuffer(m_CommandBuffer);
	}

	public void ResetBackdropWaterToSeaLevel()
	{
		ResetToLevel(m_WaterSystem.SeaLevel, m_WaterSystem.WaterBackdropTexture, isBackdrop: true);
		ResetToLevel(m_WaterSystem.SeaLevel, m_WaterSystem.WaterBackdropRenderTexture, isBackdrop: true);
	}

	internal unsafe void LoadWaterData<TReader>(TReader reader, int width, int height, RenderTexture target, SurfaceDataReader surfaceReader) where TReader : IReader
	{
		int num = width * height;
		NativeArray<float4> nativeArray = new NativeArray<float4>(num, Allocator.Temp);
		bool num2 = width == target.width && height == target.height;
		if (reader.context.version >= Version.terrainWaterSnowCompression)
		{
			NativeArray<byte> nativeArray2 = new NativeArray<byte>(num * sizeof(float4), Allocator.Temp);
			NativeArray<byte> value = nativeArray2;
			reader.Read(value);
			NativeCompression.UnfilterDataAfterRead((IntPtr)nativeArray2.GetUnsafePtr(), (IntPtr)nativeArray.GetUnsafePtr(), nativeArray2.Length, sizeof(float4));
			nativeArray2.Dispose();
		}
		else
		{
			NativeArray<float4> value2 = nativeArray;
			reader.Read(value2);
		}
		if (num2)
		{
			surfaceReader?.LoadData(nativeArray);
		}
		ComputeBuffer computeBuffer = new ComputeBuffer(num, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Default);
		computeBuffer.SetData(nativeArray);
		m_CommandBuffer.SetComputeBufferParam(m_UpdateShader, m_LoadKernel, "_LoadSource", computeBuffer);
		m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_LoadKernel, m_ID_Result, target);
		m_CommandBuffer.DispatchCompute(m_UpdateShader, m_LoadKernel, target.width / 16, target.height / 16, 1);
		Graphics.ExecuteCommandBuffer(m_CommandBuffer);
		computeBuffer.Dispose();
		nativeArray.Dispose();
		m_CommandBuffer.Clear();
	}

	public void SourceStep(CommandBuffer cmd, NativeList<WaterSourceCache> LastFrameSourceCache)
	{
		if (m_WaterSystem.IsNewMap)
		{
			return;
		}
		if (m_needAreaUpdate)
		{
			UpdateAreaInternal(cmd);
		}
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		Bounds3 bounds = TerrainUtils.GetBounds(ref data);
		int num = 0;
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.SourceStep)))
		{
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.positionOffset.y, 0f, 0f));
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_WaterSystem.GetTimeStep());
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_WaterSourceSpeed, WaterSourceSpeed);
			foreach (WaterSourceCache item in LastFrameSourceCache)
			{
				float3 worldPosition = new float3(item.m_Position.x, 0f, item.m_Position.z) + m_TerrainSystem.positionOffset;
				if (!MathUtils.Intersect(bounds.xz, worldPosition.xz) || item.m_Radius == 0f)
				{
					continue;
				}
				cmd.SetComputeVectorParam(val: new float4(new float3(item.m_Position.x, item.m_Height, item.m_Position.z), 0f), computeShader: m_UpdateShader, nameID: m_ID_AddPosition);
				int num2 = Mathf.CeilToInt(item.m_Radius / WaterSystem.kCellSize);
				num2 = 2 * num2 + 1;
				cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddRadius, item.m_Radius);
				if (item.m_Polluted > 0f)
				{
					cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddAmount, 0.3165952f * (float)m_WaterSystem.SimulationCycleSteps * item.m_Height);
					cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddPolluted, item.m_Polluted);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddKernel, m_ID_Result, m_WaterSystem.WaterTexture);
					cmd.SetComputeBufferParam(m_UpdateShader, m_AddKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
					cmd.DispatchCompute(m_UpdateShader, m_AddKernel, num2, num2, 1);
				}
				else
				{
					float num3 = TerrainUtils.SampleHeight(ref data, worldPosition);
					num3 += item.m_Height;
					if (item.m_Height < 0f)
					{
						float t = item.m_Height / -150000f;
						num3 = math.lerp(0f, -1f, t);
					}
					else
					{
						num3 = item.m_Position.y + item.m_Height;
					}
					cmd.SetComputeVectorParam(val: new float4(new float3(item.m_Position.x, num3, item.m_Position.z), 0f), computeShader: m_UpdateShader, nameID: m_ID_AddPosition);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantKernel, m_ID_Result, m_WaterSystem.WaterTexture);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
					cmd.SetComputeBufferParam(m_UpdateShader, m_AddConstantKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
					cmd.DispatchCompute(m_UpdateShader, m_AddConstantKernel, num2, num2, 1);
				}
				num++;
			}
		}
	}

	public void SourceStepBackdrop(CommandBuffer cmd, NativeList<WaterSourceCache> LastFrameSourceCache)
	{
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		TerrainUtils.GetBounds(ref data);
		int num = 0;
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.SourceStep)))
		{
			int val = m_WaterSystem.GridSize / 2;
			float backdropCellSize = m_WaterSystem.BackdropCellSize;
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, backdropCellSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, val);
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.positionOffset.y, 0f, 0f));
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_WaterSystem.GetTimeStep());
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_WaterSourceSpeed, WaterSourceSpeed);
			if (m_WaterSystem.IsNewMap)
			{
				return;
			}
			foreach (WaterSourceCache item in LastFrameSourceCache)
			{
				float3 @float = new float3(item.m_Position.x, item.m_Height, item.m_Position.z) + m_TerrainSystem.positionOffset - m_TerrainSystem.positionOffset * 4f;
				int num2 = Mathf.CeilToInt(item.m_Radius / backdropCellSize);
				num2 = 2 * num2 + 1;
				cmd.SetComputeFloatParam(m_UpdateShader, m_ID_AddRadius, item.m_Radius);
				if (!(item.m_Polluted > 0f))
				{
					float y = item.m_Position.y + item.m_Height;
					cmd.SetComputeVectorParam(val: new float4(new float3(@float.x, y, @float.z), 0f), computeShader: m_UpdateShader, nameID: m_ID_AddPosition);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantBackdropKernel, m_ID_Result, m_WaterSystem.WaterBackdropTexture);
					cmd.SetComputeTextureParam(m_UpdateShader, m_AddConstantBackdropKernel, m_ID_TerrainDownScaled, m_TerrainSystem.downscaledHeightmap);
					cmd.SetComputeBufferParam(m_UpdateShader, m_AddConstantBackdropKernel, m_ID_Active, BackdropActiveTilesHelper.GetActiveBuffer());
					cmd.DispatchCompute(m_UpdateShader, m_AddConstantBackdropKernel, num2, num2, 1);
					num++;
				}
			}
		}
	}

	public void EvaporateStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.EvaporateStep)))
		{
			bool num = m_WaterSystem.HasActiveGridSizeChanged(cmd);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_SeaLevel, m_WaterSystem.SeaLevel);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountX, m_WaterSystem.TextureSize.x / m_WaterSystem.GridSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountY, m_WaterSystem.TextureSize.y / m_WaterSystem.GridSize);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_MaxVelocity, MaxVelocity);
			cmd.SetComputeBufferParam(m_UpdateShader, m_EvaporateKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaporateKernel, m_ID_Result, m_WaterSystem.WaterTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaporateKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_WaterSystem.GetTimeStep());
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_WaterSystem.GridSize);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Evaporation, Evaporation);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_RainConstant, RainConstant);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_PollutionDecayRate, PollutionDecayRate);
			cmd.SetComputeTextureParam(m_UpdateShader, m_EvaporateKernel, m_ID_FlowDownscaled, m_WaterSystem.FlowDownScaled(0));
			bool flag = num | m_WaterSystem.IsNewMap;
			if (flag)
			{
				cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_TimeStep);
			}
			ActiveTilesHelper.UpdateActiveIndices(flag || !m_WaterSystem.UseActiveCellsCulling);
			cmd.SetComputeBufferParam(m_UpdateShader, m_EvaporateKernel, m_ID_CurrentActiveIndices, ActiveTilesHelper.GetActiveTilesIndices());
			if (ActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_EvaporateKernel, ActiveTilesHelper.numThreadGroupsX, ActiveTilesHelper.numThreadGroupsY, ActiveTilesHelper.numThreadGroupsY);
			}
		}
	}

	public void DepthStepBackdrop(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.DepthStep)))
		{
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthBackdropKernel, m_ID_Previous, m_WaterSystem.WaterBackdropRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthBackdropKernel, m_ID_Result, m_WaterSystem.WaterBackdropTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthBackdropKernel, m_ID_TerrainDownScaled, m_TerrainSystem.downscaledHeightmap);
			cmd.SetComputeBufferParam(m_UpdateShader, m_DepthBackdropKernel, m_ID_CurrentActiveIndices, BackdropActiveTilesHelper.GetActiveTilesIndices());
			cmd.SetComputeBufferParam(m_UpdateShader, m_DepthBackdropKernel, m_ID_Active, BackdropActiveTilesHelper.GetActiveBuffer());
			if (BackdropActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_DepthBackdropKernel, BackdropActiveTilesHelper.numThreadGroupsX, BackdropActiveTilesHelper.numThreadGroupsY, BackdropActiveTilesHelper.numThreadGroupsY);
			}
		}
	}

	public void VelocityStepBackdrop(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.VelocityStep)))
		{
			bool num = m_WaterSystem.HasActiveGridSizeChanged(cmd);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityBackdropKernel, m_ID_Result, m_WaterSystem.WaterBackdropRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityBackdropKernel, m_ID_TerrainDownScaled, m_TerrainSystem.downscaledHeightmap);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityBackdropKernel, m_ID_Previous, m_WaterSystem.WaterBackdropTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityBackdropKernel, "_DownscaledResultBackdrop", m_WaterSystem.BackDropFlowDownScaled(0));
			cmd.SetComputeBufferParam(m_UpdateShader, m_VelocityBackdropKernel, m_ID_Active, BackdropActiveTilesHelper.GetActiveBuffer());
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountX, BackdropActiveTilesHelper.GridSize.x);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountY, BackdropActiveTilesHelper.GridSize.y);
			cmd.SetComputeFloatParam(m_UpdateShader, "postFlowspeedMultiplier", m_WaterSystem.PostFlowspeedMultiplier);
			bool flag = num | m_WaterSystem.IsNewMap;
			if (flag)
			{
				cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Timestep, m_TimeStep);
			}
			BackdropActiveTilesHelper.UpdateActiveIndices(flag || !m_WaterSystem.UseActiveCellsCulling);
			cmd.SetComputeBufferParam(m_UpdateShader, m_VelocityBackdropKernel, m_ID_CurrentActiveIndices, BackdropActiveTilesHelper.GetActiveTilesIndices());
			if (BackdropActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_VelocityBackdropKernel, BackdropActiveTilesHelper.numThreadGroupsX, BackdropActiveTilesHelper.numThreadGroupsY, BackdropActiveTilesHelper.numThreadGroupsY);
			}
			int num2 = m_WaterSystem.FlowDownScaled(0).width / 8;
			cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurKernel2, m_ID_Previous2, m_WaterSystem.BackDropFlowDownScaled(0));
			cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurKernel2, m_ID_Result2, m_WaterSystem.BackDropFlowDownScaled(1));
			cmd.DispatchCompute(m_UpdateShader, m_VerticalBlurKernel2, num2, num2, 1);
			cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurKernel2, m_ID_Previous2, m_WaterSystem.BackDropFlowDownScaled(1));
			cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurKernel2, m_ID_Result2, m_WaterSystem.BackDropFlowDownScaled(2));
			cmd.DispatchCompute(m_UpdateShader, m_HorizontalBlurKernel2, num2, num2, 1);
			if (m_WaterSystem.FlowPostProcess)
			{
				cmd.SetComputeTextureParam(m_UpdateShader, m_FlowPostProcessKernel2, m_ID_Result2, m_WaterSystem.BackDropFlowDownScaled(2));
				cmd.SetComputeFloatParam(m_UpdateShader, "maxFlowlengthForRender", m_WaterSystem.MaxFlowlengthForRender);
				cmd.SetComputeFloatParam(m_UpdateShader, "postFlowspeedMultiplier", m_WaterSystem.PostFlowspeedMultiplier);
				cmd.DispatchCompute(m_UpdateShader, m_FlowPostProcessKernel2, num2, num2, 1);
			}
		}
	}

	public void VelocityStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.VelocityStep)))
		{
			cmd.SetComputeTextureParam(m_UpdateShader, m_ResetKernel, m_ID_Result, m_WaterSystem.WaterRenderTexture);
			cmd.DispatchCompute(m_UpdateShader, m_ResetKernel, m_WaterSystem.TextureSize.x / 16, m_WaterSystem.TextureSize.y / 16, 1);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.xy, 0f, 0f));
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_Previous, m_WaterSystem.WaterTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_Result, m_WaterSystem.WaterRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_DownScaledResults, m_WaterSystem.FlowDownScaled(0));
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_MaxHeightDownscaled, m_WaterSystem.MaxHeightDownscaled);
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_SeaFlowParams, m_WaterSystem.GetSeaFlowParams());
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_WindVelocityScale, WindVelocityScale);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VelocityKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_WaterSystem.GridSize);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Fluidness, Fluidness);
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_Damping, Damping);
			bool flag = m_WaterSystem.ForceUpdateFlow || m_WaterSystem.IsNewMap;
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_FlowInterpolationFatcor, flag ? 1f : 0.1f);
			cmd.SetComputeBufferParam(m_UpdateShader, m_VelocityKernel, m_ID_CurrentActiveIndices, ActiveTilesHelper.GetActiveTilesIndices());
			if (ActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_VelocityKernel, ActiveTilesHelper.numThreadGroupsX, ActiveTilesHelper.numThreadGroupsY, ActiveTilesHelper.numThreadGroupsY);
			}
		}
	}

	public void MaxHeightStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.MaxHeightStep)))
		{
			cmd.SetComputeTextureParam(m_UpdateShader, m_MaxHeightKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeTextureParam(m_UpdateShader, m_MaxHeightKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			cmd.SetComputeTextureParam(m_UpdateShader, m_MaxHeightKernel, m_ID_Result, m_WaterSystem.WaterTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_MaxHeightKernel, m_ID_Water, m_WaterSystem.WaterTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_MaxHeightKernel, m_ID_MaxHeightDownscaled, m_WaterSystem.MaxHeightDownscaled);
			int num = m_WaterSystem.MaxHeightDownscaled.width / 8;
			cmd.DispatchCompute(m_UpdateShader, m_MaxHeightKernel, num, num, 1);
		}
	}

	internal static float RoundDownToNearest(float number, float to)
	{
		float num = 1f / to;
		return Mathf.Floor(number * num) / num;
	}

	public void UpdateWaterArea(Rect viewportRect)
	{
		m_needAreaUpdate = true;
		m_areaUpdateViewport = viewportRect;
	}

	private void UpdateAreaInternal(CommandBuffer cmd)
	{
		m_needAreaUpdate = false;
		float2 @float = m_areaUpdateViewport.min / 2f;
		@float = new float2(RoundDownToNearest(@float.x, 8f), RoundDownToNearest(@float.y, 8f));
		int2 @int = (int2)@float;
		float2 float2 = m_areaUpdateViewport.size / 2f;
		float2 = new float2(RoundDownToNearest(float2.x, 8f) + 8f, RoundDownToNearest(float2.y, 8f) + 8f);
		if (!(float2.x > 256f) && !(float2.y > 256f))
		{
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountX, @int.x);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountY, @int.y);
			cmd.SetRenderTarget(m_blurTempTexture);
			cmd.ClearRenderTarget(clearDepth: false, clearColor: true, UnityEngine.Color.black);
			cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurAreaKernel, m_ID_Result, m_blurTempTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurAreaKernel, m_ID_Previous, m_WaterSystem.WaterRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurAreaKernel, m_ID_Water, m_WaterSystem.WaterTexture);
			int2 int2 = (int2)float2 / 8;
			cmd.DispatchCompute(m_UpdateShader, m_HorizontalBlurAreaKernel, int2.x, int2.y, 1);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurAreaKernel, m_ID_Result, m_WaterSystem.WaterRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurAreaKernel, m_ID_Previous, m_blurTempTexture);
			cmd.DispatchCompute(m_UpdateShader, m_VerticalBlurAreaKernel, int2.x, int2.y, 1);
			RestoreHeightFromHeightmap(-1000f);
		}
	}

	public void ResetActive(CommandBuffer cmd)
	{
		int2 @int = m_WaterSystem.TextureSize / m_WaterSystem.GridSize;
		cmd.SetComputeBufferParam(m_UpdateShader, m_ResetActiveKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
		cmd.DispatchCompute(m_UpdateShader, m_ResetActiveKernel, @int.x * @int.y, 1, 1);
		cmd.SetComputeBufferParam(m_UpdateShader, m_ResetActiveKernel, m_ID_Active, BackdropActiveTilesHelper.GetActiveBuffer());
		cmd.DispatchCompute(m_UpdateShader, m_ResetActiveKernel, @int.x * @int.y, 1, 1);
	}

	public void RestoreHeightFromHeightmap(float restoreHeightMinWaterHeight)
	{
		m_CommandBuffer.Clear();
		using (new ProfilingScope(m_CommandBuffer, ProfilingSampler.Get(ProfileId.CopyToHeightMap)))
		{
			m_CommandBuffer.SetComputeFloatParam(m_UpdateShader, m_ID_RestoreHeightMinWaterHeight, restoreHeightMinWaterHeight);
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_RestoreHeightFromHeightmapKernel, m_ID_Result, m_WaterSystem.WaterTexture);
			m_CommandBuffer.SetComputeBufferParam(m_UpdateShader, m_RestoreHeightFromHeightmapKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_RestoreHeightFromHeightmapKernel, m_ID_Previous, m_WaterSystem.WaterRenderTexture);
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_RestoreHeightFromHeightmapKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			m_CommandBuffer.SetComputeTextureParam(m_UpdateShader, m_RestoreHeightFromHeightmapKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			m_CommandBuffer.SetComputeBufferParam(m_UpdateShader, m_RestoreHeightFromHeightmapKernel, m_ID_CurrentActiveIndices, ActiveTilesHelper.GetActiveTilesIndices());
			m_CommandBuffer.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.xy, 0f, 0f));
			m_CommandBuffer.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			m_CommandBuffer.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_WaterSystem.GridSize);
			if (ActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				m_CommandBuffer.DispatchCompute(m_UpdateShader, m_RestoreHeightFromHeightmapKernel, ActiveTilesHelper.numThreadGroupsX, ActiveTilesHelper.numThreadGroupsY, ActiveTilesHelper.numThreadGroupsY);
			}
		}
		Graphics.ExecuteCommandBuffer(m_CommandBuffer);
	}

	public void DepthStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.DepthStep)))
		{
			cmd.SetComputeFloatParam(m_UpdateShader, m_ID_CellSize, WaterSystem.kCellSize);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_Previous, m_WaterSystem.WaterRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_Result, m_WaterSystem.WaterTexture);
			cmd.SetComputeBufferParam(m_UpdateShader, m_DepthKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_WaterSystem.GridSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountX, m_WaterSystem.TextureSize.x / m_WaterSystem.GridSize);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_AreaCountY, m_WaterSystem.TextureSize.y / m_WaterSystem.GridSize);
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeTextureParam(m_UpdateShader, m_DepthKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.x, m_TerrainSystem.heightScaleOffset.y, 0f, 0f));
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			int y = (m_WaterSystem.TextureSize / m_WaterSystem.GridSize).y;
			_ = 0;
			cmd.SetComputeBufferParam(m_UpdateShader, m_DepthKernel, m_ID_CurrentActiveIndices, ActiveTilesHelper.GetActiveTilesIndices());
			if (ActiveTilesHelper.numThreadGroupsTotal > 0)
			{
				cmd.DispatchCompute(m_UpdateShader, m_DepthKernel, ActiveTilesHelper.numThreadGroupsX, ActiveTilesHelper.numThreadGroupsY, ActiveTilesHelper.numThreadGroupsY);
			}
			if (m_WaterSystem.FlowMapNumDownscale > 0)
			{
				int2 @int = m_WaterSystem.TextureSize / 2 / 8;
				int num = m_WaterSystem.FlowMapNumDownscale - 1;
				for (int i = 0; i < num; i++)
				{
					@int /= 2;
					cmd.SetComputeTextureParam(m_UpdateShader, m_DownsampleKernel, m_ID_Previous, m_WaterSystem.FlowDownScaled(i));
					cmd.SetComputeTextureParam(m_UpdateShader, m_DownsampleKernel, m_ID_Result, m_WaterSystem.FlowDownScaled(i + 1));
					cmd.DispatchCompute(m_UpdateShader, m_DownsampleKernel, @int.x, @int.y, 1);
				}
				if (m_WaterSystem.BlurFlowMap && m_WaterSystem.FlowMapNumDownscale > 1)
				{
					cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurKernel, m_ID_Previous, m_WaterSystem.FlowDownScaled(m_WaterSystem.FlowMapNumDownscale - 1));
					cmd.SetComputeTextureParam(m_UpdateShader, m_VerticalBlurKernel, m_ID_Result, m_WaterSystem.FlowDownScaled(m_WaterSystem.FlowMapNumDownscale));
					cmd.DispatchCompute(m_UpdateShader, m_VerticalBlurKernel, @int.x, @int.y, 1);
					cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurKernel, m_ID_Previous, m_WaterSystem.FlowDownScaled(m_WaterSystem.FlowMapNumDownscale));
					cmd.SetComputeTextureParam(m_UpdateShader, m_HorizontalBlurKernel, m_ID_Result, m_WaterSystem.FlowDownScaled(m_WaterSystem.FlowMapNumDownscale - 1));
					cmd.DispatchCompute(m_UpdateShader, m_HorizontalBlurKernel, @int.x, @int.y, 1);
				}
				if (m_WaterSystem.FlowPostProcess)
				{
					cmd.SetComputeTextureParam(m_UpdateShader, m_FlowPostProcessKernel, m_ID_SeaPropagationTexture, m_WaterSystem.SeaPropagationTexture);
					cmd.SetComputeTextureParam(m_UpdateShader, m_FlowPostProcessKernel, m_ID_Result, m_WaterSystem.FlowDownScaled(m_WaterSystem.FlowMapNumDownscale - 1));
					cmd.SetComputeFloatParam(m_UpdateShader, "maxFlowlengthForRender", m_WaterSystem.MaxFlowlengthForRender);
					cmd.SetComputeFloatParam(m_UpdateShader, "postFlowspeedMultiplier", m_WaterSystem.PostFlowspeedMultiplier);
					cmd.DispatchCompute(m_UpdateShader, m_FlowPostProcessKernel, @int.x, @int.y, 1);
				}
				if (m_WaterSystem.BlurFlowMap)
				{
					int num2 = m_WaterSystem.FlowDownScaled(1).width / 8;
					cmd.SetComputeTextureParam(m_UpdateShader, m_BlurSeaPropagationKernel, m_ID_Previous, m_WaterSystem.FlowDownScaled(1));
					cmd.SetComputeTextureParam(m_UpdateShader, m_BlurSeaPropagationKernel, m_ID_Result, m_WaterSystem.SeaPropagationDownscaled);
					cmd.DispatchCompute(m_UpdateShader, m_BlurSeaPropagationKernel, num2, num2, 1);
				}
			}
		}
	}

	public void CopyToHeightmapStep(CommandBuffer cmd)
	{
		using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileId.CopyToHeightMap)))
		{
			cmd.SetComputeTextureParam(m_UpdateShader, m_CopyToHeightmapKernel, m_ID_Previous, m_WaterSystem.WaterTexture);
			cmd.SetComputeBufferParam(m_UpdateShader, m_CopyToHeightmapKernel, m_ID_Active, ActiveTilesHelper.GetActiveBuffer());
			cmd.SetComputeTextureParam(m_UpdateShader, m_CopyToHeightmapKernel, "_WaterOut", m_WaterSystem.WaterRenderTexture);
			cmd.SetComputeTextureParam(m_UpdateShader, m_CopyToHeightmapKernel, m_ID_Terrain, m_TerrainSystem.GetCascadeTexture());
			cmd.SetComputeTextureParam(m_UpdateShader, m_CopyToHeightmapKernel, m_ID_TerrainObjectsLayers, m_TerrainSystem.GetObjectsLayerTexture());
			cmd.SetComputeVectorParam(m_UpdateShader, m_ID_TerrainScale, new float4(m_TerrainSystem.heightScaleOffset.xy, 0f, 0f));
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_TerrainLod, TerrainSystem.baseLod);
			cmd.SetComputeIntParam(m_UpdateShader, m_ID_CellsPerArea, m_WaterSystem.GridSize);
			int num = m_WaterSystem.TextureSize.x / 8;
			cmd.DispatchCompute(m_UpdateShader, m_CopyToHeightmapKernel, num, num, 1);
		}
	}

	public void Restart()
	{
		int2 @int = m_WaterSystem.TextureSize / m_WaterSystem.GridSize;
		int num = @int.x * @int.y;
		NativeArray<int> nativeArray = new NativeArray<int>(num, Allocator.TempJob);
		IJobParallelForExtensions.Schedule(new MemsetNativeArray<int>
		{
			Source = nativeArray,
			Value = 0
		}, num, 64).Complete();
		ActiveTilesHelper.GetActiveBuffer().SetData(nativeArray);
		nativeArray.Dispose();
		num = m_WaterSystem.TextureSize.x * m_WaterSystem.TextureSize.y;
		NativeArray<float4> nativeArray2 = new NativeArray<float4>(num, Allocator.TempJob);
		IJobParallelForExtensions.Schedule(new MemsetNativeArray<float4>
		{
			Source = nativeArray2,
			Value = 0
		}, num, 64).Complete();
		ComputeBuffer computeBuffer = new ComputeBuffer(num, UnsafeUtility.SizeOf<float4>(), ComputeBufferType.Default);
		computeBuffer.SetData(nativeArray2);
		m_UpdateShader.SetInt(m_ID_CellsPerArea, m_WaterSystem.GridSize);
		m_UpdateShader.SetInt(m_ID_AreaCountX, m_WaterSystem.TextureSize.x / m_WaterSystem.GridSize);
		m_UpdateShader.SetBuffer(m_LoadKernel, "_LoadSource", computeBuffer);
		m_UpdateShader.SetTexture(m_LoadKernel, m_ID_Result, m_WaterSystem.WaterTexture);
		computeBuffer.Dispose();
		nativeArray2.Dispose();
	}
}
