// Full source: Game.dll -> Game.Prefabs.Crime (ComponentBase)
// This is the prefab authoring component that creates CrimeData on the event prefab.

using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Events;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Events/", new Type[] { typeof(EventPrefab) })]
public class Crime : ComponentBase
{
    public EventTargetType m_RandomTargetType = EventTargetType.Citizen;
    public CrimeType m_CrimeType;

    [Tooltip("The probability for citizen to do the crime first time")]
    public Bounds1 m_OccurenceProbability = new Bounds1(0f, 50f);

    [Tooltip("The probability for criminal to do the crime again")]
    public Bounds1 m_RecurrenceProbability = new Bounds1(0f, 100f);

    [Tooltip("The police may be requested after crime start min seconds, and will be surely requested after max seconds")]
    public Bounds1 m_AlarmDelay = new Bounds1(5f, 10f);

    [Tooltip("The crime can be finished earlier without caught if it's last longer than min seconds, if the crime last longer than max seconds without police arrive and secure the crime scene, the criminal will not be caught")]
    public Bounds1 m_CrimeDuration = new Bounds1(20f, 60f);

    [Tooltip("The absolute part of steal money amount from crime victim, random between min and max, combined with relative for final steal amount")]
    public Bounds1 m_CrimeIncomeAbsolute = new Bounds1(100f, 1000f);

    [Tooltip("The percentage of steal money amount from victim's money, random between min and max, combined with absolute for final steal amount")]
    public Bounds1 m_CrimeIncomeRelative = new Bounds1(0f, 0.25f);

    [Tooltip("The criminal will be put into jail for how many game days")]
    public Bounds1 m_JailTimeRange = new Bounds1(0.125f, 1f);

    [Tooltip("The criminal will be sentenced to prison for how many game days after jail time")]
    public Bounds1 m_PrisonTimeRange = new Bounds1(1f, 100f);

    [Tooltip("The probability of the criminal in jail sentence to prison")]
    public float m_PrisonProbability = 50f;

    // GetPrefabComponents: adds CrimeData to the prefab entity
    public override void GetPrefabComponents(HashSet<ComponentType> components)
    {
        components.Add(ComponentType.ReadWrite<CrimeData>());
    }

    // GetArchetypeComponents: defines what components crime event instances get
    // This is the ARCHETYPE that EventData.m_Archetype stores:
    //   - Game.Events.Crime (tag component)
    //   - TargetElement (buffer)
    public override void GetArchetypeComponents(HashSet<ComponentType> components)
    {
        components.Add(ComponentType.ReadWrite<Game.Events.Crime>());
        components.Add(ComponentType.ReadWrite<TargetElement>());
    }

    // Initialize: copies values from this authoring component to CrimeData on the prefab entity
    public override void Initialize(EntityManager entityManager, Entity entity)
    {
        base.Initialize(entityManager, entity);
        CrimeData componentData = default(CrimeData);
        componentData.m_RandomTargetType = m_RandomTargetType;
        componentData.m_CrimeType = m_CrimeType;
        componentData.m_OccurenceProbability = m_OccurenceProbability;
        componentData.m_RecurrenceProbability = m_RecurrenceProbability;
        componentData.m_AlarmDelay = m_AlarmDelay;
        componentData.m_CrimeDuration = m_CrimeDuration;
        componentData.m_CrimeIncomeAbsolute = m_CrimeIncomeAbsolute;
        componentData.m_CrimeIncomeRelative = m_CrimeIncomeRelative;
        componentData.m_JailTimeRange = m_JailTimeRange;
        componentData.m_PrisonTimeRange = m_PrisonTimeRange;
        componentData.m_PrisonProbability = m_PrisonProbability;
        entityManager.SetComponentData(entity, componentData);
    }
}
