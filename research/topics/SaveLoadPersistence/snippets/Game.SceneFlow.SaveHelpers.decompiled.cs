using Colossal.IO.AssetDatabase;
using Colossal.PSI.Environment;
using Game.Assets;
using Game.PSI.PdxSdk;
using Game.Settings;

namespace Game.SceneFlow;

public static class SaveHelpers
{
	public const string kSaveLoadTaskName = "SaveLoadGame";

	public static AssetDataPath GetAssetDataPath<T>(ILocalAssetDatabase database, string saveName)
	{
		AssetDataPath result = saveName;
		if (!database.dataSource.isRemoteStorageSource)
		{
			string specialPath = EnvPath.GetSpecialPath<T>();
			if (specialPath != null)
			{
				result = AssetDataPath.Create(specialPath, saveName);
			}
		}
		return result;
	}

	public static void DeleteSaveGame(SaveGameMetadata saveGameMetadata)
	{
		UserState userState = GameManager.instance.settings.userState;
		if (userState.lastSaveGameMetadata == saveGameMetadata)
		{
			userState.lastSaveGameMetadata = null;
			userState.ApplyAndSave();
			Launcher.DeleteLastSaveMetadata();
		}
		AssetDatabase.global.DeleteAsset(saveGameMetadata);
	}
}
