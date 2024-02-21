﻿/*
 * Copyright (c) 2023 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Linq;
using System.Text;
using SafeExamBrowser.Browser.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Monitoring.Contracts.Applications;
using SafeExamBrowser.WindowsApi.Contracts.Events;

namespace SafeExamBrowser.Proctoring.ScreenProctoring.Data
{
	internal class MetaDataAggregator
	{
		private readonly IApplicationMonitor applicationMonitor;
		private readonly IBrowserApplication browser;
		private readonly ILogger logger;

		private string applicationInfo;
		private string browserInfo;
		private TimeSpan elapsed;
		private string triggerInfo;
		private string urls;
		private string windowTitle;

		internal MetaData Data => new MetaData
		{
			ApplicationInfo = applicationInfo,
			BrowserInfo = browserInfo,
			Elapsed = elapsed,
			TriggerInfo = triggerInfo,
			Urls = urls,
			WindowTitle = windowTitle
		};

		internal MetaDataAggregator(IApplicationMonitor applicationMonitor, IBrowserApplication browser, TimeSpan elapsed, ILogger logger)
		{
			this.applicationMonitor = applicationMonitor;
			this.browser = browser;
			this.elapsed = elapsed;
			this.logger = logger;
		}

		internal void Capture(IntervalTrigger interval = default, KeyboardTrigger keyboard = default, MouseTrigger mouse = default)
		{
			CaptureApplicationData();
			CaptureBrowserData();

			if (interval != default)
			{
				CaptureIntervalTrigger(interval);
			}
			else if (keyboard != default)
			{
				CaptureKeyboardTrigger(keyboard);
			}
			else if (mouse != default)
			{
				CaptureMouseTrigger(mouse);
			}

			// TODO: Can only log URLs when allowed by policy in browser configuration!
			logger.Debug($"Captured metadata: {applicationInfo} / {browserInfo} / {triggerInfo} / {urls} / {windowTitle}.");
		}

		private void CaptureApplicationData()
		{
			if (applicationMonitor.TryGetActiveApplication(out var application))
			{
				applicationInfo = BuildApplicationInfo(application);
				windowTitle = string.IsNullOrEmpty(application.Window.Title) ? "-" : application.Window.Title;
			}
			else
			{
				applicationInfo = "-";
				windowTitle = "-";
			}
		}

		private void CaptureBrowserData()
		{
			var windows = browser.GetWindows();

			browserInfo = string.Join(", ", windows.Select(w => $"{(w.IsMainWindow ? "Main" : "Additional")} Window: {w.Title} ({w.Url})"));
			urls = string.Join(", ", windows.Select(w => w.Url));
		}

		private void CaptureIntervalTrigger(IntervalTrigger interval)
		{
			triggerInfo = $"Maximum interval of {interval.ConfigurationValue}ms has been reached.";
		}

		private void CaptureKeyboardTrigger(KeyboardTrigger keyboard)
		{
			var flags = Enum.GetValues(typeof(KeyModifier)).OfType<KeyModifier>().Where(m => m != KeyModifier.None && keyboard.Modifier.HasFlag(m));
			var modifiers = flags.Any() ? string.Join(" + ", flags) + " + " : string.Empty;

			triggerInfo = $"'{modifiers}{keyboard.Key}' has been {keyboard.State.ToString().ToLower()}.";
		}

		private void CaptureMouseTrigger(MouseTrigger mouse)
		{
			if (mouse.Info.IsTouch)
			{
				triggerInfo = $"Tap as {mouse.Button} mouse button has been {mouse.State.ToString().ToLower()} at ({mouse.Info.X}/{mouse.Info.Y}).";
			}
			else
			{
				triggerInfo = $"{mouse.Button} mouse button has been {mouse.State.ToString().ToLower()} at ({mouse.Info.X}/{mouse.Info.Y}).";
			}
		}

		private string BuildApplicationInfo(ActiveApplication application)
		{
			var info = new StringBuilder();

			info.Append(application.Process.Name);

			if (application.Process.OriginalName != default)
			{
				info.Append($" ({application.Process.OriginalName}{(application.Process.Signature == default ? ")" : "")}");
			}

			if (application.Process.Signature != default)
			{
				info.Append($"{(application.Process.OriginalName == default ? "(" : ", ")}{application.Process.Signature})");
			}

			return info.ToString();
		}
	}
}
