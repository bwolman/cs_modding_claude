// Source: Game.dll -> Game.City.IncomeSource
namespace Game.City;

public enum IncomeSource
{
    TaxResidential,
    TaxCommercial,
    TaxIndustrial,
    FeeHealthcare,
    FeeElectricity,
    GovernmentSubsidy,
    FeeEducation,
    ExportElectricity,
    ExportWater,
    FeeParking,
    FeePublicTransport,
    TaxOffice,
    FeeGarbage,
    FeeWater,
    Count
}

// Source: Game.dll -> Game.City.ExpenseSource
public enum ExpenseSource
{
    SubsidyResidential,
    LoanInterest,
    ImportElectricity,
    ImportWater,
    ExportSewage,
    ServiceUpkeep,
    SubsidyCommercial,
    SubsidyIndustrial,
    SubsidyOffice,
    ImportPoliceService,
    ImportAmbulanceService,
    ImportHearseService,
    ImportFireEngineService,
    ImportGarbageService,
    MapTileUpkeep,
    Count
}

// Source: Game.dll -> Game.City.PlayerResource
public enum PlayerResource
{
    Electricity,
    Healthcare,
    BasicEducation,
    SecondaryEducation,
    HigherEducation,
    Garbage,
    Water,
    Mail,
    PublicTransport,
    FireResponse,
    Police,
    Sewage,
    Parking,
    Count
}

// Source: Game.dll -> Game.Simulation.TaxAreaType
public enum TaxAreaType : byte
{
    None,
    Residential,
    Commercial,
    Industrial,
    Office
}

// Source: Game.dll -> Game.City.TaxRate
public enum TaxRate
{
    Main = 0,
    ResidentialOffset = 1,
    CommercialOffset = 2,
    IndustrialOffset = 3,
    OfficeOffset = 4,
    EducationZeroOffset = 5,
    CommercialResourceZeroOffset = 10,
    IndustrialResourceZeroOffset = 51,
    Count = 92
}
