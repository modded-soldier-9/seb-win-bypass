﻿/*
 * Copyright (c) 2019 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Windows;
using SafeExamBrowser.Contracts.I18n;
using SafeExamBrowser.Contracts.UserInterface.MessageBox;
using SafeExamBrowser.Contracts.UserInterface.Windows;
using MessageBoxResult = SafeExamBrowser.Contracts.UserInterface.MessageBox.MessageBoxResult;

namespace SafeExamBrowser.UserInterface.Mobile
{
	public class MessageBox : IMessageBox
	{
		private IText text;

		public MessageBox(IText text)
		{
			this.text = text;
		}

		public MessageBoxResult Show(string message, string title, MessageBoxAction action = MessageBoxAction.Confirm, MessageBoxIcon icon = MessageBoxIcon.Information, IWindow parent = null)
		{
			var result = default(MessageBoxResult);

			if (parent is Window window)
			{
				result = window.Dispatcher.Invoke(() => new MessageBoxDialog(text).Show(message, title, action, icon, window));
			}
			else
			{
				result = new MessageBoxDialog(text).Show(message, title, action, icon);
			}

			return result;
		}

		public MessageBoxResult Show(TextKey message, TextKey title, MessageBoxAction action = MessageBoxAction.Confirm, MessageBoxIcon icon = MessageBoxIcon.Information, IWindow parent = null)
		{
			return Show(text.Get(message), text.Get(title), action, icon, parent);
		}
	}
}
