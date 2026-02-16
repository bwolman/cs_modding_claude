// Decompiled from Game.dll — Game.Simulation.HouseholdFindPropertySystem
// Matches homeless or property-seeking households with available residential properties.

using Game.Citizens;
using Unity.Burst;
using Unity.Entities;

namespace Game.Simulation;

public class HouseholdFindPropertySystem : GameSystemBase
{
    // Quality evaluation for apartments
    public struct GenericApartmentQuality
    {
        public float apartmentSize;      // Space per household member
        public float2 educationBonus;    // Education coverage bonus
        public float welfareBonus;       // Welfare coverage bonus
        public float score;              // Overall quality score
        public int level;                // Building level
    }

    // PreparePropertyJob:
    // - Runs on all residential buildings with Renter buffers
    // - Calculates free units: totalResidentialProperties - currentHouseholdRenters
    // - For abandoned buildings or parks with AllowHomeless: uses shelter capacity
    // - Computes GenericApartmentQuality using PropertyUtils.GetGenericApartmentQuality()
    //   which evaluates pollution, crime, services, utilities, etc.
    // - Caches results in NativeParallelHashMap<Entity, CachedPropertyInformation>

    // FindPropertyJob:
    // - Processes households that need homes (homeless + moved-in seeking better)
    // - Uses pathfinding results (PathInformations) to find reachable properties
    // - Evaluates property score based on quality, commute distance, household wealth
    // - Assigns best property via PropertyRenter component
    // - Handles both normal households and homeless (shelter) cases
}
