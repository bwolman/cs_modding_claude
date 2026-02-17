using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Colossal;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Game.Assets;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;
using Game.UI.Menu;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game;

[CompilerGenerated]
public class AutoSaveSystem : GameSystemBase
{
	private float m_LastAutoSaveCheck = -1f;

	private float timeSinceStartup => UnityEngine.Time.realtimeSinceStartup;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		SharedSettings.instance.general.onSettingsApplied += OnSettingsChanged;
	}

	private void OnSettingsChanged(Setting setting)
	{
		if (!GameManager.instance.gameMode.IsGame() || !(setting is GeneralSettings generalSettings))
		{
			return;
		}
		if (generalSettings.autoSave)
		{
			if (m_LastAutoSaveCheck < 0f)
			{
				PruneAutoSaves(generalSettings);
				m_LastAutoSaveCheck = timeSinceStartup;
				COSystemBase.baseLog.Debug("Auto-save watch active!");
			}
		}
		else if (m_LastAutoSaveCheck >= 0f)
		{
			PruneAutoSaves(generalSettings);
			m_LastAutoSaveCheck = -1f;
			COSystemBase.baseLog.Debug("Auto-save watch inactive!");
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		SharedSettings.instance.general.onSettingsApplied -= OnSettingsChanged;
		base.OnDestroy();
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		if (SharedSettings.instance.general.autoSave)
		{
			COSystemBase.baseLog.Debug("Auto-save watch inactive!");
			m_LastAutoSaveCheck = -1f;
		}
	}

	protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
	{
		if ((purpose == Purpose.LoadGame || purpose == Purpose.NewGame) && SharedSettings.instance.general.autoSave)
		{
			COSystemBase.baseLog.Debug("Auto-save watch active!");
			m_LastAutoSaveCheck = timeSinceStartup;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_LastAutoSaveCheck >= 0f && GameManager.instance.gameMode.IsGame())
		{
			GeneralSettings general = SharedSettings.instance.general;
			if (general.autoSave)
			{
				CheckAutoSave(general);
			}
		}
	}

	private async void CheckAutoSave(GeneralSettings settings)
	{
		if (timeSinceStartup - m_LastAutoSaveCheck > (float)settings.autoSaveInterval)
		{
			COSystemBase.baseLog.DebugFormat("Auto-save triggered after {0}s", m_LastAutoSaveCheck);
			m_LastAutoSaveCheck = timeSinceStartup;
			await PerformAutoSave(settings);
		}
	}

	private void PruneAutoSaves(GeneralSettings settings)
	{
		if (settings.autoSaveCount == GeneralSettings.AutoSaveCount.Unlimited)
		{
			return;
		}
		try
		{
			List<SaveGameMetadata> source = (from s in GetAutoSaveDatabaseTarget().GetAssets(default(SearchFilter<SaveGameMetadata>))
				where s.target.autoSave
				orderby s.target.lastModified descending
				select s).ToList();
			int autoSaveCount = (int)settings.autoSaveCount;
			foreach (SaveGameMetadata item in source.Skip(autoSaveCount))
			{
				SaveHelpers.DeleteSaveGame(item);
			}
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception, "An error occurred while pruning auto-saves");
		}
	}

	public async Task PerformAutoSave(GeneralSettings settings)
	{
		await SafeAutoSave();
		PruneAutoSaves(settings);
	}

	private static Task SafeAutoSave()
	{
		return TaskManager.instance.EnqueueTask("SaveLoadGame", AutoSave, 1);
	}

	private static async Task AutoSave()
	{
		RenderTexture preview = ScreenCaptureHelper.CreateRenderTarget("PreviewSaveGame-Auto", 680, 383);
		ScreenCaptureHelper.CaptureScreenshot(Camera.main, preview, new MenuHelpers.SaveGamePreviewSettings());
		MenuUISystem existingSystemManaged = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MenuUISystem>();
		string text = $"{DateTime.Now:dd-MMMM-HH-mm-ss}";
		COSystemBase.baseLog.InfoFormat("Auto-saving {0}...", text);
		try
		{
			ILocalAssetDatabase autoSaveDatabaseTarget = GetAutoSaveDatabaseTarget();
			if (autoSaveDatabaseTarget.Exists<PackageAsset>(SaveHelpers.GetAssetDataPath<SaveGameMetadata>(autoSaveDatabaseTarget, text), out var asset))
			{
				autoSaveDatabaseTarget.DeleteAsset(asset);
			}
			await GameManager.instance.Save(text, existingSystemManaged.GetSaveInfo(autoSave: true), autoSaveDatabaseTarget, preview);
		}
		catch (Exception exception)
		{
			COSystemBase.baseLog.Error(exception);
		}
		finally
		{
			CoreUtils.Destroy(preview);
		}
	}

	private static ILocalAssetDatabase GetAutoSaveDatabaseTarget()
	{
		return AssetDatabase.user;
	}

	[Preserve]
	public AutoSaveSystem()
	{
	}
}
