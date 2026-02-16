// NOTE: Abbreviated for key logic -- see full decompiled source for TypeHandle and complete scheduling code.
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class NetPollutionSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateNetPollutionJob : IJobChunk
	{
		// ... handles omitted for brevity ...

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			PollutionParameterData pollutionParameters = m_PollutionParameters;
			float t = 4f / (float)kUpdatesPerDay;
			// For nodes: accumulate pollution from connected edges, then apply
			// For edges (curves): sample along bezier curve length, apply air + noise
			// Sound barriers reduce noise: both sides = 0x, one side = 0.5x left/1.5x right
			// Beautification reduces noise: both sides = 0.5x, one side = 0.75x
			// Tunnels skip noise pollution entirely
		}

		private void CheckUpgrades(ref float3 noisePollution, Upgraded upgraded)
		{
			if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
			{
				noisePollution *= new float3(0f, 0.5f, 0f);
			}
			else if ((upgraded.m_Flags.m_Left & CompositionFlags.Side.SoundBarrier) != 0)
			{
				noisePollution *= new float3(0f, 0.5f, 1.5f);
			}
			else if ((upgraded.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
			{
				noisePollution *= new float3(1.5f, 0.5f, 0f);
			}
			if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.PrimaryBeautification) != 0)
			{
				noisePollution *= new float3(0.5f, 0.5f, 0.5f);
			}
			// ... secondary beautification and middle beautification similar patterns ...
		}
	}

	public static readonly int kUpdatesPerDay = 128;
}
