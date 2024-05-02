﻿/*
 * Copyright (c) 2024 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Windows;
using System.Windows.Input;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.UserInterface.Contracts.Windows;
using SafeExamBrowser.UserInterface.Contracts.Windows.Data;
using SafeExamBrowser.UserInterface.Contracts.Windows.Events;

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	public partial class NetworkDialog : Window, INetworkDialog
	{
		private readonly IText text;

		private WindowClosedEventHandler closed;
		private WindowClosingEventHandler closing;

		event WindowClosedEventHandler IWindow.Closed
		{
			add { closed += value; }
			remove { closed -= value; }
		}

		event WindowClosingEventHandler IWindow.Closing
		{
			add { closing += value; }
			remove { closing -= value; }
		}

		internal NetworkDialog(string message, string title, IText text)
		{
			this.text = text;

			InitializeComponent();
			InitializeNetworkDialog(message, title);
		}

		public void BringToForeground()
		{
			Dispatcher.Invoke(Activate);
		}

		public NetworkDialogResult Show(IWindow parent = null)
		{
			return Dispatcher.Invoke(() =>
			{
				var result = new NetworkDialogResult { Success = false };

				if (parent is Window)
				{
					Owner = parent as Window;
					WindowStartupLocation = WindowStartupLocation.CenterOwner;
				}

				if (ShowDialog() is true)
				{
					result.Password = Password.Password;
					result.Success = true;
					result.Username = Username.Text;
				}

				return result;
			});
		}

		private void InitializeNetworkDialog(string message, string title)
		{
			Message.Text = message;
			// TODO
			PasswordLabel.Content = "Password";
			Title = title;
			// TODO
			UsernameLabel.Content = "Username";
			WindowStartupLocation = WindowStartupLocation.CenterScreen;

			Closed += (o, args) => closed?.Invoke();
			Closing += (o, args) => closing?.Invoke();
			Loaded += (o, args) => Activate();

			CancelButton.Content = text.Get(TextKey.PasswordDialog_Cancel);
			CancelButton.Click += CancelButton_Click;

			ConfirmButton.Content = text.Get(TextKey.PasswordDialog_Confirm);
			ConfirmButton.Click += ConfirmButton_Click;

			Password.KeyDown += Password_KeyDown;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void ConfirmButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Password_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				DialogResult = true;
				Close();
			}
		}
	}
}
