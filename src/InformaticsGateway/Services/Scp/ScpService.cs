// SPDX-FileCopyrightText: � 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: � 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using FoDicomNetwork = FellowOakDicom.Network;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal sealed class ScpService : IHostedService, IDisposable, IMonaiService
    {
#pragma warning disable S2223 // Non-constant static fields should not be visible
        public static int ActiveConnections = 0;
#pragma warning restore S2223 // Non-constant static fields should not be visible

        private readonly IServiceScope _serviceScope;
        private readonly IApplicationEntityManager _associationDataProvider;
        private readonly ILogger<ScpService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private FoDicomNetwork.IDicomServer _server;
        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public string ServiceName => "DICOM SCP Service";

        public ScpService(IServiceScopeFactory serviceScopeFactory,
                                IApplicationEntityManager applicationEntityManager,
                                IHostApplicationLifetime appLifetime,
                                IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _serviceScope = serviceScopeFactory.CreateScope();
            _associationDataProvider = applicationEntityManager ?? throw new ArgumentNullException(nameof(applicationEntityManager));

            var logginFactory = _serviceScope.ServiceProvider.GetService<ILoggerFactory>();

            _logger = logginFactory.CreateLogger<ScpService>();
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _ = DicomDictionary.Default;
        }

        public void Dispose()
        {
            _serviceScope.Dispose();
            _server?.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.ScpServiceLoading(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version);

            try
            {
                _logger.ServiceStarting(ServiceName);
                _server = FoDicomNetwork.DicomServerFactory.Create<ScpServiceInternal>(
                    FoDicomNetwork.NetworkManager.IPv4Any,
                    _configuration.Value.Dicom.Scp.Port,
                    userState: _associationDataProvider);

                _server.Options.IgnoreUnsupportedTransferSyntaxChange = true;
                _server.Options.LogDimseDatasets = _configuration.Value.Dicom.Scp.LogDimseDatasets;
                _server.Options.MaxClientsAllowed = _configuration.Value.Dicom.Scp.MaximumNumberOfAssociations;

                if (_server.Exception != null)
                {
                    _logger.ScpListenerInitializationFailure();
                    throw _server.Exception;
                }

                Status = ServiceStatus.Running;
                _logger.ScpListeningOnPort(_configuration.Value.Dicom.Scp.Port);
            }
            catch (System.Exception ex)
            {
                Status = ServiceStatus.Cancelled;
                _logger.ServiceFailedToStart(ServiceName, ex);
                _appLifetime.StopApplication();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            _server?.Stop();
            _server?.Dispose();
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }
    }
}
