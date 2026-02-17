// Decompiled from Game.dll -> Game.Simulation.GameModeGovernmentSubsidiesSystem
// Decompiled with ilspycmd on 2026-02-16

// Government subsidy system - only active when ModeSettingData.m_EnableGovernmentSubsidies is true.
//
// Runs 128 times per day.
// Subsidy triggers when PlayerMoney < ModeSettingData.m_MoneyCoverThreshold.x
//
// Formula:
//   range = threshold.x - threshold.y
//   coverRatio = clamp(1 - (money - threshold.y) / range, 0, 1)
//   subsidyFraction = coverRatio * (MaxMoneyCoverPercentage / 100)
//   monthlySubsidy = abs(subsidyFraction * TotalExpenses)
//   lastSubsidyCoverPerDay = monthlySubsidy / 128
//
// So subsidy scales linearly from 0% to MaxMoneyCoverPercentage of total expenses,
// based on how far money has dropped below the threshold range.
// When money is above threshold.x, no subsidy.
// When money is at or below threshold.y, maximum subsidy.
