using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.PSI;
using Game.UI.Localization;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Menu;

[CompilerGenerated]
public class NotificationUISystem : UISystemBase
{
	private class DelayedNotificationInfo
	{
		private NotificationInfo m_Notification;

		private float m_Delay;

		public DelayedNotificationInfo(NotificationInfo notification, float delay)
		{
			m_Notification = notification;
			m_Delay = delay;
		}

		public void Reset(float delay)
		{
			m_Delay = delay;
		}

		public bool Update(float deltaTime, out NotificationInfo notification)
		{
			notification = null;
			m_Delay -= deltaTime;
			if (m_Delay <= 0f)
			{
				notification = m_Notification;
				return true;
			}
			return false;
		}
	}

	public class NotificationInfo : IJsonWritable
	{
		public readonly string id;

		[CanBeNull]
		public string thumbnail;

		[CanBeNull]
		public LocalizedString? title;

		[CanBeNull]
		public LocalizedString? text;

		public ProgressState progressState;

		public int progress;

		public Action onClicked;

		public NotificationInfo(string id)
		{
			this.id = id;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("id");
			writer.Write(id);
			writer.PropertyName("thumbnail");
			writer.Write(thumbnail);
			writer.PropertyName("title");
			writer.Write(title);
			writer.PropertyName("text");
			writer.Write(text);
			writer.PropertyName("progressState");
			writer.Write((int)progressState);
			writer.PropertyName("progress");
			writer.Write(progress);
			writer.TypeEnd();
		}
	}

	private const string kGroup = "notification";

	private const string kInstallation = "installation";

	private const string kDownloading = "downloading";

	private const float kDelay = 2f;

	private const string kInstallCompleteText = "InstallComplete";

	private const string kInstallFailedText = "InstallFailed";

	private const string kInstallCancelledText = "InstallCancelled";

	private const string kDownloadPendingText = "DownloadPending";

	private const string kDownloadFailedText = "DownloadFailed";

	private const string kDownloadCompleteText = "DownloadComplete";

	private const string kDownloadProgressingText = "DownloadProgressing";

	private const string kDownloadCancelledText = "DownloadCancelled";

	private const string kValidationProgressingText = "ValidationProgressing";

	private ValueBinding<List<NotificationInfo>> m_NotificationsBinding;

	private Dictionary<string, DelayedNotificationInfo> m_PendingRemoval;

	private Dictionary<string, NotificationInfo> m_NotificationsMap;

	private Dictionary<int, Mod> m_ModInfoCache;

	private bool m_Dirty;

	public static int width
	{
		get
		{
			float num = (float)Screen.width / 1920f;
			return Mathf.CeilToInt(48f * num);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		NotificationSystem.BindUI(this);
		base.OnCreate();
		m_NotificationsMap = new Dictionary<string, NotificationInfo>();
		m_PendingRemoval = new Dictionary<string, DelayedNotificationInfo>();
		m_ModInfoCache = new Dictionary<int, Mod>();
		AddBinding(m_NotificationsBinding = new ValueBinding<List<NotificationInfo>>("notification", "notifications", new List<NotificationInfo>(), new ListWriter<NotificationInfo>(new ValueWriter<NotificationInfo>())));
		AddBinding(new TriggerBinding<string>("notification", "selectNotification", SelectNotification));
		PlatformManager.instance.onModSubscriptionChanged += HandleModSubscription;
		PlatformManager.instance.onModDownloadStarted += HandleModDownloadStarted;
		PlatformManager.instance.onModDownloadCompleted += HandleModDownloadCompleted;
		PlatformManager.instance.onModDownloadFailed += HandleModDownloadFailed;
		PlatformManager.instance.onModDownloadCancelled += HandleModDownloadCancelled;
		PlatformManager.instance.onModInstallProgress += HandleModInstallProgress;
		PlatformManager.instance.onTransferOnGoing += HandleTransferOnGoing;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		NotificationSystem.UnbindUI();
	}

	public static string GetTitle(string titleId)
	{
		return "Menu.NOTIFICATION_TITLE[" + titleId + "]";
	}

	public static string GetText(string textId)
	{
		return "Menu.NOTIFICATION_DESCRIPTION[" + textId + "]";
	}

	private void AddModNotification(Mod mod, string notificationId = null)
	{
		string identifier = notificationId ?? GetModNotificationId(mod);
		LocalizedString? title = LocalizedString.Value(mod.displayName);
		string thumbnail = $"{mod.thumbnailPath}?width={width})";
		Action onClick = mod.onClick;
		AddOrUpdateNotification(identifier, title, null, thumbnail, null, null, onClick);
	}

	private string GetModNotificationId(Mod mod, string suffix = null)
	{
		return GetModNotificationId(mod.id.ToString(), suffix);
	}

	private string GetModNotificationId(string modId, string suffix = null)
	{
		if (!string.IsNullOrEmpty(suffix))
		{
			return modId + "." + suffix;
		}
		return modId;
	}

	private void HandleModSubscription(IModSupport psi, Mod mod, ModSubscriptionStatus status)
	{
		string text = status.ToString();
		string modNotificationId = GetModNotificationId(mod, text);
		LocalizedString? title = LocalizedString.Value(mod.displayName);
		LocalizedString? text2 = GetText(text);
		string thumbnail = $"{mod.thumbnailPath}?width={width})";
		Action onClick = mod.onClick;
		RemoveNotification(modNotificationId, 2f, title, text2, thumbnail, null, null, onClick);
	}

	private void HandleModDownloadStarted(IModSupport psi, Mod mod)
	{
		m_ModInfoCache[mod.id] = mod;
		string modNotificationId = GetModNotificationId(mod, "installation");
		AddModNotification(mod, modNotificationId);
		LocalizedString? text = GetText("DownloadPending");
		ProgressState? progressState = ProgressState.Indeterminate;
		int? progress = 0;
		AddOrUpdateNotification(modNotificationId, null, text, null, progressState, progress);
	}

	private void HandleModDownloadCompleted(IModSupport psi, Mod mod)
	{
		m_ModInfoCache.Remove(mod.id);
		string modNotificationId = GetModNotificationId(mod, "installation");
		LocalizedString? text = GetText("InstallComplete");
		ProgressState? progressState = ProgressState.Complete;
		int? progress = 100;
		RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
	}

	private void HandleModDownloadFailed(IModSupport psi, Mod mod)
	{
		m_ModInfoCache.Remove(mod.id);
		string modNotificationId = GetModNotificationId(mod, "installation");
		LocalizedString? text = GetText("InstallFailed");
		ProgressState? progressState = ProgressState.Failed;
		int? progress = 100;
		RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
	}

	private void HandleModDownloadCancelled(IModSupport psi, Mod mod)
	{
		string modNotificationId = GetModNotificationId(mod, "installation");
		if (NotificationExists(modNotificationId))
		{
			LocalizedString? text = GetText("InstallCancelled");
			ProgressState? progressState = ProgressState.Failed;
			int? progress = 100;
			RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
		}
		string modNotificationId2 = GetModNotificationId(mod, "downloading");
		if (NotificationExists(modNotificationId2))
		{
			LocalizedString? text = GetText("DownloadCancelled");
			ProgressState? progressState = ProgressState.Failed;
			int? progress = 100;
			RemoveNotification(modNotificationId2, 2f, null, text, null, progressState, progress);
		}
		m_ModInfoCache.Remove(mod.id);
	}

	private void HandleModInstallProgress(IModSupport psi, int modId, TransferStatus status)
	{
		switch (status.type)
		{
		case TransferType.Install:
		{
			string modNotificationId2 = GetModNotificationId(status.id, "installation");
			LocalizedString? text = GetText($"{TransferType.Install}{status.progressState}");
			ProgressState? progressState = status.progressState;
			int? progress = Mathf.CeilToInt(status.progress * 100f);
			AddOrUpdateNotification(modNotificationId2, null, text, null, progressState, progress);
			break;
		}
		case TransferType.Download:
		{
			string modNotificationId = GetModNotificationId(status.id, "downloading");
			switch (status.progressState)
			{
			case ProgressState.Progressing:
				if (!NotificationExists(modNotificationId))
				{
					if (!m_ModInfoCache.TryGetValue(modId, out var value))
					{
						value = new Mod
						{
							id = modId
						};
					}
					AddModNotification(value, modNotificationId);
					LocalizedString? text = GetText("DownloadPending");
					ProgressState? progressState = ProgressState.Indeterminate;
					int? progress = 0;
					AddOrUpdateNotification(modNotificationId, null, text, null, progressState, progress);
				}
				break;
			case ProgressState.Complete:
				if (NotificationExists(modNotificationId))
				{
					LocalizedString? text = GetText("DownloadComplete");
					ProgressState? progressState = ProgressState.Complete;
					int? progress = 100;
					RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
				}
				break;
			case ProgressState.Failed:
				if (NotificationExists(modNotificationId))
				{
					LocalizedString? text = GetText("DownloadFailed");
					ProgressState? progressState = ProgressState.Failed;
					int? progress = 100;
					RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
				}
				break;
			case ProgressState.Cancelled:
				if (NotificationExists(modNotificationId))
				{
					LocalizedString? text = GetText("DownloadCancelled");
					ProgressState? progressState = ProgressState.Failed;
					int? progress = 100;
					RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
				}
				break;
			case ProgressState.Indeterminate:
				break;
			}
			break;
		}
		}
	}

	private void HandleTransferOnGoing(ITransferSupport psi, TransferStatus status)
	{
		string modNotificationId = GetModNotificationId(status.id, "downloading");
		if (NotificationExists(modNotificationId))
		{
			if (status.progressState == ProgressState.Complete)
			{
				LocalizedString? text = GetText("DownloadComplete");
				ProgressState? progressState = ProgressState.Complete;
				int? progress = 100;
				RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
			}
			else if (status.progressState == ProgressState.Failed)
			{
				LocalizedString? text = GetText("DownloadFailed");
				ProgressState? progressState = ProgressState.Failed;
				int? progress = 100;
				RemoveNotification(modNotificationId, 2f, null, text, null, progressState, progress);
			}
			else if (status.progress < 1f)
			{
				LocalizedString? text = GetText("DownloadProgressing");
				ProgressState? progressState = status.progressState;
				int? progress = Mathf.CeilToInt(status.progress * 100f);
				AddOrUpdateNotification(modNotificationId, null, text, null, progressState, progress);
			}
			else
			{
				LocalizedString? text = GetText("ValidationProgressing");
				ProgressState? progressState = ProgressState.Indeterminate;
				int? progress = Mathf.CeilToInt(status.progress * 100f);
				AddOrUpdateNotification(modNotificationId, null, text, null, progressState, progress);
			}
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		ProcessPendingRemovals(base.CheckedStateRef.WorldUnmanaged.Time.DeltaTime);
		if (m_Dirty)
		{
			m_Dirty = false;
			m_NotificationsBinding.TriggerUpdate();
		}
	}

	private void SelectNotification(string notificationId)
	{
		if (m_NotificationsMap.TryGetValue(notificationId, out var value))
		{
			value.onClicked?.Invoke();
		}
	}

	private void UpdateNotification(NotificationInfo notificationInfo, LocalizedString? title, LocalizedString? text, string thumbnail, ProgressState? progressState, int? progress, Action onClicked)
	{
		if (title.HasValue && !notificationInfo.title.HasValue)
		{
			notificationInfo.title = title;
		}
		if (text.HasValue)
		{
			notificationInfo.text = text;
		}
		if (thumbnail != null && notificationInfo.thumbnail == null)
		{
			notificationInfo.thumbnail = thumbnail;
		}
		if (progressState.HasValue)
		{
			notificationInfo.progressState = progressState.Value;
		}
		if (progress.HasValue)
		{
			notificationInfo.progress = progress.Value;
		}
		if (onClicked != null && notificationInfo.onClicked == null)
		{
			notificationInfo.onClicked = onClicked;
		}
	}

	public NotificationInfo AddOrUpdateNotification(string identifier, LocalizedString? title = null, LocalizedString? text = null, string thumbnail = null, ProgressState? progressState = null, int? progress = null, Action onClicked = null)
	{
		if (m_NotificationsMap.TryGetValue(identifier, out var value))
		{
			UpdateNotification(value, title, text, thumbnail, progressState, progress, onClicked);
		}
		else
		{
			value = new NotificationInfo(identifier);
			UpdateNotification(value, title, text, thumbnail, progressState, progress, onClicked);
			m_NotificationsMap.Add(identifier, value);
			m_NotificationsBinding.value.Add(value);
		}
		m_Dirty = true;
		return value;
	}

	public void RemoveNotification(string identifier, float delay = 0f, LocalizedString? title = null, LocalizedString? text = null, string thumbnail = null, ProgressState? progressState = null, int? progress = null, Action onClicked = null)
	{
		NotificationInfo notificationInfo = AddOrUpdateNotification(identifier, title, text, thumbnail, progressState, progress, onClicked);
		DelayedNotificationInfo value;
		if (delay == 0f)
		{
			m_NotificationsBinding.value.Remove(notificationInfo);
			m_NotificationsMap.Remove(notificationInfo.id);
		}
		else if (m_PendingRemoval.TryGetValue(identifier, out value))
		{
			value.Reset(delay);
		}
		else
		{
			m_PendingRemoval.Add(identifier, new DelayedNotificationInfo(notificationInfo, delay));
		}
		m_Dirty = true;
	}

	public bool NotificationExists(string identifier)
	{
		return m_NotificationsMap.ContainsKey(identifier);
	}

	private void ProcessPendingRemovals(float deltaTime)
	{
		List<NotificationInfo> list = null;
		foreach (KeyValuePair<string, DelayedNotificationInfo> item in m_PendingRemoval)
		{
			if (item.Value.Update(deltaTime, out var notification))
			{
				list = new List<NotificationInfo> { notification };
				m_Dirty = true;
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (NotificationInfo item2 in list)
		{
			m_NotificationsBinding.value.Remove(item2);
			m_NotificationsMap.Remove(item2.id);
			m_PendingRemoval.Remove(item2.id);
		}
	}

	[Preserve]
	public NotificationUISystem()
	{
	}
}
