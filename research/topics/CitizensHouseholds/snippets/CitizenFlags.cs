// Decompiled from Game.dll — Game.Citizens.CitizenFlags, CitizenAge
// Enums encoding citizen state in bitfields.

using System;

namespace Game.Citizens;

public enum CitizenAge
{
    Child,
    Teen,
    Adult,
    Elderly
}

[Flags]
public enum CitizenFlags : short
{
    None = 0,
    AgeBit1 = 1,
    AgeBit2 = 2,
    MovingAwayReachOC = 4,
    Male = 8,
    EducationBit1 = 0x10,
    EducationBit2 = 0x20,
    EducationBit3 = 0x40,
    FailedEducationBit1 = 0x80,
    FailedEducationBit2 = 0x100,
    Tourist = 0x200,
    Commuter = 0x400,
    LookingForPartner = 0x800,
    NeedsNewJob = 0x1000,
    BicycleUser = 0x2000
}
