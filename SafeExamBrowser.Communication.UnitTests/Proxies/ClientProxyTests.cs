﻿/*
 * Copyright (c) 2018 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SafeExamBrowser.Contracts.Communication.Data;
using SafeExamBrowser.Contracts.Communication.Proxies;
using SafeExamBrowser.Contracts.Logging;
using SafeExamBrowser.Communication.Proxies;

namespace SafeExamBrowser.Communication.UnitTests.Proxies
{
	[TestClass]
	public class ClientProxyTests
	{
		private Mock<ILogger> logger;
		private Mock<IProxyObjectFactory> proxyObjectFactory;
		private Mock<IProxyObject> proxy;
		private ClientProxy sut;

		[TestInitialize]
		public void Initialize()
		{
			var response = new ConnectionResponse
			{
				CommunicationToken = Guid.NewGuid(),
				ConnectionEstablished = true
			};

			logger = new Mock<ILogger>();
			proxyObjectFactory = new Mock<IProxyObjectFactory>();
			proxy = new Mock<IProxyObject>();

			proxy.Setup(p => p.Connect(It.IsAny<Guid>())).Returns(response);
			proxy.Setup(o => o.State).Returns(CommunicationState.Opened);
			proxyObjectFactory.Setup(f => f.CreateObject(It.IsAny<string>())).Returns(proxy.Object);

			sut = new ClientProxy("net.pipe://random/address/here", proxyObjectFactory.Object, logger.Object);
			sut.Connect(Guid.NewGuid());
		}

		[TestMethod]
		public void MustCorrectlyInitiateShutdown()
		{
			proxy.Setup(p => p.Send(It.Is<SimpleMessage>(m => m.Purport == SimpleMessagePurport.Shutdown))).Returns(new SimpleResponse(SimpleResponsePurport.Acknowledged));

			var communication = sut.InitiateShutdown();

			proxy.Verify(p => p.Send(It.Is<SimpleMessage>(m => m.Purport == SimpleMessagePurport.Shutdown)), Times.Once);
			Assert.IsTrue(communication.Success);
		}

		[TestMethod]
		public void MustFailIfShutdownCommandNotAcknowledged()
		{
			proxy.Setup(p => p.Send(It.Is<SimpleMessage>(m => m.Purport == SimpleMessagePurport.Shutdown))).Returns<Response>(null);

			var communication = sut.InitiateShutdown();

			Assert.IsFalse(communication.Success);
		}

		[TestMethod]
		public void MustCorrectlyRequestAuthentication()
		{
			proxy.Setup(p => p.Send(It.Is<SimpleMessage>(m => m.Purport == SimpleMessagePurport.Authenticate))).Returns(new AuthenticationResponse());

			var communication = sut.RequestAuthentication();
			var response = communication.Value;

			proxy.Verify(p => p.Send(It.Is<SimpleMessage>(m => m.Purport == SimpleMessagePurport.Authenticate)), Times.Once);

			Assert.IsTrue(communication.Success);
			Assert.IsInstanceOfType(response, typeof(AuthenticationResponse));
		}

		[TestMethod]
		public void MustFailIfAuthenticationCommandNotAcknowledged()
		{
			proxy.Setup(p => p.Send(It.Is<SimpleMessage>(m => m.Purport == SimpleMessagePurport.Authenticate))).Returns<Response>(null);

			var communication = sut.RequestAuthentication();

			Assert.AreEqual(default(AuthenticationResponse), communication.Value);
			Assert.IsFalse(communication.Success);
		}

		[TestMethod]
		public void MustCorrectlyInformAboutReconfigurationDenial()
		{
			proxy.Setup(p => p.Send(It.IsAny<ReconfigurationDeniedMessage>())).Returns(new SimpleResponse(SimpleResponsePurport.Acknowledged));

			var communication = sut.InformReconfigurationDenied(null);

			proxy.Verify(p => p.Send(It.IsAny<ReconfigurationDeniedMessage>()), Times.Once);
			Assert.IsTrue(communication.Success);
		}

		[TestMethod]
		public void MustFailIfReconfigurationDenialNotAcknowledged()
		{
			proxy.Setup(p => p.Send(It.IsAny<ReconfigurationDeniedMessage>())).Returns<Response>(null);

			var communication = sut.InformReconfigurationDenied(null);

			Assert.IsFalse(communication.Success);
		}

		[TestMethod]
		public void MustCorrectlyRequestPassword()
		{
			proxy.Setup(p => p.Send(It.IsAny<PasswordRequestMessage>())).Returns(new SimpleResponse(SimpleResponsePurport.Acknowledged));

			var communication = sut.RequestPassword(default(PasswordRequestPurpose), default(Guid));

			proxy.Verify(p => p.Send(It.IsAny<PasswordRequestMessage>()), Times.Once);
			Assert.IsTrue(communication.Success);
		}

		[TestMethod]
		public void MustFailIfPasswordRequestNotAcknowledged()
		{
			proxy.Setup(p => p.Send(It.IsAny<PasswordRequestMessage>())).Returns<Response>(null);

			var communication = sut.RequestPassword(default(PasswordRequestPurpose), default(Guid));

			Assert.IsFalse(communication.Success);
		}

		[TestMethod]
		public void MustExecuteOperationsFailsafe()
		{
			proxy.Setup(p => p.Send(It.IsAny<Message>())).Throws<Exception>();

			var authenticate = sut.RequestAuthentication();
			var password = sut.RequestPassword(default(PasswordRequestPurpose), default(Guid));
			var reconfiguration = sut.InformReconfigurationDenied(null);
			var shutdown = sut.InitiateShutdown();

			Assert.IsFalse(authenticate.Success);
			Assert.IsFalse(password.Success);
			Assert.IsFalse(reconfiguration.Success);
			Assert.IsFalse(shutdown.Success);
		}
	}
}