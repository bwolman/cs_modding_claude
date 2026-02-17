using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TourismSystem : GameSystemBase
{
	[BurstCompile]
	private struct TourismJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_HotelChunks;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_m_AttractivenessProviderChunks;

		[ReadOnly]
		public ComponentTypeHandle<AttractivenessProvider> m_ProviderType;

		[ReadOnly]
		public ComponentTypeHandle<LodgingProvider> m_LodgingProviderType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public AttractivenessParameterData m_Parameters;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public ComponentLookup<Tourism> m_Tourisms;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public bool m_IsRaining;

		[ReadOnly]
		public bool m_IsSnowing;

		[ReadOnly]
		public float m_Temperature;

		[ReadOnly]
		public float m_Precipitation;

		[ReadOnly]
		public int m_TouristCitizenCount;

		[ReadOnly]
		public int m_Nu;

		[ReadOnly]
		public ClimateSystem.WeatherClassification m_WeatherClassification;

		public void Execute()
		{
			Tourism value = new Tourism
			{
				m_CurrentTourists = m_TouristCitizenCount
			};
			int2 lodging = default(int2);
			for (int i = 0; i < m_HotelChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_HotelChunks[i];
				NativeArray<LodgingProvider> nativeArray = archetypeChunk.GetNativeArray(ref m_LodgingProviderType);
				BufferAccessor<Renter> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RenterType);
				for (int j = 0; j < archetypeChunk.Count; j++)
				{
					LodgingProvider lodgingProvider = nativeArray[j];
					DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[j];
					lodging += new int2(dynamicBuffer.Length, dynamicBuffer.Length + lodgingProvider.m_FreeRooms);
				}
			}
			value.m_Lodging = lodging;
			float num = 0f;
			for (int k = 0; k < m_m_AttractivenessProviderChunks.Length; k++)
			{
				NativeArray<AttractivenessProvider> nativeArray2 = m_m_AttractivenessProviderChunks[k].GetNativeArray(ref m_ProviderType);
				for (int l = 0; l < nativeArray2.Length; l++)
				{
					AttractivenessProvider attractivenessProvider = nativeArray2[l];
					num += (float)(attractivenessProvider.m_Attractiveness * attractivenessProvider.m_Attractiveness) / 10000f;
				}
			}
			num = 200f / (1f + math.exp(-0.3f * num)) - 100f;
			DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
			CityUtils.ApplyModifier(ref num, modifiers, CityModifierType.Attractiveness);
			value.m_Attractiveness = Mathf.RoundToInt(num);
			value.m_AverageTourists = Mathf.RoundToInt(2f * GetTouristProbability(m_Parameters, value.m_Attractiveness, value.m_CurrentTourists, m_WeatherClassification, m_Temperature, m_Precipitation, m_IsRaining, m_IsSnowing) * 100000f / 16f);
			m_Tourisms[m_City] = value;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		public ComponentLookup<Tourism> __Game_City_Tourism_RW_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_LodgingProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingProvider>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_City_Tourism_RW_ComponentLookup = state.GetComponentLookup<Tourism>();
			__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>(isReadOnly: true);
		}
	}

	private int2 m_CachedLodging;

	private CitySystem m_CitySystem;

	private ClimateSystem m_ClimateSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private EntityQuery m_AttractivenessProviderGroup;

	private EntityQuery m_HotelGroup;

	private EntityQuery m_ParameterQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 32768;
	}

	public static int GetTouristRandomStay()
	{
		return 262144;
	}

	public static float GetRawTouristProbability(int attractiveness)
	{
		return (float)attractiveness / 1000f;
	}

	public static int GetTargetTourists(int attractiveness)
	{
		if (attractiveness <= 100)
		{
			return attractiveness * 15;
		}
		float num = attractiveness - 100;
		float f = 100f * Mathf.Log10(1f + num);
		return 1500 + Mathf.RoundToInt(f);
	}

	public static float GetSpawnProbability(int attractiveness, int currentTourists)
	{
		int targetTourists = GetTargetTourists(attractiveness);
		int num = targetTourists * 110 / 100;
		if (currentTourists >= targetTourists)
		{
			return GetRawTouristProbability(attractiveness);
		}
		float num2 = (float)currentTourists / (float)num;
		if (num2 < 0.5f)
		{
			return 1f;
		}
		float num3 = 1f - (num2 - 0.5f) / 0.5f;
		return math.saturate(1.5f * num3 * num3);
	}

	public static float GetTouristProbability(AttractivenessParameterData parameterData, int attractiveness, int numberOfCurrentTourists, ClimateSystem.WeatherClassification weatherClassification, float temperature, float precipitation, bool isRaining, bool isSnowing)
	{
		return GetSpawnProbability(attractiveness, numberOfCurrentTourists) * GetWeatherEffect(parameterData, weatherClassification, temperature, precipitation, isRaining, isSnowing);
	}

	public static float GetWeatherEffect(AttractivenessParameterData parameterData, ClimateSystem.WeatherClassification weatherClassification, float temperature, float precipitation, bool isRaining, bool isSnowing)
	{
		float num = 1f;
		if (temperature > parameterData.m_AttractiveTemperature.x && temperature < parameterData.m_AttractiveTemperature.y)
		{
			num += Mathf.Lerp(parameterData.m_TemperatureAffect.x, 0f, math.abs(temperature - (parameterData.m_AttractiveTemperature.x + parameterData.m_AttractiveTemperature.y) / 2f) / ((parameterData.m_AttractiveTemperature.y - parameterData.m_AttractiveTemperature.x) / 2f));
		}
		else if (temperature > parameterData.m_ExtremeTemperature.y)
		{
			num += Mathf.Lerp(0f, parameterData.m_TemperatureAffect.y, (temperature - parameterData.m_ExtremeTemperature.y) / 10f);
		}
		else if (temperature < parameterData.m_ExtremeTemperature.x)
		{
			num += Mathf.Lerp(0f, parameterData.m_TemperatureAffect.y, (parameterData.m_ExtremeTemperature.x - temperature) / 10f);
		}
		if (isSnowing && precipitation > parameterData.m_SnowEffectRange.x && precipitation < parameterData.m_SnowEffectRange.y)
		{
			num += Mathf.Lerp(0f, parameterData.m_SnowRainExtremeAffect.x, (precipitation - parameterData.m_SnowEffectRange.x) / (parameterData.m_SnowEffectRange.y - parameterData.m_SnowEffectRange.x));
		}
		else if (isRaining && precipitation > parameterData.m_RainEffectRange.x && precipitation < parameterData.m_RainEffectRange.y)
		{
			num += Mathf.Lerp(0f, parameterData.m_SnowRainExtremeAffect.y, (precipitation - parameterData.m_RainEffectRange.x) / (parameterData.m_RainEffectRange.y - parameterData.m_RainEffectRange.x));
		}
		if (weatherClassification == ClimateSystem.WeatherClassification.Stormy)
		{
			num += parameterData.m_SnowRainExtremeAffect.z;
		}
		return math.clamp(num, 0.5f, 1.5f);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_AttractivenessProviderGroup = GetEntityQuery(ComponentType.ReadWrite<AttractivenessProvider>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HotelGroup = GetEntityQuery(ComponentType.ReadOnly<LodgingProvider>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TourismJob jobData = new TourismJob
		{
			m_m_AttractivenessProviderChunks = m_AttractivenessProviderGroup.ToArchetypeChunkArray(Allocator.TempJob),
			m_HotelChunks = m_HotelGroup.ToArchetypeChunkArray(Allocator.TempJob),
			m_LodgingProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_Tourisms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Tourism_RW_ComponentLookup, ref base.CheckedStateRef),
			m_Parameters = m_ParameterQuery.GetSingleton<AttractivenessParameterData>(),
			m_ProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_IsRaining = m_ClimateSystem.isRaining,
			m_IsSnowing = m_ClimateSystem.isSnowing,
			m_Temperature = m_ClimateSystem.temperature,
			m_Precipitation = m_ClimateSystem.precipitation,
			m_TouristCitizenCount = m_CountHouseholdDataSystem.TouristCitizenCount,
			m_WeatherClassification = m_ClimateSystem.classification
		};
		base.Dependency = IJobExtensions.Schedule(jobData, base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public TourismSystem()
	{
	}
}
