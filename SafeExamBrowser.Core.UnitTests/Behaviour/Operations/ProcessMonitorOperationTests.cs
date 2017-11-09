﻿/*
 * Copyright (c) 2017 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SafeExamBrowser.Contracts.Logging;
using SafeExamBrowser.Contracts.Monitoring;
using SafeExamBrowser.Contracts.UserInterface;
using SafeExamBrowser.Core.Behaviour.Operations;

namespace SafeExamBrowser.Core.UnitTests.Behaviour.Operations
{
	[TestClass]
	public class ProcessMonitorOperationTests
	{
		private Mock<ILogger> loggerMock;
		private Mock<IProcessMonitor> processMonitorMock;
		private Mock<ISplashScreen> splashScreenMock;

		private ProcessMonitorOperation sut;

		[TestInitialize]
		public void Initialize()
		{
			loggerMock = new Mock<ILogger>();
			processMonitorMock = new Mock<IProcessMonitor>();
			splashScreenMock = new Mock<ISplashScreen>();

			sut = new ProcessMonitorOperation(loggerMock.Object, processMonitorMock.Object)
			{
				SplashScreen = splashScreenMock.Object
			};
		}

		[TestMethod]
		public void MustPerformCorrectly()
		{
			var order = 0;

			processMonitorMock.Setup(p => p.CloseExplorerShell()).Callback(() => Assert.AreEqual(++order, 1));
			processMonitorMock.Setup(p => p.StartMonitoringExplorer()).Callback(() => Assert.AreEqual(++order, 2));

			sut.Perform();

			processMonitorMock.Verify(p => p.CloseExplorerShell(), Times.Once);
			processMonitorMock.Verify(p => p.StartMonitoringExplorer(), Times.Once);
		}

		[TestMethod]
		public void MustRevertCorrectly()
		{
			var order = 0;

			processMonitorMock.Setup(p => p.StopMonitoringExplorer()).Callback(() => Assert.AreEqual(++order, 1));
			processMonitorMock.Setup(p => p.StartExplorerShell()).Callback(() => Assert.AreEqual(++order, 2));

			sut.Revert();

			processMonitorMock.Verify(p => p.StopMonitoringExplorer(), Times.Once);
			processMonitorMock.Verify(p => p.StartExplorerShell(), Times.Once);
		}
	}
}