﻿/*
 * Copyright (c) 2019 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using CefSharp;
using CefSharp.WinForms;
using SafeExamBrowser.Contracts.UserInterface.Browser;
using SafeExamBrowser.Contracts.UserInterface.Browser.Events;

namespace SafeExamBrowser.Browser
{
	internal class BrowserControl : ChromiumWebBrowser, IBrowserControl
	{
		private const uint WS_EX_NOACTIVATE = 0x08000000;
		private const double ZOOM_FACTOR = 0.1;

		private IContextMenuHandler contextMenuHandler;
		private IDisplayHandler displayHandler;
		private IDownloadHandler downloadHandler;
		private IKeyboardHandler keyboardHandler;
		private ILifeSpanHandler lifeSpanHandler;
		private IRequestHandler requestHandler;

		private AddressChangedEventHandler addressChanged;
		private LoadingStateChangedEventHandler loadingStateChanged;
		private TitleChangedEventHandler titleChanged;

		public bool CanNavigateBackwards => GetBrowser().CanGoBack;
		public bool CanNavigateForwards => GetBrowser().CanGoForward;

		event AddressChangedEventHandler IBrowserControl.AddressChanged
		{
			add { addressChanged += value; }
			remove { addressChanged -= value; }
		}

		event LoadingStateChangedEventHandler IBrowserControl.LoadingStateChanged
		{
			add { loadingStateChanged += value; }
			remove { loadingStateChanged -= value; }
		}

		event TitleChangedEventHandler IBrowserControl.TitleChanged
		{
			add { titleChanged += value; }
			remove { titleChanged -= value; }
		}

		public BrowserControl(
			IContextMenuHandler contextMenuHandler,
			IDisplayHandler displayHandler,
			IDownloadHandler downloadHandler,
			IKeyboardHandler keyboardHandler,
			ILifeSpanHandler lifeSpanHandler,
			IRequestHandler requestHandler,
			string url) : base(url)
		{
			this.contextMenuHandler = contextMenuHandler;
			this.displayHandler = displayHandler;
			this.downloadHandler = downloadHandler;
			this.keyboardHandler = keyboardHandler;
			this.lifeSpanHandler = lifeSpanHandler;
			this.requestHandler = requestHandler;
		}

		public void Initialize()
		{
			AddressChanged += (o, args) => addressChanged?.Invoke(args.Address);
			LoadingStateChanged += (o, args) => loadingStateChanged?.Invoke(args.IsLoading);
			TitleChanged += (o, args) => titleChanged?.Invoke(args.Title);

			DisplayHandler = displayHandler;
			DownloadHandler = downloadHandler;
			KeyboardHandler = keyboardHandler;
			LifeSpanHandler = lifeSpanHandler;
			MenuHandler = contextMenuHandler;
			RequestHandler = requestHandler;
		}

		public void NavigateBackwards()
		{
			GetBrowser().GoBack();
		}

		public void NavigateForwards()
		{
			GetBrowser().GoForward();
		}

		public void NavigateTo(string address)
		{
			Load(address);
		}

		public void Reload()
		{
			GetBrowser().Reload();
		}

		public void ZoomReset()
		{
			GetBrowser().SetZoomLevel(0);
		}

		public void ZoomIn()
		{
			GetBrowser().GetZoomLevelAsync().ContinueWith(task =>
			{
				if (task.IsCompleted)
				{
					GetBrowser().SetZoomLevel(task.Result + ZOOM_FACTOR);
				}
			});
		}

		public void ZoomOut()
		{
			GetBrowser().GetZoomLevelAsync().ContinueWith(task =>
			{
				if (task.IsCompleted)
				{
					GetBrowser().SetZoomLevel(task.Result - ZOOM_FACTOR);
				}
			});
		}

		/// <summary>
		/// TODO: This is a workaround due to the broken initial touch activation in version 73.1.130, it must be removed once fixed in CefSharp.
		///       See https://github.com/cefsharp/CefSharp/issues/2776 for more information.
		/// </summary>
		protected override IWindowInfo CreateBrowserWindowInfo(IntPtr handle)
		{
			var windowInfo = base.CreateBrowserWindowInfo(handle);

			windowInfo.ExStyle &= ~WS_EX_NOACTIVATE;

			return windowInfo;
		}
	}
}
