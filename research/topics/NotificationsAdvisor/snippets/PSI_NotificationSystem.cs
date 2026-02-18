using System;
using Colossal.PSI.Common;
using Game.UI.Localization;
using Game.UI.Menu;

namespace Game.PSI;

public static class NotificationSystem
{
	private static NotificationUISystem s_System;

	public static void BindUI(NotificationUISystem value)
	{
		s_System = value;
	}

	public static void UnbindUI()
	{
		s_System = null;
	}

	public static void Push(string identifier, LocalizedString? title = null, LocalizedString? text = null, string titleId = null, string textId = null, string thumbnail = null, ProgressState? progressState = null, int? progress = null, Action onClicked = null)
	{
		s_System?.AddOrUpdateNotification(identifier, title ?? ((LocalizedString)NotificationUISystem.GetTitle(titleId)), text ?? ((LocalizedString)NotificationUISystem.GetText(textId)), thumbnail, progressState, progress, onClicked);
	}

	public static void Pop(string identifier, float delay = 0f, LocalizedString? title = null, LocalizedString? text = null, string titleId = null, string textId = null, string thumbnail = null, ProgressState? progressState = null, int? progress = null, Action onClicked = null)
	{
		s_System?.RemoveNotification(identifier, delay, title ?? ((LocalizedString)NotificationUISystem.GetTitle(titleId)), text ?? ((LocalizedString)NotificationUISystem.GetText(textId)), thumbnail, progressState, progress, onClicked);
	}

	public static bool Exist(string identifier)
	{
		return s_System?.NotificationExists(identifier) ?? false;
	}
}
