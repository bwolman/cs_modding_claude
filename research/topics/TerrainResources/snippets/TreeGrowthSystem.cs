// Source: Game.dll -> Game.Simulation.TreeGrowthSystem
// Decompiled with ILSpy

using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TreeGrowthSystem : GameSystemBase
{
	[BurstCompile]
	private struct TreeGrowthJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Tree> m_TreeType;
		public ComponentTypeHandle<Destroyed> m_DestroyedType;
		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			// Processes trees differently based on whether they are Destroyed, Damaged, or normal
			// Destroyed trees: tick cleared counter until fully cleared, then reset to child
			// Damaged trees: heal damage over time, remove Damaged component when healed
			// Normal trees: progress through life cycle stages
		}

		private bool TickTree(ref Tree tree, ref Random random)
		{
			switch (tree.m_State & (TreeState.Teen | TreeState.Adult | TreeState.Elderly | TreeState.Dead | TreeState.Stump))
			{
			case TreeState.Teen:    return TickTeen(ref tree, ref random);
			case TreeState.Adult:   return TickAdult(ref tree, ref random);
			case TreeState.Elderly: return TickElderly(ref tree, ref random);
			case TreeState.Dead:
			case TreeState.Stump:   return TickDead(ref tree, ref random);
			default:                return TickChild(ref tree, ref random);
			}
		}

		// Tick speeds control growth rate via random threshold
		// Higher value = slower growth
		private bool TickChild(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(1280) >> 8);  // TICK_SPEED_CHILD = 1280
			if (num < 256) { tree.m_Growth = (byte)num; return false; }
			tree.m_State |= TreeState.Teen;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickTeen(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(938) >> 8);   // TICK_SPEED_TEEN = 938
			if (num < 256) { tree.m_Growth = (byte)num; return false; }
			tree.m_State = (tree.m_State & ~TreeState.Teen) | TreeState.Adult;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickAdult(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(548) >> 8);   // TICK_SPEED_ADULT = 548
			if (num < 256) { tree.m_Growth = (byte)num; return false; }
			tree.m_State = (tree.m_State & ~TreeState.Adult) | TreeState.Elderly;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickElderly(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(548) >> 8);   // TICK_SPEED_ELDERLY = 548
			if (num < 256) { tree.m_Growth = (byte)num; return false; }
			tree.m_State = (tree.m_State & ~TreeState.Elderly) | TreeState.Dead;
			tree.m_Growth = 0;
			return true;
		}

		private bool TickDead(ref Tree tree, ref Random random)
		{
			int num = tree.m_Growth + (random.NextInt(2304) >> 8);  // TICK_SPEED_DEAD = 2304
			if (num < 256) { tree.m_Growth = (byte)num; return false; }
			tree.m_State &= ~(TreeState.Dead | TreeState.Stump);
			tree.m_Growth = 0;
			return true;
		}
	}

	// --- Constants ---
	public const int UPDATES_PER_DAY = 32;
	public const int TICK_SPEED_CHILD = 1280;
	public const int TICK_SPEED_TEEN = 938;
	public const int TICK_SPEED_ADULT = 548;
	public const int TICK_SPEED_ELDERLY = 548;
	public const int TICK_SPEED_DEAD = 2304;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TreeQuery = GetEntityQuery(
			ComponentType.ReadWrite<Tree>(),
			ComponentType.ReadOnly<UpdateFrame>(),
			ComponentType.Exclude<Deleted>(),
			ComponentType.Exclude<Overridden>(),
			ComponentType.Exclude<Temp>());
		RequireForUpdate(m_TreeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 32, 16);
		m_TreeQuery.ResetFilter();
		m_TreeQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));
		// Schedules TreeGrowthJob as parallel chunk job
	}
}
