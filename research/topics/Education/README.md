# Research: Education System

> **Status**: Complete
> **Date started**: 2026-02-15
> **Last updated**: 2026-02-15

## Scope

**What we're investigating**: How CS2 handles the full education pipeline -- citizens seeking schools, enrollment, daily attendance, graduation, dropout, and how education levels affect citizen eligibility for jobs.

**Why**: To understand the ECS systems governing education so mods can alter graduation rates, school capacity, enrollment behavior, or add new education-related mechanics.

**Boundaries**: This research covers the simulation-side education pipeline only. School building placement, zoning, and district-level education policies are out of scope. The economic effects of education (wage differences) are covered only where they directly influence dropout decisions.

## Relevant Assemblies & Namespaces

| Assembly | Namespace | What's there |
|----------|-----------|-------------|
| Game.dll | Game.Simulation | SchoolAISystem, StudentSystem, FindSchoolSystem, GraduationSystem |
| Game.dll | Game.Citizens | Student, SchoolSeeker, CitizenEducationLevel, Citizen (education fields) |
| Game.dll | Game.Buildings | School (runtime component) |
| Game.dll | Game.Prefabs | SchoolData, SchoolLevel |
| Game.dll | Game.UI.InGame | EducationInfoviewUISystem |

## Architecture Overview

The education system follows a clear pipeline with four main ECS systems:

1. **School seeking** (`FindSchoolSystem`) -- Citizens needing education create a `SchoolSeeker` entity. The system uses pathfinding to locate a suitable school, then enrolls the citizen.
2. **Daily attendance** (`StudentSystem`) -- Enrolled students travel to their school building during study hours and return home after.
3. **School operations** (`SchoolAISystem`) -- Processes each school building, calculating average graduation time and failure probability for its students.
4. **Graduation** (`GraduationSystem`) -- Periodically evaluates each student for graduation, dropout, or failure.

### Education Levels

Citizens have a `CitizenEducationLevel` stored in the `Citizen` component:

| Level | Enum Value | Meaning |
|-------|-----------|---------|
| 0 | Uneducated | No education completed |
| 1 | PoorlyEducated | Elementary school graduate |
| 2 | Educated | High school graduate |
| 3 | WellEducated | College graduate |
| 4 | HighlyEducated | University graduate |

Schools operate at one of the `SchoolLevel` tiers:

| Value | Enum Name | Educates to Level |
|-------|-----------|-------------------|
| 1 | Elementary | 1 (PoorlyEducated) |
| 2 | HighSchool | 2 (Educated) |
| 3 | College | 3 (WellEducated) |
| 4 | University | 4 (HighlyEducated) |
| 5 | Outside | External education |

## Component Map

### `Student` (Game.Citizens)

Attached to a citizen entity when they are enrolled in a school.

| Field | Type | Description |
|-------|------|-------------|
| m_School | Entity | The school building entity the student attends |
| m_LastCommuteTime | float | Duration of the last commute to school (used for dropout calculations) |
| m_Level | byte | The education level being pursued (255 = use school's default level) |

### `School` (Game.Buildings)

Attached to a school building entity. Tracks runtime school statistics.

| Field | Type | Description |
|-------|------|-------------|
| m_AverageGraduationTime | float | Average estimated time to graduation across current students |
| m_AverageFailProbability | float | Average dropout/failure probability across current students |
| m_StudentWellbeing | sbyte | Wellbeing bonus applied to students (scaled by efficiency, range -100 to 100) |
| m_StudentHealth | sbyte | Health bonus applied to students (scaled by efficiency, range -100 to 100) |

### `SchoolData` (Game.Prefabs)

Prefab data defining a school's static characteristics.

| Field | Type | Description |
|-------|------|-------------|
| m_StudentCapacity | int | Maximum number of students this school can hold |
| m_GraduationModifier | float | Additive modifier to graduation probability |
| m_EducationLevel | byte | The education level this school teaches (1-4) |
| m_StudentWellbeing | sbyte | Base wellbeing bonus for attending students |
| m_StudentHealth | sbyte | Base health bonus for attending students |

Implements `ICombineData<SchoolData>` so upgrades can stack capacity and modifiers via `UpgradeUtils.CombineStats`.

### `SchoolSeeker` (Game.Citizens)

Tag component placed on a pathfinding request entity when a citizen is looking for a school.

| Field | Type | Description |
|-------|------|-------------|
| m_Level | int | The education level the citizen is seeking |

## System Details

### FindSchoolSystem (Game.Simulation)

**Update interval**: Every 16 simulation frames.

Handles the enrollment pipeline in two phases:

**Phase 1 -- FindSchoolJob (IJobChunk):** For each `SchoolSeeker` entity without a `PathInformation` component, enqueues a pathfinding request. The destination type is `SetupTargetType.SchoolSeekerTo` with the desired school level. Children cannot use vehicles for pathfinding; adults can.

**Phase 2 -- StartStudyingJob (IJob):** Processes completed pathfinding results:
- Checks the destination school has capacity (`dynamicBuffer.Length < studentCapacity`)
- Adds the citizen to the school's `Student` buffer
- Attaches a `Student` component to the citizen entity
- If the citizen was employed, removes them from their workplace's `Employee` buffer and removes the `Worker` component
- Resets the citizen's failed education count to 0
- Fires `TriggerType.CitizenStartedSchool`
- On failure, attaches `SchoolSeekerCooldown` to prevent immediate retry

### StudentSystem (Game.Simulation)

**Update interval**: Every 16 simulation frames.

Contains two jobs:

**GoToSchoolJob:** Determines when students should travel to school. Uses `IsTimeToStudy()` which:
- Calculates a per-citizen study offset from `CitizenPseudoRandom.WorkOffset` (range approximately +/- 0.042 normalized day)
- Factors in commute time to determine departure time
- Study hours follow the economy parameter `WorkDayStart` and `WorkDayEnd` with offset
- Traffic reduction based on population reduces attendance on some days
- Creates a `TripNeeded` with `Purpose.GoingToSchool`

**StudyJob:** Manages in-progress study sessions:
- If the school no longer exists, removes the student component
- If outside study hours or attending a meeting, removes the `TravelPurpose`

**Key static methods:**
- `GetStudyOffset(Citizen)` -- returns a small time offset so not all students travel simultaneously
- `IsTimeToStudy(...)` -- checks if it is time for this citizen to go to school
- `GetTimeToStudy(...)` -- returns a float2(start, end) of study hours for a citizen

### SchoolAISystem (Game.Simulation)

**Update interval**: Every 256 simulation frames (offset 96).

Processes each school building. For each student in the school's `Student` buffer:
- If the school's efficiency drops below 0.001, students leave with probability `m_InoperableSchoolLeaveProbability`
- Otherwise, calculates graduation probability and failure estimates
- Updates the school's `m_AverageGraduationTime` and `m_AverageFailProbability`
- Scales `m_StudentWellbeing` and `m_StudentHealth` by building efficiency
- Calculates service usage as `students / capacity`

### GraduationSystem (Game.Simulation)

**Update interval**: Every 16,384 simulation frames.

The core graduation logic. Each tick, every student has a 50% chance of being evaluated (random coin flip). For those evaluated:

**Graduation check:**
- Calls `GetGraduationProbability(level, wellbeing, schoolData, modifiers, studyWillingness, efficiency)`
- If `random.NextFloat() < graduationProbability`, the student graduates
- Graduation sets the citizen's education level to `max(currentLevel, schoolLevel)`
- For levels > 1 (high school and above), the student leaves the school
- Elementary students (level 1) graduate but stay (they automatically proceed)
- Fires `TriggerType.CitizenGraduated`

**Failure/dropout check (levels > 2 only):**
- If the student does NOT graduate AND their level is > 2 (college/university):
  - Increments the failed education count
  - If failed count < 3, calculates dropout probability
  - Dropout probability is amplified: `1 - (1 - dropoutProb)^32`
  - If they drop out, fires `TriggerType.CitizenDroppedOutSchool`
  - If failed count >= 3, forced to leave with `TriggerType.CitizenFailedSchool`

**Graduation probability formula:**
```
num = saturate((0.5 + studyWillingness) * wellbeing / 75)

Level 1 (Elementary): smoothstep(0, 1, 0.6 * num + 0.41)
Level 2 (HighSchool): 0.6 * log(2.6 * num + 1.1)
Level 3 (College):    (90 * log(1.6 * num + 1) + collegeModifier.x + result * collegeModifier.y) / 100
Level 4 (University): (70 * num + uniModifier.x + result * uniModifier.y) / 100

Final: 1 - (1 - result) / efficiency + graduationModifier
```

City modifiers `CollegeGraduation` and `UniversityGraduation` can provide additive (x) and multiplicative (y) bonuses.

**Dropout probability formula:**
```
remainingAttempts = 4 - failedEducationCount
failProb = pow(1 - graduationProbability, remainingAttempts)
timeToGrad = 1 / (graduationProbability * 2)
schoolCost = timeToGrad * fee - timeToGrad * unemploymentBenefit  (for level > 2)
remainingWorkDays = elderAgeLimit - currentAge
earningsNow = wage(min(2, level-1)) * remainingWorkDays
earningsWithDegree = lerp(wage(level), wage(level-1), failProb) * (remainingWorkDays - timeToGrad) - schoolCost + (0.5 + studyWillingness) * unemploymentBenefit * timeToGrad

if earningsNow < earningsWithDegree:
    benefit = (earningsWithDegree - earningsNow) / earningsNow
    dropout = saturate(-0.1 + level/4 - 10*benefit - wealth/(earningsWithDegree - earningsNow) + commute/5000)
else:
    dropout = 1.0
```

This means citizens make a rational economic decision: if staying in school produces higher lifetime earnings, they are less likely to drop out. Long commutes and high service fees increase dropout rates.

## Education and Job Eligibility

Education level directly affects which jobs a citizen can hold. The `EconomyParameterData.GetWage(level)` method returns different wage rates per education level. Citizens compare potential earnings at their current education level versus the level they would achieve by graduating, factoring in:
- Time remaining before elder age
- Service fees for education
- Unemployment benefits
- Study willingness (a per-citizen pseudo-random trait)

Higher education levels qualify citizens for higher-tier jobs with better wages, creating the economic incentive to pursue education.

## Modding Entry Points

### Adjusting graduation rates
- **Harmony patch** `GraduationSystem.GetGraduationProbability()` -- static method, easy to patch
- Alternatively, modify `SchoolData.m_GraduationModifier` on prefabs at runtime

### Changing school capacity
- Modify `SchoolData.m_StudentCapacity` on school prefabs
- The capacity check is in `FindSchoolSystem.StartStudyingJob.Execute()`

### Altering dropout behavior
- **Harmony patch** `GraduationSystem.GetDropoutProbability()` -- static method
- The dropout amplification factor (`^32`) is hardcoded in the GraduationJob

### Custom school behavior
- Create a `GameSystemBase` that runs after `SchoolAISystem` (update interval 256)
- Read/write `School` components on building entities
- Modify `Student` buffer entries on school buildings

### Forcing graduation
- The system has a `debugFastGraduationLevel` field that bypasses normal probability checks
- Setting this via reflection can force all students at a specific level to graduate immediately

## Key Constants

| Constant | Value | Location | Meaning |
|----------|-------|----------|---------|
| SchoolAI update interval | 256 | SchoolAISystem.GetUpdateInterval | Frames between school processing ticks |
| Graduation update interval | 16,384 | GraduationSystem.GetUpdateInterval | Frames between graduation checks |
| StudentSystem update interval | 16 | StudentSystem.GetUpdateInterval | Frames between attendance checks |
| FindSchool update interval | 16 | FindSchoolSystem.GetUpdateInterval | Frames between school search ticks |
| Max failed attempts | 3 | GraduationSystem.GraduationJob | Failures before forced school exit |
| Graduation check slowdown | 2 | GraduationSystem (NextInt(2)) | 50% chance of being evaluated each tick |
| Max pathfind cost | kMaxPathfindCost | CitizenBehaviorSystem | Maximum pathfinding cost for school search |

## Open Questions

- **EducationParameterData**: Referenced by `SchoolAISystem` (field `m_InoperableSchoolLeaveProbability`) but the type definition is not in Game.dll. It may be defined in another assembly or generated at runtime. The exact leave probability value when a school becomes inoperable is unknown.
- **School selection criteria**: `FindSchoolSystem` uses `SetupTargetType.SchoolSeekerTo` for pathfinding destinations, but the exact filtering logic (how the pathfinder selects which school) is in the pathfinding system, not decompiled here.
- **Elementary auto-progression**: After graduating elementary (level 1), students stay enrolled. The mechanism for transitioning them to high school seeking is not in these systems -- it may be in `CitizenBehaviorSystem` or `AgingSystem`.
- **Outside education**: `SchoolLevel.Outside` (value 5) exists but no system explicitly handles it. It may be used for citizens educated via outside connections.

## Snippets

All decompiled source is saved in `snippets/`:

| File | Type | Namespace |
|------|------|-----------|
| SchoolAISystem.cs | System | Game.Simulation |
| StudentSystem.cs | System | Game.Simulation |
| FindSchoolSystem.cs | System | Game.Simulation |
| GraduationSystem.cs | System | Game.Simulation |
| Student.cs | Component | Game.Citizens |
| School.cs | Component | Game.Buildings |
| SchoolData.cs | Prefab data | Game.Prefabs |
| SchoolSeeker.cs | Component | Game.Citizens |
| CitizenEducationLevel.cs | Enum | Game.Citizens |
| SchoolLevel.cs | Enum | Game.Prefabs |
| EducationInfoviewUISystem.cs | UI System | Game.UI.InGame |
