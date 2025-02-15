﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO.Abstractions;
using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ContainerRunnerFactoryTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<ILogger<DockerRunner>> _logger;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IDockerClient> _dockerClient;

        public ContainerRunnerFactoryTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _configurationService = new Mock<IConfigurationService>();
            _serviceScope = new Mock<IServiceScope>();
            _logger = new Mock<ILogger<DockerRunner>>();
            _fileSystem = new Mock<IFileSystem>();
            _dockerClient = new Mock<IDockerClient>();
        }

        [Fact(DisplayName = "ContainerRunnerFactory Constructor")]
        public void ContainerRunnerFactory_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new ContainerRunnerFactory(null, null));
            Assert.Throws<ArgumentNullException>(() => new ContainerRunnerFactory(_serviceScopeFactory.Object, null));
        }

        [Fact(DisplayName = "GetContainerRunner")]
        public void GetContainerRunner()
        {
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            _configurationService.SetupGet(p => p.Configurations.Runner).Returns(Runner.Docker);
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider.GetService(It.IsAny<Type>())).Returns(runner);
            var factory = new ContainerRunnerFactory(_serviceScopeFactory.Object, _configurationService.Object);

            var result = factory.GetContainerRunner();
            Assert.Equal(result, runner);
        }

        [Fact(DisplayName = "GetContainerRunner NotImplementedException")]
        public void GetContainerRunner_NotImplementedException()
        {
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            _configurationService.SetupGet(p => p.Configurations.Runner).Returns(Runner.Helm);
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            var factory = new ContainerRunnerFactory(_serviceScopeFactory.Object, _configurationService.Object);

            Assert.Throws<NotImplementedException>(() => factory.GetContainerRunner());
        }
    }
}
