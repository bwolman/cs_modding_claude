using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Notifications;

[CompilerGenerated]
public class IconClusterSystem : GameSystemBase, IPreDeserialize
{
	public struct ClusterData
	{
		private NativeList<IconCluster> m_Clusters;

		private NativeList<ClusterIcon> m_Icons;

		private NativeList<int> m_Roots;

		public bool isEmpty => m_Clusters.Length == 0;

		public ClusterData(NativeList<IconCluster> clusters, NativeList<ClusterIcon> icons, NativeList<int> roots)
		{
			m_Clusters = clusters;
			m_Icons = icons;
			m_Roots = roots;
		}

		public bool GetRoot(ref int index, out IconCluster cluster)
		{
			if (m_Roots.Length > index)
			{
				cluster = m_Clusters[m_Roots[index++]];
				return true;
			}
			cluster = default(IconCluster);
			return false;
		}

		public IconCluster GetCluster(int index)
		{
			return m_Clusters[index];
		}

		public NativeArray<ClusterIcon> GetIcons(int firstIcon, int iconCount)
		{
			return m_Icons.AsArray().GetSubArray(firstIcon, iconCount);
		}
	}

	public struct IconCluster : IEquatable<IconCluster>
	{
		private float3 m_Center;

		private float3 m_Size;

		private int2 m_SubClusters;

		private float m_DistanceFactor;

		private float m_Radius;

		private int m_ParentCluster;

		private NativeHeapBlock m_IconAllocation;

		private int m_IconCount;

		private int m_Level;

		private int m_PrefabIndex;

		private IconClusterLayer m_Layer;

		private IconFlags m_Flags;

		private bool m_IsMoving;

		private bool m_IsTemp;

		public float3 center => m_Center;

		public float3 size => m_Size;

		public float distanceFactor => m_DistanceFactor;

		public IconClusterLayer layer => m_Layer;

		public IconFlags flags => m_Flags;

		public bool isMoving => m_IsMoving;

		public bool isTemp => m_IsTemp;

		public int parentCluster => m_ParentCluster;

		public int level => m_Level;

		public int prefabIndex => m_PrefabIndex;

		public IconCluster(float3 center, float3 size, int parentCluster, int2 subClusters, float radius, float distanceFactor, NativeHeapBlock iconAllocation, IconClusterLayer layer, IconFlags flags, int iconCount, int level, int prefabIndex, bool isMoving, bool isTemp)
		{
			m_Center = center;
			m_Size = size;
			m_ParentCluster = parentCluster;
			m_SubClusters = subClusters;
			m_DistanceFactor = distanceFactor;
			m_Radius = radius;
			m_IconAllocation = iconAllocation;
			m_Layer = layer;
			m_Flags = flags;
			m_IconCount = iconCount;
			m_Level = level;
			m_PrefabIndex = prefabIndex;
			m_IsMoving = isMoving;
			m_IsTemp = isTemp;
		}

		public IconCluster(IconCluster cluster1, IconCluster cluster2, int index1, int index2, NativeHeapBlock iconAllocation, int iconCount, int level)
		{
			float3 @float = math.min(cluster1.m_Center - cluster1.m_Size, cluster2.m_Center - cluster2.m_Size) * 0.5f;
			float3 float2 = math.max(cluster1.m_Center + cluster1.m_Size, cluster2.m_Center + cluster2.m_Size) * 0.5f;
			float num = math.distance(cluster1.m_Center, cluster2.m_Center);
			float num2 = math.select(1f, 0.5f, iconCount == cluster1.m_IconCount + cluster2.m_IconCount);
			m_Center = @float + float2;
			m_Size = float2 - @float;
			m_ParentCluster = 0;
			m_SubClusters = new int2(index1, index2);
			m_IconAllocation = iconAllocation;
			m_IconCount = iconCount;
			m_Radius = math.max(cluster1.m_Radius, cluster2.m_Radius);
			m_DistanceFactor = math.max(cluster1.m_DistanceFactor, cluster2.m_DistanceFactor);
			m_DistanceFactor = math.max(m_DistanceFactor, num / (m_Radius * num2));
			m_PrefabIndex = ((cluster1.m_PrefabIndex == cluster2.m_PrefabIndex) ? cluster1.m_PrefabIndex : (-1));
			m_Level = level;
			m_IsMoving = cluster1.m_IsMoving && cluster2.m_IsMoving;
			m_IsTemp = cluster1.m_IsTemp;
			m_Layer = cluster1.m_Layer;
			m_Flags = cluster1.m_Flags & cluster2.m_Flags;
		}

		public static void SetParent(ref IconCluster cluster, int parent)
		{
			cluster.m_ParentCluster = parent;
		}

		public bool GetSubClusters(out int2 subClusters)
		{
			subClusters = m_SubClusters;
			return m_SubClusters.x != 0;
		}

		public float GetRadius(float distance)
		{
			return CalculateRadius(m_Radius, distance);
		}

		public static float CalculateRadius(float radius, float distance)
		{
			return radius * math.pow(distance, 0.6f) * 0.063f;
		}

		public Bounds3 GetBounds(float distance, float3 cameraUp)
		{
			float radius = GetRadius(distance);
			float3 @float = m_Center + cameraUp * radius;
			float3 float2 = m_Size + radius;
			return new Bounds3(@float - float2, @float + float2);
		}

		public bool KeepCluster(float distance)
		{
			if (m_SubClusters.x >= 0)
			{
				return math.pow(distance, 0.6f) * 0.18900001f * (1f + distance * 2E-05f) > m_DistanceFactor;
			}
			return true;
		}

		public NativeArray<ClusterIcon> GetIcons(ClusterData clusterData)
		{
			return clusterData.GetIcons((int)m_IconAllocation.Begin, m_IconCount);
		}

		public NativeHeapBlock GetIcons(out int firstIcon, out int iconCount)
		{
			firstIcon = (int)m_IconAllocation.Begin;
			iconCount = m_IconCount;
			return m_IconAllocation;
		}

		public bool Equals(IconCluster other)
		{
			if (m_Center.Equals(other.m_Center) && m_Size.Equals(other.m_Size) && m_SubClusters.Equals(other.m_SubClusters) && m_DistanceFactor.Equals(other.m_DistanceFactor) && m_Radius.Equals(other.m_Radius) && m_ParentCluster.Equals(other.m_ParentCluster) && m_IconAllocation.Begin.Equals(other.m_IconAllocation.Begin) && m_IconCount.Equals(other.m_IconCount) && m_Level.Equals(other.m_Level) && m_PrefabIndex.Equals(other.m_PrefabIndex) && m_Layer == other.m_Layer && m_Flags == other.m_Flags && m_IsMoving == other.m_IsMoving)
			{
				return m_IsTemp == other.m_IsTemp;
			}
			return false;
		}
	}

	public struct ClusterIcon
	{
		private Entity m_Icon;

		private Entity m_Prefab;

		private float2 m_Order;

		private IconPriority m_Priority;

		private IconFlags m_Flags;

		public Entity icon => m_Icon;

		public Entity prefab => m_Prefab;

		public float2 order => m_Order;

		public IconPriority priority => m_Priority;

		public IconFlags flags => m_Flags;

		public ClusterIcon(Entity icon, Entity prefab, float2 order, IconPriority priority, IconFlags flags)
		{
			m_Icon = icon;
			m_Prefab = prefab;
			m_Order = order;
			m_Priority = priority;
			m_Flags = flags;
		}
	}

	private struct TempIconCluster : IComparable<TempIconCluster>
	{
		public float3 m_Center;

		public float3 m_Size;

		public Entity m_Icon;

		public Entity m_Prefab;

		public int2 m_SubClusters;

		public float m_Radius;

		public int m_FriendIndex;

		public int m_MovingGroup;

		public IconPriority m_Priority;

		public IconClusterLayer m_ClusterLayer;

		public IconFlags m_Flags;

		public TempIconCluster(Entity entity, Entity prefab, Icon icon, NotificationIconDisplayData displayData, int movingGroup)
		{
			m_Center = icon.m_Location;
			m_Size = default(float3);
			m_Icon = entity;
			m_Prefab = prefab;
			m_SubClusters = 0;
			m_Radius = math.lerp(displayData.m_MinParams.x, displayData.m_MaxParams.x, (float)(int)icon.m_Priority * 0.003921569f);
			m_FriendIndex = 0;
			m_MovingGroup = movingGroup;
			m_Priority = icon.m_Priority;
			m_ClusterLayer = icon.m_ClusterLayer;
			m_Flags = icon.m_Flags;
		}

		public TempIconCluster(in TempIconCluster cluster1, in TempIconCluster cluster2, int index1, int index2)
		{
			float3 @float = math.min(cluster1.m_Center - cluster1.m_Size, cluster2.m_Center - cluster2.m_Size) * 0.5f;
			float3 float2 = math.max(cluster1.m_Center + cluster1.m_Size, cluster2.m_Center + cluster2.m_Size) * 0.5f;
			m_Center = @float + float2;
			m_Size = float2 - @float;
			m_Icon = Entity.Null;
			m_Prefab = ((cluster1.m_Prefab == cluster2.m_Prefab) ? cluster1.m_Prefab : Entity.Null);
			m_SubClusters = new int2(index1, index2);
			m_Radius = math.max(cluster1.m_Radius, cluster2.m_Radius);
			m_FriendIndex = 0;
			m_MovingGroup = math.select(-1, cluster1.m_MovingGroup, cluster1.m_MovingGroup == cluster2.m_MovingGroup);
			m_Priority = (IconPriority)math.max((int)cluster1.m_Priority, (int)cluster2.m_Priority);
			m_ClusterLayer = cluster1.m_ClusterLayer;
			m_Flags = cluster1.m_Flags & cluster2.m_Flags;
		}

		public int CompareTo(TempIconCluster other)
		{
			return math.select(m_Icon.Index - other.m_Icon.Index, math.select(-1, 1, m_Center.x > other.m_Center.x), m_Center.x != other.m_Center.x);
		}
	}

	private struct TreeBounds : IEquatable<TreeBounds>, IBounds2<TreeBounds>
	{
		public Bounds3 m_Bounds;

		public ulong m_LevelMask;

		public ulong m_LayerMask;

		public void Reset()
		{
			m_Bounds = new Bounds3(float.MaxValue, float.MinValue);
			m_LevelMask = 0uL;
			m_LayerMask = 0uL;
		}

		public float2 Center()
		{
			return MathUtils.Center(m_Bounds.xz);
		}

		public float2 Size()
		{
			return MathUtils.Size(m_Bounds.xz);
		}

		public TreeBounds Merge(TreeBounds other)
		{
			return new TreeBounds
			{
				m_Bounds = (m_Bounds | other.m_Bounds),
				m_LevelMask = (m_LevelMask | other.m_LevelMask),
				m_LayerMask = (m_LayerMask | other.m_LayerMask)
			};
		}

		public bool Intersect(TreeBounds other)
		{
			if (MathUtils.Intersect(m_Bounds, other.m_Bounds) && (m_LevelMask & other.m_LevelMask) != 0L)
			{
				return (m_LayerMask & other.m_LayerMask) != 0;
			}
			return false;
		}

		public bool Equals(TreeBounds other)
		{
			if (m_Bounds.Equals(other.m_Bounds) && m_LevelMask == other.m_LevelMask)
			{
				return m_LayerMask == other.m_LayerMask;
			}
			return false;
		}
	}

	[BurstCompile]
	private struct IconChunkJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<DisallowCluster> m_DisallowClusterType;

		[ReadOnly]
		public ComponentTypeHandle<Animation> m_AnimationType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> m_IconDisplayData;

		public ComponentTypeHandle<Icon> m_IconType;

		public IconData m_IconData;

		public NativeList<UnsafeHashSet<int>> m_Orphans;

		public NativeList<TempIconCluster> m_TempBuffer;

		public void Execute()
		{
			NativeList<int> tempList = new NativeList<int>(100, Allocator.Temp);
			m_IconData.HandleOldRoots(m_Orphans, tempList);
			HandleChunks(m_Orphans, m_TempBuffer);
			tempList.Dispose();
		}

		public void HandleChunks(NativeList<UnsafeHashSet<int>> orphans, NativeList<TempIconCluster> tempBuffer)
		{
			bool flag = m_IconData.m_IconClusters.Length <= 1;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Icon> nativeArray = archetypeChunk.GetNativeArray(ref m_IconType);
				if (archetypeChunk.Has(ref m_DeletedType) || archetypeChunk.Has(ref m_DisallowClusterType) || archetypeChunk.Has(ref m_AnimationType))
				{
					if (archetypeChunk.Has(ref m_TempType))
					{
						continue;
					}
					for (int j = 0; j < nativeArray.Length; j++)
					{
						ref Icon reference = ref nativeArray.ElementAt(j);
						if (!flag)
						{
							m_IconData.Remove(reference.m_ClusterIndex, 0, -1, orphans);
						}
						reference.m_ClusterIndex = 0;
					}
					continue;
				}
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Owner> nativeArray3 = archetypeChunk.GetNativeArray(ref m_OwnerType);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				if (archetypeChunk.Has(ref m_TempType))
				{
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						Entity entity = nativeArray2[k];
						Icon icon = nativeArray[k];
						PrefabRef prefabRef = nativeArray4[k];
						if (m_IconDisplayData.IsComponentEnabled(prefabRef.m_Prefab))
						{
							NotificationIconDisplayData displayData = m_IconDisplayData[prefabRef.m_Prefab];
							int movingGroup = -1;
							if (CollectionUtils.TryGet(nativeArray3, k, out var value) && m_MovingData.HasComponent(value.m_Owner))
							{
								movingGroup = value.m_Owner.Index;
							}
							tempBuffer.Add(new TempIconCluster(entity, prefabRef.m_Prefab, icon, displayData, movingGroup));
						}
					}
					continue;
				}
				for (int l = 0; l < nativeArray2.Length; l++)
				{
					Entity icon2 = nativeArray2[l];
					PrefabRef prefabRef2 = nativeArray4[l];
					ref Icon reference2 = ref nativeArray.ElementAt(l);
					reference2.m_ClusterIndex = math.select(reference2.m_ClusterIndex, 0, flag);
					if (m_IconDisplayData.IsComponentEnabled(prefabRef2.m_Prefab))
					{
						NotificationIconDisplayData notificationIconDisplayData = m_IconDisplayData[prefabRef2.m_Prefab];
						if (reference2.m_ClusterIndex == 0)
						{
							reference2.m_ClusterIndex = m_IconData.GetNewClusterIndex();
						}
						float num = math.lerp(notificationIconDisplayData.m_MinParams.x, notificationIconDisplayData.m_MaxParams.x, (float)(int)reference2.m_Priority * 0.003921569f);
						int num2 = -1;
						if (CollectionUtils.TryGet(nativeArray3, l, out var value2) && m_MovingData.HasComponent(value2.m_Owner))
						{
							num2 = value2.m_Owner.Index;
						}
						ref IconCluster reference3 = ref m_IconData.m_IconClusters.ElementAt(reference2.m_ClusterIndex);
						int firstIcon;
						int iconCount;
						NativeHeapBlock iconAllocation = reference3.GetIcons(out firstIcon, out iconCount);
						if (iconCount == 0)
						{
							iconCount = 1;
							iconAllocation = m_IconData.AllocateIcons(iconCount);
							firstIcon = (int)iconAllocation.Begin;
						}
						for (int m = 0; m < iconCount; m++)
						{
							m_IconData.m_ClusterIcons.ElementAt(firstIcon + m) = new ClusterIcon(icon2, prefabRef2.m_Prefab, reference2.m_Location.xz, reference2.m_Priority, reference2.m_Flags);
						}
						IconCluster iconCluster = new IconCluster(reference2.m_Location, num, reference3.parentCluster, 0, num, 0f, iconAllocation, reference2.m_ClusterLayer, reference2.m_Flags, iconCount, 0, prefabRef2.m_Prefab.Index, num2 != -1, isTemp: false);
						if (!iconCluster.Equals(reference3))
						{
							m_IconData.Remove(reference3.parentCluster, reference2.m_ClusterIndex, -1, orphans);
							reference3 = iconCluster;
							IconCluster.SetParent(ref reference3, 0);
							m_IconData.AddOrphan(reference2.m_ClusterIndex, 0, orphans);
							m_IconData.m_ClusterTree.AddOrUpdate(reference2.m_ClusterIndex, new TreeBounds
							{
								m_Bounds = new Bounds3(reference3.center - reference3.size, reference3.center + reference3.size),
								m_LevelMask = 1uL,
								m_LayerMask = (ulong)(1L << (int)reference3.layer)
							});
						}
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct IconClusterJob : IJob
	{
		public IconData m_IconData;

		public NativeList<UnsafeHashSet<int>> m_Orphans;

		public NativeList<TempIconCluster> m_TempBuffer;

		public void Execute()
		{
			NativeQuadTreeSelectorBuffer<float> selectorBuffer = new NativeQuadTreeSelectorBuffer<float>(Allocator.Temp);
			NativeList<ClusterIcon> tempIcons = new NativeList<ClusterIcon>(10, Allocator.Temp);
			m_IconData.HandleOrphans(m_Orphans, selectorBuffer, tempIcons);
			m_IconData.HandleTemps(m_TempBuffer, tempIcons);
			selectorBuffer.Dispose();
			tempIcons.Dispose();
		}
	}

	private struct IconData
	{
		private struct Selector : INativeQuadTreeSelector<int, TreeBounds, float>, IUnsafeQuadTreeSelector<int, TreeBounds, float>
		{
			public float m_BestCost;

			public float m_BestDistance;

			public int m_BestClusterIndex;

			public int m_IgnoreClusterIndex;

			public ulong m_LevelMask;

			public ulong m_LayerMask;

			public IconCluster m_Cluster;

			public NativeList<IconCluster> m_IconClusters;

			public bool Check(TreeBounds bounds, out float priority)
			{
				priority = MathUtils.DistanceSquared(bounds.m_Bounds, m_Cluster.center);
				if (priority <= m_BestDistance && (bounds.m_LevelMask & m_LevelMask) != 0L)
				{
					return (bounds.m_LayerMask & m_LayerMask) != 0;
				}
				return false;
			}

			public bool Check(float priority)
			{
				return priority <= m_BestDistance;
			}

			public bool Better(float priority1, float priority2)
			{
				return priority1 < priority2;
			}

			public void Select(TreeBounds bounds, int item)
			{
				if (!(MathUtils.DistanceSquared(bounds.m_Bounds, m_Cluster.center) > m_BestDistance) && (bounds.m_LevelMask & m_LevelMask) != 0L && (bounds.m_LayerMask & m_LayerMask) != 0L && item != m_IgnoreClusterIndex)
				{
					IconCluster iconCluster = m_IconClusters[item];
					bool test = iconCluster.prefabIndex == m_Cluster.prefabIndex && ((iconCluster.flags | m_Cluster.flags) & IconFlags.Unique) == 0;
					float num = math.distancesq(iconCluster.center, m_Cluster.center);
					float num2 = math.select(num, num * 0.25f, test);
					if (num2 < m_BestCost || (num2 == m_BestCost && iconCluster.parentCluster == 0))
					{
						m_BestClusterIndex = item;
						m_BestCost = num2;
						m_BestDistance = math.select(num * 4.01f, num * 1.01f, test);
					}
				}
			}
		}

		public NativeQuadTree<int, TreeBounds> m_ClusterTree;

		public NativeHeapAllocator m_IconAllocator;

		public NativeList<IconCluster> m_IconClusters;

		public NativeList<ClusterIcon> m_ClusterIcons;

		public NativeList<int> m_RootClusters;

		public NativeList<int> m_FreeClusterIndices;

		public void ValidateClusters()
		{
			NativeList<int> nativeList = new NativeList<int>(64, Allocator.Temp);
			NativeHashSet<int> nativeHashSet = new NativeHashSet<int>(64, Allocator.Temp);
			for (int i = 0; i < m_RootClusters.Length; i++)
			{
				nativeList.Add(m_RootClusters[i]);
			}
			bool flag = false;
			while (nativeList.Length != 0)
			{
				if (!nativeHashSet.Add(nativeList[nativeList.Length - 1]))
				{
					flag = true;
					nativeList.Clear();
					nativeHashSet.Clear();
					break;
				}
				IconCluster iconCluster = m_IconClusters[nativeList[nativeList.Length - 1]];
				nativeList.RemoveAtSwapBack(nativeList.Length - 1);
				if (iconCluster.GetSubClusters(out var subClusters))
				{
					nativeList.Add(in subClusters.x);
					nativeList.Add(in subClusters.y);
				}
			}
			if (!flag)
			{
				return;
			}
			for (int j = 0; j < m_RootClusters.Length; j++)
			{
				nativeList.Add(m_RootClusters[j]);
			}
			while (nativeList.Length != 0)
			{
				IconCluster iconCluster2 = m_IconClusters[nativeList[nativeList.Length - 1]];
				iconCluster2.GetSubClusters(out var subClusters2);
				UnityEngine.Debug.Log($"{iconCluster2.level}: {nativeList[nativeList.Length - 1]} -> {subClusters2.x}, {subClusters2.y}");
				if (!nativeHashSet.Add(nativeList[nativeList.Length - 1]))
				{
					m_ClusterTree.Clear();
					m_IconAllocator.Clear();
					m_IconClusters.Clear();
					m_ClusterIcons.Clear();
					m_RootClusters.Clear();
					m_FreeClusterIndices.Clear();
					m_IconClusters.Add(default(IconCluster));
					m_ClusterIcons.ResizeUninitialized((int)m_IconAllocator.Size);
					break;
				}
				nativeList.RemoveAtSwapBack(nativeList.Length - 1);
				if (iconCluster2.GetSubClusters(out var subClusters3))
				{
					nativeList.Add(in subClusters3.x);
					nativeList.Add(in subClusters3.y);
				}
			}
		}

		public void HandleOldRoots(NativeList<UnsafeHashSet<int>> orphans, NativeList<int> tempList)
		{
			for (int i = 0; i < m_RootClusters.Length; i++)
			{
				int num = m_RootClusters[i];
				ref IconCluster reference = ref m_IconClusters.ElementAt(num);
				if (reference.isTemp)
				{
					RemoveTemp(num, tempList);
				}
				else
				{
					AddOrphan(num, reference.level, orphans);
				}
			}
			m_RootClusters.Clear();
		}

		public void HandleOrphans(NativeList<UnsafeHashSet<int>> orphans, NativeQuadTreeSelectorBuffer<float> selectorBuffer, NativeList<ClusterIcon> tempIcons)
		{
			int i = 0;
			UnsafeHashSet<int> value = default(UnsafeHashSet<int>);
			Selector selector = new Selector
			{
				m_IconClusters = m_IconClusters
			};
			for (; i < orphans.Length; i++)
			{
				UnsafeHashSet<int> unsafeHashSet = orphans[i];
				selector.m_LevelMask = (ulong)(1L << i);
				while (unsafeHashSet.IsCreated)
				{
					UnsafeHashSet<int> unsafeHashSet2 = unsafeHashSet;
					UnsafeHashSet<int>.Enumerator enumerator = unsafeHashSet2.GetEnumerator();
					orphans[i] = value;
					while (enumerator.MoveNext())
					{
						ref IconCluster reference = ref m_IconClusters.ElementAt(enumerator.Current);
						if (reference.parentCluster != 0)
						{
							continue;
						}
						if (i != 63)
						{
							selector.m_LayerMask = (ulong)(1L << (int)reference.layer);
							selector.m_BestCost = float.MaxValue;
							selector.m_BestDistance = float.MaxValue;
							selector.m_BestClusterIndex = 0;
							selector.m_IgnoreClusterIndex = enumerator.Current;
							selector.m_Cluster = reference;
							m_ClusterTree.Select(ref selector, selectorBuffer);
							if (selector.m_BestClusterIndex != 0)
							{
								float num = selector.m_BestCost;
								int num2 = selector.m_BestClusterIndex;
								ref IconCluster reference2 = ref m_IconClusters.ElementAt(num2);
								if (reference2.parentCluster == 0 || num < GetCurrentCost(num2, ref reference2))
								{
									selector.m_BestCost = float.MaxValue;
									selector.m_BestDistance = float.MaxValue;
									selector.m_BestClusterIndex = 0;
									selector.m_IgnoreClusterIndex = num2;
									selector.m_Cluster = reference2;
									m_ClusterTree.Select(ref selector, selectorBuffer);
									if (selector.m_BestClusterIndex == enumerator.Current || selector.m_BestCost == num)
									{
										if (reference2.parentCluster != 0)
										{
											Remove(reference2.parentCluster, num2, i, orphans);
										}
										int newClusterIndex = GetNewClusterIndex();
										reference = ref m_IconClusters.ElementAt(enumerator.Current);
										reference2 = ref m_IconClusters.ElementAt(num2);
										ref IconCluster reference3 = ref m_IconClusters.ElementAt(newClusterIndex);
										AddIcons(reference, reference2, tempIcons);
										NativeHeapBlock iconAllocation = AllocateIcons(tempIcons.Length);
										for (int j = 0; j < tempIcons.Length; j++)
										{
											m_ClusterIcons[(int)iconAllocation.Begin + j] = tempIcons[j];
										}
										reference3 = new IconCluster(reference, reference2, enumerator.Current, num2, iconAllocation, tempIcons.Length, i + 1);
										tempIcons.Clear();
										m_ClusterTree.AddOrUpdate(newClusterIndex, new TreeBounds
										{
											m_Bounds = new Bounds3(reference3.center - reference3.size, reference3.center + reference3.size),
											m_LevelMask = (ulong)(1L << i + 1),
											m_LayerMask = (ulong)(1L << (int)reference.layer)
										});
										UpdateLevelMask(num2, (ulong)((-1L << reference2.level) & ~(-1L << i + 1)));
										IconCluster.SetParent(ref reference, newClusterIndex);
										IconCluster.SetParent(ref reference2, newClusterIndex);
										AddOrphan(newClusterIndex, i + 1, orphans);
										continue;
									}
								}
								ulong num3 = (ulong)(-1L << reference.level);
								num3 = math.select(num3, num3 & (ulong)(~(-1L << i + 2)), i < 62);
								UpdateLevelMask(enumerator.Current, num3);
								AddOrphan(enumerator.Current, i + 1, orphans);
								continue;
							}
						}
						UpdateLevelMask(enumerator.Current, (ulong)(1L << reference.level));
						m_RootClusters.Add(enumerator.Current);
					}
					enumerator.Dispose();
					value = unsafeHashSet2;
					value.Clear();
					unsafeHashSet = orphans[i];
					if (unsafeHashSet.IsCreated && unsafeHashSet.IsEmpty)
					{
						unsafeHashSet.Dispose();
					}
				}
			}
			if (value.IsCreated)
			{
				value.Dispose();
			}
		}

		public void HandleTemps(NativeList<TempIconCluster> tempBuffer, NativeList<ClusterIcon> tempIcons)
		{
			int num = tempBuffer.Length;
			if (num > 1)
			{
				tempBuffer.Sort();
			}
			NativeArray<TempIconCluster> nativeArray = new NativeArray<TempIconCluster>(num, Allocator.Temp);
			NativeArray<TempIconCluster> a = tempBuffer.AsArray();
			NativeArray<TempIconCluster> b = nativeArray;
			while (num > 1)
			{
				for (int i = 0; i < num; i++)
				{
					ref TempIconCluster reference = ref a.ElementAt(i);
					reference.m_FriendIndex = i;
					float num2 = float.MaxValue;
					float num3 = float.MaxValue;
					bool flag = true;
					int num4 = i - 1;
					int num5 = i + 1;
					while (num4 >= 0 || num5 < num)
					{
						if (num4 >= 0)
						{
							ref TempIconCluster reference2 = ref a.ElementAt(num4);
							float num6 = reference.m_Center.x - reference2.m_Center.x;
							if (num6 * num6 > num3)
							{
								num4 = -1;
							}
							else
							{
								if (reference.m_ClusterLayer == reference2.m_ClusterLayer)
								{
									bool test = reference2.m_Prefab == reference.m_Prefab && ((reference2.m_Flags | reference.m_Flags) & IconFlags.Unique) == 0;
									float num7 = math.distancesq(reference2.m_Center, reference.m_Center);
									float num8 = math.select(num7, num7 * 0.25f, test);
									if (num8 < num2 || (num8 == num2 && reference2.m_FriendIndex == i))
									{
										reference.m_FriendIndex = num4;
										num2 = num8;
										num3 = math.select(num7 * 4.01f, num7 * 1.01f, test);
										flag = reference2.m_FriendIndex != i;
									}
								}
								num4--;
							}
						}
						if (num5 >= num)
						{
							continue;
						}
						ref TempIconCluster reference3 = ref a.ElementAt(num5);
						float num9 = reference3.m_Center.x - reference.m_Center.x;
						if (num9 * num9 > num3)
						{
							num5 = num;
							continue;
						}
						if (reference.m_ClusterLayer == reference3.m_ClusterLayer)
						{
							bool test2 = reference.m_Prefab == reference3.m_Prefab && ((reference.m_Flags | reference3.m_Flags) & IconFlags.Unique) == 0;
							float num10 = math.distancesq(reference.m_Center, reference3.m_Center);
							float num11 = math.select(num10, num10 * 0.25f, test2);
							if (num11 < num2 || (num11 == num2 && flag))
							{
								reference.m_FriendIndex = num5;
								num2 = num11;
								num3 = math.select(num10 * 4.01f, num10 * 1.01f, test2);
								flag = false;
							}
						}
						num5++;
					}
				}
				int num12 = 0;
				for (int j = 0; j < num; j++)
				{
					ref TempIconCluster reference4 = ref a.ElementAt(j);
					ref TempIconCluster reference5 = ref a.ElementAt(reference4.m_FriendIndex);
					if (reference5.m_FriendIndex == j)
					{
						if (reference4.m_FriendIndex < j)
						{
							int newClusterIndex = GetNewClusterIndex();
							int newClusterIndex2 = GetNewClusterIndex();
							b[num12++] = new TempIconCluster(in reference5, in reference4, newClusterIndex2, newClusterIndex);
							AddCluster(in reference5, newClusterIndex2, isRoot: false, tempIcons);
							AddCluster(in reference4, newClusterIndex, isRoot: false, tempIcons);
						}
						else if (reference4.m_FriendIndex == j)
						{
							AddCluster(in reference4, GetNewClusterIndex(), isRoot: true, tempIcons);
						}
					}
					else
					{
						b[num12++] = reference4;
					}
				}
				CommonUtils.Swap(ref a, ref b);
				if (num == num12)
				{
					break;
				}
				num = num12;
				if (num > 1)
				{
					a.GetSubArray(0, num).Sort();
				}
			}
			for (int k = 0; k < num; k++)
			{
				AddCluster(in a.ElementAt(k), GetNewClusterIndex(), isRoot: true, tempIcons);
			}
			nativeArray.Dispose();
		}

		private void AddCluster(in TempIconCluster tempCluster, int index, bool isRoot, NativeList<ClusterIcon> tempIcons)
		{
			int num2;
			NativeHeapBlock iconAllocation;
			float x;
			if (tempCluster.m_SubClusters.x != 0)
			{
				IconCluster subCluster = m_IconClusters[tempCluster.m_SubClusters.x];
				IconCluster subCluster2 = m_IconClusters[tempCluster.m_SubClusters.y];
				int num = AddIcons(subCluster, subCluster2, tempIcons);
				num2 = tempIcons.Length;
				iconAllocation = AllocateIcons(num2);
				for (int i = 0; i < tempIcons.Length; i++)
				{
					m_ClusterIcons[(int)iconAllocation.Begin + i] = tempIcons[i];
				}
				tempIcons.Clear();
				float num3 = math.distance(subCluster.center, subCluster2.center);
				float num4 = math.select(1f, 0.5f, num2 == num);
				x = math.max(subCluster.distanceFactor, subCluster2.distanceFactor);
				x = math.max(x, num3 / (tempCluster.m_Radius * num4));
			}
			else
			{
				iconAllocation = AllocateIcons(1);
				m_ClusterIcons[(int)iconAllocation.Begin] = new ClusterIcon(tempCluster.m_Icon, tempCluster.m_Prefab, tempCluster.m_Center.xz, tempCluster.m_Priority, tempCluster.m_Flags);
				num2 = 1;
				x = 0f;
			}
			m_IconClusters[index] = new IconCluster(tempCluster.m_Center, tempCluster.m_Size, -1, tempCluster.m_SubClusters, tempCluster.m_Radius, x, iconAllocation, tempCluster.m_ClusterLayer, tempCluster.m_Flags, num2, -1, -1, tempCluster.m_MovingGroup != -1, isTemp: true);
			if (isRoot)
			{
				m_RootClusters.Add(in index);
			}
		}

		private int AddIcons(IconCluster subCluster1, IconCluster subCluster2, NativeList<ClusterIcon> tempIcons)
		{
			int2 @int = default(int2);
			int2 int2 = default(int2);
			subCluster1.GetIcons(out @int.x, out int2.x);
			subCluster2.GetIcons(out @int.y, out int2.y);
			int result = math.csum(int2);
			int2 += @int;
			while (math.all(@int < int2))
			{
				ClusterIcon icon = m_ClusterIcons[@int.x];
				ClusterIcon icon2 = m_ClusterIcons[@int.y];
				if ((int)icon.priority >= (int)icon2.priority)
				{
					AddIcon(icon, tempIcons);
					@int.x++;
				}
				else
				{
					AddIcon(icon2, tempIcons);
					@int.y++;
				}
			}
			for (int i = @int.x; i < int2.x; i++)
			{
				AddIcon(m_ClusterIcons[i], tempIcons);
			}
			for (int j = @int.y; j < int2.y; j++)
			{
				AddIcon(m_ClusterIcons[j], tempIcons);
			}
			return result;
		}

		private void UpdateLevelMask(int clusterIndex, ulong levelMask)
		{
			if (m_ClusterTree.TryGet(clusterIndex, out var bounds) && bounds.m_LevelMask != levelMask)
			{
				bounds.m_LevelMask = levelMask;
				m_ClusterTree.Update(clusterIndex, bounds);
			}
		}

		public NativeHeapBlock AllocateIcons(int iconCount)
		{
			NativeHeapBlock result = m_IconAllocator.Allocate((uint)iconCount);
			while (result.Empty)
			{
				m_IconAllocator.Resize(m_IconAllocator.Size + 1024);
				m_ClusterIcons.ResizeUninitialized((int)m_IconAllocator.Size);
				result = m_IconAllocator.Allocate((uint)iconCount);
			}
			return result;
		}

		private void AddIcon(ClusterIcon icon, NativeList<ClusterIcon> icons)
		{
			if ((icon.flags & IconFlags.Unique) == 0)
			{
				for (int i = 0; i < icons.Length; i++)
				{
					ClusterIcon clusterIcon = icons[i];
					if (clusterIcon.prefab == icon.prefab && (clusterIcon.flags & IconFlags.Unique) == 0)
					{
						return;
					}
				}
			}
			icons.Add(in icon);
		}

		public int GetNewClusterIndex()
		{
			int num;
			if (m_FreeClusterIndices.Length != 0)
			{
				num = m_FreeClusterIndices[m_FreeClusterIndices.Length - 1];
				m_FreeClusterIndices.RemoveAt(m_FreeClusterIndices.Length - 1);
				m_IconClusters[num] = default(IconCluster);
			}
			else
			{
				num = m_IconClusters.Length;
				m_IconClusters.Add(default(IconCluster));
			}
			return num;
		}

		private float GetCurrentCost(int clusterIndex, ref IconCluster cluster)
		{
			m_IconClusters.ElementAt(cluster.parentCluster).GetSubClusters(out var subClusters);
			ref IconCluster reference = ref m_IconClusters.ElementAt(math.select(subClusters.x, subClusters.y, clusterIndex == subClusters.x));
			bool test = reference.prefabIndex == cluster.prefabIndex && ((reference.flags | cluster.flags) & IconFlags.Unique) == 0;
			float num = math.distancesq(reference.center, cluster.center);
			return math.select(num, num * 0.25f, test);
		}

		public void Remove(int clusterIndex, int subCluster, int subLevel, NativeList<UnsafeHashSet<int>> orphans)
		{
			while (clusterIndex != 0)
			{
				ref IconCluster reference = ref m_IconClusters.ElementAt(clusterIndex);
				m_ClusterTree.TryRemove(clusterIndex);
				RemoveOrphan(clusterIndex, reference.level, orphans);
				int firstIcon;
				int iconCount;
				NativeHeapBlock icons = reference.GetIcons(out firstIcon, out iconCount);
				if (!icons.Empty)
				{
					m_IconAllocator.Release(icons);
				}
				if (clusterIndex == m_IconClusters.Length - 1)
				{
					m_IconClusters.RemoveAt(clusterIndex);
				}
				else
				{
					m_FreeClusterIndices.Add(in clusterIndex);
				}
				if (reference.GetSubClusters(out var subClusters))
				{
					if (subClusters.x != subCluster)
					{
						ref IconCluster reference2 = ref m_IconClusters.ElementAt(subClusters.x);
						IconCluster.SetParent(ref reference2, 0);
						int num = math.max(reference2.level, subLevel);
						AddOrphan(subClusters.x, num, orphans);
						UpdateLevelMask(subClusters.x, (ulong)((-1L << reference2.level) & ~(-1L << num + 1)));
					}
					if (subClusters.y != subCluster)
					{
						ref IconCluster reference3 = ref m_IconClusters.ElementAt(subClusters.y);
						IconCluster.SetParent(ref reference3, 0);
						int num2 = math.max(reference3.level, subLevel);
						AddOrphan(subClusters.y, num2, orphans);
						UpdateLevelMask(subClusters.y, (ulong)((-1L << reference3.level) & ~(-1L << num2 + 1)));
					}
				}
				subCluster = clusterIndex;
				clusterIndex = reference.parentCluster;
			}
		}

		private void RemoveTemp(int clusterIndex, NativeList<int> tempList)
		{
			tempList.Add(in clusterIndex);
			while (!tempList.IsEmpty)
			{
				clusterIndex = tempList[tempList.Length - 1];
				tempList.RemoveAt(tempList.Length - 1);
				ref IconCluster reference = ref m_IconClusters.ElementAt(clusterIndex);
				int firstIcon;
				int iconCount;
				NativeHeapBlock icons = reference.GetIcons(out firstIcon, out iconCount);
				if (!icons.Empty)
				{
					m_IconAllocator.Release(icons);
				}
				if (clusterIndex == m_IconClusters.Length - 1)
				{
					m_IconClusters.RemoveAt(clusterIndex);
				}
				else
				{
					m_FreeClusterIndices.Add(in clusterIndex);
				}
				if (reference.GetSubClusters(out var subClusters))
				{
					tempList.Add(in subClusters.x);
					tempList.Add(in subClusters.y);
				}
			}
		}

		public void AddOrphan(int clusterIndex, int level, NativeList<UnsafeHashSet<int>> orphans)
		{
			if (orphans.Length <= level)
			{
				orphans.Length = level + 1;
			}
			ref UnsafeHashSet<int> reference = ref orphans.ElementAt(level);
			if (!reference.IsCreated)
			{
				reference = new UnsafeHashSet<int>(10, Allocator.TempJob);
			}
			reference.Add(clusterIndex);
		}

		private void RemoveOrphan(int clusterIndex, int level, NativeList<UnsafeHashSet<int>> orphans)
		{
			if (orphans.Length > level)
			{
				ref UnsafeHashSet<int> reference = ref orphans.ElementAt(level);
				if (reference.IsCreated)
				{
					reference.Remove(clusterIndex);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<DisallowCluster> __Game_Notifications_DisallowCluster_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Animation> __Game_Notifications_Animation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> __Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		public ComponentTypeHandle<Icon> __Game_Notifications_Icon_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Notifications_DisallowCluster_RO_ComponentTypeHandle = state.GetComponentTypeHandle<DisallowCluster>(isReadOnly: true);
			__Game_Notifications_Animation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Animation>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup = state.GetComponentLookup<NotificationIconDisplayData>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Notifications_Icon_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Icon>();
		}
	}

	private EntityQuery m_IconQuery;

	private EntityQuery m_ModifiedQuery;

	private EntityQuery m_ModifiedAndTempQuery;

	private NativeQuadTree<int, TreeBounds> m_ClusterTree;

	private NativeHeapAllocator m_IconAllocator;

	private NativeList<IconCluster> m_IconClusters;

	private NativeList<ClusterIcon> m_ClusterIcons;

	private NativeList<int> m_RootClusters;

	private NativeList<int> m_FreeClusterIndices;

	private JobHandle m_ClusterReadDeps;

	private JobHandle m_ClusterWriteDeps;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconQuery = GetEntityQuery(ComponentType.ReadOnly<Icon>());
		m_ModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Icon>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ModifiedAndTempQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Icon>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ClusterTree = new NativeQuadTree<int, TreeBounds>(1f, Allocator.Persistent);
		m_IconAllocator = new NativeHeapAllocator(1024u, 1u, Allocator.Persistent);
		m_IconClusters = new NativeList<IconCluster>(1024, Allocator.Persistent);
		m_ClusterIcons = new NativeList<ClusterIcon>(1024, Allocator.Persistent);
		m_RootClusters = new NativeList<int>(8, Allocator.Persistent);
		m_FreeClusterIndices = new NativeList<int>(128, Allocator.Persistent);
		m_IconClusters.Add(default(IconCluster));
		m_ClusterIcons.ResizeUninitialized((int)m_IconAllocator.Size);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_IconClusters.IsCreated)
		{
			m_ClusterReadDeps.Complete();
			m_ClusterWriteDeps.Complete();
			m_ClusterTree.Dispose();
			m_IconAllocator.Dispose();
			m_IconClusters.Dispose();
			m_ClusterIcons.Dispose();
			m_RootClusters.Dispose();
			m_FreeClusterIndices.Dispose();
		}
		base.OnDestroy();
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool loaded = GetLoaded();
		if (!(loaded ? m_IconQuery : m_ModifiedQuery).IsEmptyIgnoreFilter)
		{
			if (loaded)
			{
				ClearData();
			}
			NativeList<UnsafeHashSet<int>> orphans = new NativeList<UnsafeHashSet<int>>(64, Allocator.TempJob);
			NativeList<TempIconCluster> tempBuffer = new NativeList<TempIconCluster>(100, Allocator.TempJob);
			IconData iconData = new IconData
			{
				m_ClusterTree = m_ClusterTree,
				m_IconAllocator = m_IconAllocator,
				m_IconClusters = m_IconClusters,
				m_ClusterIcons = m_ClusterIcons,
				m_RootClusters = m_RootClusters,
				m_FreeClusterIndices = m_FreeClusterIndices
			};
			JobHandle outJobHandle;
			IconChunkJob jobData = new IconChunkJob
			{
				m_Chunks = (loaded ? m_IconQuery : m_ModifiedAndTempQuery).ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DisallowClusterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_DisallowCluster_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AnimationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Animation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_IconDisplayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IconType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Icon_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_IconData = iconData,
				m_Orphans = orphans,
				m_TempBuffer = tempBuffer
			};
			IconClusterJob jobData2 = new IconClusterJob
			{
				m_IconData = iconData,
				m_Orphans = orphans,
				m_TempBuffer = tempBuffer
			};
			JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(outJobHandle, m_ClusterReadDeps, m_ClusterWriteDeps));
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
			jobData.m_Chunks.Dispose(jobHandle);
			orphans.Dispose(jobHandle2);
			tempBuffer.Dispose(jobHandle2);
			m_ClusterWriteDeps = jobHandle2;
			m_ClusterReadDeps = default(JobHandle);
			base.Dependency = jobHandle;
		}
	}

	public void PreDeserialize(Context context)
	{
		ClearData();
		m_Loaded = true;
	}

	public ClusterData GetIconClusterData(bool readOnly, out JobHandle dependencies)
	{
		dependencies = (readOnly ? m_ClusterWriteDeps : JobHandle.CombineDependencies(m_ClusterReadDeps, m_ClusterWriteDeps));
		return new ClusterData(m_IconClusters, m_ClusterIcons, m_RootClusters);
	}

	public void AddIconClusterReader(JobHandle jobHandle)
	{
		m_ClusterReadDeps = JobHandle.CombineDependencies(m_ClusterReadDeps, jobHandle);
	}

	public void AddIconClusterWriter(JobHandle jobHandle)
	{
		m_ClusterWriteDeps = jobHandle;
	}

	public void RecalculateClusters()
	{
		m_Loaded = true;
	}

	private void ClearData()
	{
		if (m_IconClusters.IsCreated)
		{
			m_ClusterReadDeps.Complete();
			m_ClusterWriteDeps.Complete();
			m_ClusterReadDeps = default(JobHandle);
			m_ClusterWriteDeps = default(JobHandle);
			m_ClusterTree.Clear();
			m_IconAllocator.Clear();
			m_IconClusters.Clear();
			m_ClusterIcons.Clear();
			m_RootClusters.Clear();
			m_FreeClusterIndices.Clear();
			m_IconClusters.Add(default(IconCluster));
			m_ClusterIcons.ResizeUninitialized((int)m_IconAllocator.Size);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public IconClusterSystem()
	{
	}
}
