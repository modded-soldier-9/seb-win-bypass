﻿/*
 * Copyright (c) 2024 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using SafeExamBrowser.Settings;
using SafeExamBrowser.Settings.Applications;
using SafeExamBrowser.Settings.Security;

namespace SafeExamBrowser.Configuration.ConfigurationData
{
	internal class DataProcessor
	{
		internal void Process(IDictionary<string, object> rawData, AppSettings settings)
		{
			AllowBrowserToolbarForReloading(settings);
			CalculateConfigurationKey(rawData, settings);
			InitializeBrowserHomeFunctionality(settings);
			InitializeClipboardSettings(settings);
			InitializeProctoringSettings(settings);
			RemoveLegacyBrowsers(settings);

			settings.Applications.Blacklist.Clear();
			settings.Applications.Whitelist.Add(new WhitelistApplication { ExecutableName = "mspaint.exe", ShowInShell = true });
			settings.Browser.AdditionalWindow.AllowAddressBar = true;
			settings.Browser.MainWindow.AllowAddressBar = true;
			settings.Browser.MainWindow.AllowDeveloperConsole = true;
			settings.Browser.MainWindow.AllowBackwardNavigation = true;
			settings.Browser.MainWindow.AllowForwardNavigation = true;
			settings.Browser.MainWindow.ShowHomeButton = true;
			settings.Browser.MainWindow.UrlPolicy = Settings.Browser.UrlPolicy.BeforeTitle;
			settings.Keyboard.AllowPrintScreen = true;
			settings.LogLevel = Settings.Logging.LogLevel.Debug;
			settings.Security.AllowApplicationLogAccess = true;
			settings.Security.ClipboardPolicy = ClipboardPolicy.Allow;
			settings.Security.KioskMode = KioskMode.CreateNewDesktop;
			settings.Security.QuitPasswordHash = default;
			settings.Service.IgnoreService = true;
			settings.Service.Policy = Settings.Service.ServicePolicy.Optional;
			settings.Security.VersionRestrictions.Clear();
			settings.Taskbar.ShowApplicationLog = true;
		}

		private void AllowBrowserToolbarForReloading(AppSettings settings)
		{
			if (settings.Browser.AdditionalWindow.AllowReloading && settings.Browser.AdditionalWindow.ShowReloadButton)
			{
				settings.Browser.AdditionalWindow.ShowToolbar = true;
			}

			if (settings.Browser.MainWindow.AllowReloading && settings.Browser.MainWindow.ShowReloadButton)
			{
				settings.Browser.MainWindow.ShowToolbar = true;
			}
		}

		private void CalculateConfigurationKey(IDictionary<string, object> rawData, AppSettings settings)
		{
			using (var algorithm = new SHA256Managed())
			using (var stream = new MemoryStream())
			using (var writer = new StreamWriter(stream))
			{
				Json.Serialize(rawData, writer);

				writer.Flush();
				stream.Seek(0, SeekOrigin.Begin);

				var hash = algorithm.ComputeHash(stream);
				var key = BitConverter.ToString(hash).ToLower().Replace("-", string.Empty);

				settings.Browser.ConfigurationKey = key;
			}
		}

		private void InitializeBrowserHomeFunctionality(AppSettings settings)
		{
			settings.Browser.MainWindow.ShowHomeButton = settings.Browser.UseStartUrlAsHomeUrl || !string.IsNullOrWhiteSpace(settings.Browser.HomeUrl);
			settings.Browser.HomePasswordHash = settings.Security.QuitPasswordHash;
		}

		private void InitializeClipboardSettings(AppSettings settings)
		{
			settings.Browser.UseIsolatedClipboard = settings.Security.ClipboardPolicy == ClipboardPolicy.Isolated;
			settings.Keyboard.AllowCtrlC = settings.Security.ClipboardPolicy != ClipboardPolicy.Block;
			settings.Keyboard.AllowCtrlV = settings.Security.ClipboardPolicy != ClipboardPolicy.Block;
			settings.Keyboard.AllowCtrlX = settings.Security.ClipboardPolicy != ClipboardPolicy.Block;
		}

		private void InitializeProctoringSettings(AppSettings settings)
		{
			settings.Proctoring.Enabled = settings.Proctoring.ScreenProctoring.Enabled;
		}

		private void RemoveLegacyBrowsers(AppSettings settings)
		{
			var legacyBrowsers = new List<WhitelistApplication>();

			foreach (var application in settings.Applications.Whitelist)
			{
				var isEnginePath = application.ExecutablePath?.Contains("xulrunner") == true;
				var isFirefox = application.ExecutableName?.Equals("firefox.exe", StringComparison.OrdinalIgnoreCase) == true;
				var isXulRunner = application.ExecutableName?.Equals("xulrunner.exe", StringComparison.OrdinalIgnoreCase) == true;

				if (isEnginePath && (isFirefox || isXulRunner))
				{
					legacyBrowsers.Add(application);
				}
			}

			foreach (var legacyBrowser in legacyBrowsers)
			{
				settings.Applications.Whitelist.Remove(legacyBrowser);
			}
		}
	}
}
