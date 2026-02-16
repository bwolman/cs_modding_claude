// Decompiled from Game.dll — Game.Simulation.ClimateSystem
// Central orchestrator for weather, temperature, precipitation, cloudiness, seasons.
// Implements IMod serialization, samples climate curves each frame, updates season + weather.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Effects;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Rendering;
using Game.Serialization;
using Game.Triggers;
using Game.UI.Widgets;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class ClimateSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPreSerialize, IPostDeserialize
{
    [Serializable]
    public class SeasonInfo
    {
        public SeasonPrefab m_Prefab;
        [Range(0f, 1f)] public float m_StartTime;
        [Range(-70f, 70f)] public float2 m_TempNightDay = new float2(5f, 20f);
        [Range(0.5f, 40f)] public float2 m_TempDeviationNightDay = new float2(4f, 7f);
        [Range(0f, 100f)] public float m_CloudChance = 50f;
        [Range(0f, 100f)] public float m_CloudAmount = 40f;
        [Range(1f, 100f)] public float m_CloudAmountDeviation = 20f;
        [Range(0f, 100f)] public float m_PrecipitationChance = 30f;
        [Range(0f, 100f)] public float m_PrecipitationAmount = 40f;
        [Range(1f, 100f)] public float m_PrecipitationAmountDeviation = 30f;
        [Range(0f, 1f)] public float m_Turbulence = 0.2f;
        [Range(0f, 1f)] public float m_AuroraAmount = 1f;
        [Range(0f, 100f)] public float m_AuroraChance = 10f;
        public string name => m_Prefab?.name;
    }

    public enum WeatherClassification
    {
        Irrelevant, Clear, Few, Scattered, Broken, Overcast, Stormy
    }

    public struct ClimateSample
    {
        public float temperature;
        public float precipitation;
        public float cloudiness;
        public float aurora;
        public float fog;
    }

    // Key properties (OverridableProperty allows editor overrides)
    public OverridableProperty<float> precipitation { get; }
    public OverridableProperty<float> temperature { get; }
    public OverridableProperty<float> cloudiness { get; }
    public OverridableProperty<float> aurora { get; }
    public OverridableProperty<float> fog { get; }
    public OverridableProperty<float> thunder;
    public float2 wind { get; private set; } = new float2(0.0275f, 0.0275f);
    public float hail { get; set; }
    public float rainbow { get; set; }
    public float averageTemperature { get; private set; }
    public float freezingTemperature { get; private set; }
    public float seasonTemperature { get; private set; }
    public float seasonPrecipitation { get; private set; }
    public float seasonCloudiness { get; private set; }
    public WeatherClassification classification { get; private set; }
    public float snowTemperatureHeightScale => 0.01f;

    public bool isRaining => precipitation > 0f && temperature > freezingTemperature;
    public bool isSnowing => precipitation > 0f && temperature <= freezingTemperature;
    public bool isPrecipitating => precipitation > 0f;

    // OnUpdate: sample climate curves, update season, update weather effects
    protected override void OnUpdate()
    {
        if (currentClimate != Entity.Null)
        {
            ClimatePrefab prefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(currentClimate);
            ClimateSample climateSample = SampleClimate(prefab, m_Date);
            temperature.value = climateSample.temperature;
            precipitation.value = climateSample.precipitation;
            cloudiness.value = climateSample.cloudiness;
            aurora.value = climateSample.aurora;
            fog.value = climateSample.fog;
            UpdateSeason(prefab, m_Date);
            UpdateWeather(prefab);
        }
        if (m_TriggerSystem.Enabled)
            HandleTriggers();
    }

    // SampleClimate: evaluates animation curves from ClimatePrefab at normalized date
    public ClimateSample SampleClimate(ClimatePrefab prefab, float t)
    {
        float time = t * (float)m_TimeSystem.daysPerYear;
        return new ClimateSample
        {
            temperature = prefab.m_Temperature.Evaluate(time),
            precipitation = prefab.m_Precipitation.Evaluate(time),
            cloudiness = prefab.m_Cloudiness.Evaluate(time),
            aurora = prefab.m_Aurora.Evaluate(time),
            fog = prefab.m_Aurora.Evaluate(time) // Note: fog uses aurora curve
        };
    }

    // HandleTriggers: fires trigger events based on current weather state
    private void HandleTriggers()
    {
        // Fires TriggerType.Temperature, WeatherStormy, WeatherRainy, WeatherSnowy,
        // WeatherSunny, WeatherClear, WeatherCloudy, AuroraBorealis
        // Based on: hail, cloudiness > 0.5, time of day, classification, temperature > 15
    }
}
