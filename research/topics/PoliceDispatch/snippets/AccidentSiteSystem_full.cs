// Decompiled from Game.dll using ilspycmd v9.1
// Full source for Game.Simulation.AccidentSiteSystem
// THE gatekeeper for police dispatch -- manages RequirePolice flag lifecycle

using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class AccidentSiteSystem : GameSystemBase
{
	private const uint UPDATE_INTERVAL = 64u;

	[BurstCompile]
	private struct AccidentSiteJob : IJobChunk
	{
		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<AccidentSite> nativeArray2 = chunk.GetNativeArray(ref m_AccidentSiteType);
			bool flag = chunk.Has(ref m_BuildingType);

			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				AccidentSite accidentSite = nativeArray2[i];
				Random random = m_RandomSeed.GetRandom(entity.Index);
				Entity entity2 = Entity.Null;  // highest-severity non-moving target
				int num = 0;                    // count of involved entities
				float num2 = 0f;                // max severity

				// Clear staging after 3600 frames (~60s)
				if (m_SimulationFrame - accidentSite.m_CreationFrame >= 3600)
				{
					accidentSite.m_Flags &= ~AccidentSiteFlags.StageAccident;
				}

				accidentSite.m_Flags &= ~AccidentSiteFlags.MovingVehicles;

				// Iterate TargetElement buffer to count involved entities
				if (m_TargetElements.HasBuffer(accidentSite.m_Event))
				{
					DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[accidentSite.m_Event];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity entity3 = dynamicBuffer[j].m_Entity;
						if (m_InvolvedInAccidentData.TryGetComponent(entity3, out var componentData))
						{
							if (componentData.m_Event == accidentSite.m_Event)
							{
								num++;
								// Track moving vehicles, max severity, etc.
							}
						}
						else if (m_CriminalData.HasComponent(entity3))
						{
							Criminal criminal = m_CriminalData[entity3];
							if (criminal.m_Event == accidentSite.m_Event && (criminal.m_Flags & CriminalFlags.Arrested) == 0)
							{
								num++;
								if ((criminal.m_Flags & CriminalFlags.Monitored) != 0)
								{
									accidentSite.m_Flags |= AccidentSiteFlags.CrimeMonitored;
								}
							}
						}
					}
				}

				// *** CRITICAL LINE 227: UNCONDITIONALLY CLEARS RequirePolice ***
				accidentSite.m_Flags &= ~AccidentSiteFlags.RequirePolice;

				// Re-evaluate: set RequirePolice if conditions met
				if (num2 > 0f || (accidentSite.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene)) == AccidentSiteFlags.CrimeScene)
				{
					if (num2 > 0f || (accidentSite.m_Flags & AccidentSiteFlags.CrimeDetected) != 0)
					{
						if (flag) entity2 = entity;  // Building entity is the target
						if (entity2 != Entity.Null)
						{
							accidentSite.m_Flags |= AccidentSiteFlags.RequirePolice;
							RequestPoliceIfNeeded(unfilteredChunkIndex, entity, ref accidentSite, entity2, num2);
						}
					}
				}
				// *** ACCIDENT SITE REMOVAL: when no involved entities ***
				else if (num == 0 && ((accidentSite.m_Flags & (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene)) != (AccidentSiteFlags.Secured | AccidentSiteFlags.CrimeScene) || m_SimulationFrame >= accidentSite.m_SecuredFrame + 1024))
				{
					m_CommandBuffer.RemoveComponent<AccidentSite>(unfilteredChunkIndex, entity);
				}

				nativeArray2[i] = accidentSite;
			}
		}

		// RequestPoliceIfNeeded - creates PoliceEmergencyRequest if no active request
		private void RequestPoliceIfNeeded(int jobIndex, Entity entity, ref AccidentSite accidentSite, Entity target, float severity)
		{
			if (!m_PoliceEmergencyRequestData.HasComponent(accidentSite.m_PoliceRequest))
			{
				PolicePurpose purpose = (((accidentSite.m_Flags & AccidentSiteFlags.CrimeMonitored) == 0) ? PolicePurpose.Emergency : PolicePurpose.Intelligence);
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PoliceRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new PoliceEmergencyRequest(entity, target, severity, purpose));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
			}
		}
	}

	// System update: every 64 frames
	public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;
}
