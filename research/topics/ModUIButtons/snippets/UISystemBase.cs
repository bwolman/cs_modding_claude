// Decompiled from: Game.dll -> Game.UI.UISystemBase
// Base class for all mod UI systems. Extends GameSystemBase (ECS system).
// Provides AddBinding/AddUpdateBinding for registering C#<->JS bindings.

using System.Collections.Generic;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI;
using Colossal.UI.Binding;
using Game.SceneFlow;
using UnityEngine.Scripting;

namespace Game.UI;

public abstract class UISystemBase : GameSystemBase
{
	protected static ILog log = UIManager.log;

	private List<IBinding> m_Bindings;

	private List<IUpdateBinding> m_UpdateBindings;

	/// <summary>
	/// Controls which GameModes this system is active in.
	/// Override to restrict (e.g., GameMode.Game for gameplay-only UI).
	/// Default: GameMode.All.
	/// </summary>
	public virtual GameMode gameMode => GameMode.All;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Bindings = new List<IBinding>();
		m_UpdateBindings = new List<IUpdateBinding>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		// Auto-cleanup: removes all bindings from the global registry
		foreach (IBinding binding in m_Bindings)
		{
			GameManager.instance.userInterface.bindings.RemoveBinding(binding);
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		// Polls all IUpdateBinding instances (GetterValueBinding, RawValueBinding)
		foreach (IUpdateBinding updateBinding in m_UpdateBindings)
		{
			updateBinding.Update();
		}
	}

	/// <summary>
	/// Register a binding (ValueBinding, TriggerBinding, CallBinding, etc.)
	/// with the global binding registry.
	/// </summary>
	protected void AddBinding(IBinding binding)
	{
		m_Bindings.Add(binding);
		GameManager.instance.userInterface.bindings.AddBinding(binding);
	}

	/// <summary>
	/// Register a binding that needs per-frame polling (GetterValueBinding, RawValueBinding).
	/// Also adds to the regular binding list via AddBinding.
	/// </summary>
	protected void AddUpdateBinding(IUpdateBinding binding)
	{
		AddBinding(binding);
		m_UpdateBindings.Add(binding);
	}

	/// <summary>
	/// Called on game mode transitions. Auto-disables the system
	/// if the current mode doesn't match this system's gameMode mask.
	/// </summary>
	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = (gameMode & mode) != 0;
	}

	[Preserve]
	protected UISystemBase()
	{
	}
}
