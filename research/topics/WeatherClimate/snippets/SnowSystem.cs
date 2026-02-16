// Decompiled from Game.dll — Game.Simulation.SnowSystem
// GPU-based snow accumulation and melting simulation.
// 1024x1024 RenderTexture (R16G16_UNorm), updates every 4 frames.

namespace Game.Simulation;

public class SnowSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
{
    private const int kTexSize = 1024;
    private const float kTimeStep = 0.2f;
    private const float kSnowHeightScale = 8f;
    private const float kSnowMeltScale = 1f;
    private const float m_SnowAddConstant = 1E-05f;    // Snow accumulation rate
    private const float m_WaterAddConstant = 0.1f;      // Water from melting snow

    public int SnowSimSpeed { get; set; } // default 1
    public override int GetUpdateInterval(SystemUpdatePhase phase) => 4;

    // AddSnow kernel inputs (from ClimateSystem):
    //   _Temperature = ClimateSystem.temperature
    //   _Rain = ClimateSystem.precipitation
    //   _Wind = WindSimulationSystem.constantWind
    //   _HeightScale = (terrainHeightScaleOffset, temperatureBaseHeight, snowTemperatureHeightScale)
    //   _AddMultiplier = 1E-05f
    //   _MeltMultiplier = 2E-05f
    //   _AddWaterMultiplier = 0.1f
    //   _ElapseWaterMultiplier = 0.05f
    //
    // Snow accumulates when temperature <= freezing AND precipitation > 0.
    // Snow melts when temperature > freezing.
    // Height affects snow: snowTemperatureHeightScale = 0.01f means
    //   1 degree cooler per 100 world units of elevation.

    // SnowTransfer kernel: redistributes snow based on wind
    //   Only runs when precipitation >= 0.1 AND adjusted temperature <= 0
    //   Uses terrain heightmap and wind direction

    // OnUpdate: runs AddSnow + SnowTransfer (SnowSimSpeed times), then updates backdrop texture
    protected override void OnUpdate()
    {
        // if (m_WaterSystem.Loaded)
        //     for (i = 0; i < SnowSimSpeed; i++)
        //         AddSnow + SnowTransfer
        //     UpdateSnowBackdropTexture
    }
}
