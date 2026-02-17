// Decompiled from Game.dll -> Game.Simulation.ServiceFeeSystem
// Decompiled with ilspycmd on 2026-02-16

// Key details:
// - kUpdatesPerDay = 128 (defined as const, not static readonly)
// - GetUpdateInterval returns 2048 (= 262144 / 128)
// - Queries buildings with ServiceFeeCollector + (Patient or Student), excluding OutsideConnection
// - PayFeeJob: fee = GetFee(resource, fees) / 128f; deducts from household Resources
// - FeeToCityJob: accumulates FeeEvents into CollectedCityServiceFeeData buffers
//   - Positive amount + !Outside => Internal
//   - Positive amount + Outside => Export
//   - Negative amount => Import
//   - All amounts multiplied by 128 when stored
// - TriggerJob: sends trade balance triggers per PlayerResource
//
// Static helpers:
//   GetFee(PlayerResource, DynamicBuffer<ServiceFee>) -> float
//   TryGetFee(PlayerResource, DynamicBuffer<ServiceFee>, out float) -> bool
//   SetFee(PlayerResource, DynamicBuffer<ServiceFee>, float) -> void
//   GetEducationResource(int level) -> PlayerResource (1=Basic, 2=Secondary, 3/4=Higher)
//   GetConsumptionMultiplier(PlayerResource, float relativeFee, ServiceFeeParameterData)
//   GetEfficiencyMultiplier(PlayerResource, float relativeFee, BuildingEfficiencyParameterData)
//   GetHappinessEffect(PlayerResource, float relativeFee, CitizenHappinessParameterData)
//   GetServiceFees(PlayerResource, NativeList<CollectedCityServiceFeeData>) -> int3 (internal, export, import)
//   GetServiceFeeIncomeEstimate(PlayerResource, float fee, NativeList<CollectedCityServiceFeeData>) -> int
