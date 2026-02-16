// Full source: ~160 lines. Key logic preserved with annotations.
// Decompiled from: Game.dll -> Game.Events.AddCriminalSystem

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class AddCriminalSystem : GameSystemBase
{
	// ===== AddCriminalJob (IJob) =====
	// Processes AddCriminal event entities to add Criminal component to target citizens.
	//
	// For each AddCriminal event:
	//   - If target already has Criminal: merge flags (Prisoner takes priority)
	//   - If target is new criminal: add Criminal component via EntityCommandBuffer
	//   - Adds target citizen to the crime event's TargetElement buffer
	//
	// Merge logic (MergeCriminals):
	//   - If one is Prisoner and other is not: keep the Prisoner
	//   - If one has an event and other doesn't: keep the one with event, merge flags
	//   - Otherwise: keep first, OR the flags together

	[BurstCompile]
	private struct AddCriminalJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<AddCriminal> m_AddCriminalType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<Criminal> m_Criminals;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			// Collects all AddCriminal events into NativeParallelHashMap (merging duplicates)
			// Then applies: either updates existing Criminal or adds new via CommandBuffer
			// Also adds citizen to crime event's TargetElement buffer
		}

		private Criminal MergeCriminals(Criminal criminal1, Criminal criminal2)
		{
			// Prisoner flag takes priority (existing prisoner state preserved)
			// Non-null m_Event takes priority
			// Flags are OR'd together
			if (((criminal1.m_Flags ^ criminal2.m_Flags) & CriminalFlags.Prisoner) != 0)
			{
				if ((criminal1.m_Flags & CriminalFlags.Prisoner) == 0)
					return criminal2;
				return criminal1;
			}
			// ... merges flags
			return criminal1;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		// Query: Event + AddCriminal
		// Uses ModificationBarrier4 for command buffer
	}
}
