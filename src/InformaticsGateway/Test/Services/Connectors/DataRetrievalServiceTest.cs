﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class DataRetrievalServiceTest
    {
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly Mock<ILogger<DicomWebClient>> _loggerDicomWebClient;
        private readonly Mock<ILogger<DataRetrievalService>> _logger;
        private readonly Mock<IInferenceRequestRepository> _inferenceRequestStore;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly MockFileSystem _fileSystem;
        private Mock<HttpMessageHandler> _handlerMock;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        public DataRetrievalServiceTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _logger = new Mock<ILogger<DataRetrievalService>>();
            _inferenceRequestStore = new Mock<IInferenceRequestRepository>();
            _dicomToolkit = new Mock<IDicomToolkit>();
            _fileSystem = new MockFileSystem();
            _loggerDicomWebClient = new Mock<ILogger<DicomWebClient>>();
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _payloadAssembler = new Mock<IPayloadAssembler>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns((string type) =>
            {
                return _loggerDicomWebClient.Object;
            });

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInferenceRequestRepository)))
                .Returns(_inferenceRequestStore.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "Constructor")]
        public void ConstructorTest()
        {
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(null, null, null, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, null, null, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, _httpClientFactory.Object, null, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, _httpClientFactory.Object, _logger.Object, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, _httpClientFactory.Object, _logger.Object, _fileSystem, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, _httpClientFactory.Object, _logger.Object, _fileSystem, _dicomToolkit.Object, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, _httpClientFactory.Object, _logger.Object, _fileSystem, _dicomToolkit.Object, _serviceScopeFactory.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, _httpClientFactory.Object, _logger.Object, _fileSystem, _dicomToolkit.Object, _serviceScopeFactory.Object, _payloadAssembler.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_loggerFactory.Object, _httpClientFactory.Object, _logger.Object, _fileSystem, _dicomToolkit.Object, _serviceScopeFactory.Object, _payloadAssembler.Object, _storageInfoProvider.Object, null));

            _ = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);
        }

        [RetryFact(5, 250, DisplayName = "Cancellation token shall stop the service")]
        public async Task CancellationTokenShallCancelTheService()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);
            Thread.Sleep(250);
            await store.StopAsync(cancellationTokenSource.Token);
            Thread.Sleep(500);

            _logger.VerifyLogging($"Data Retrieval Service is running.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Data Retrieval Service is stopping.", LogLevel.Information, Times.Once());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.Never());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "Insufficient storage space")]
        public async Task InsufficientStorageSpace()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(false);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);
            Thread.Sleep(250);
            await store.StopAsync(cancellationTokenSource.Token);

            _logger.VerifyLogging($"Data Retrieval Service is running.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Data Retrieval Service is stopping.", LogLevel.Information, Times.Once());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.AtLeastOnce());
        }

        [RetryFact(5, 250, DisplayName = "ProcessRequest - Shall restore previously retrieved DICOM files")]
        public async Task ProcessorRequest_ShallRestorePreviouslyRetrievedFiles()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var storagePath = "/store";
            _fileSystem.Directory.CreateDirectory(storagePath);
            _fileSystem.File.Create(_fileSystem.Path.Combine(storagePath, "file1.dcm"));
            _fileSystem.File.Create(_fileSystem.Path.Combine(storagePath, "file2.dcm"));
            _fileSystem.File.Create(_fileSystem.Path.Combine(storagePath, "file3.dcm"));
            _fileSystem.File.Create(_fileSystem.Path.Combine(storagePath, "fhir.json"));
            _fileSystem.File.Create(_fileSystem.Path.Combine(storagePath, "text.txt"));
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                InputMetadata = new InferenceRequestMetadata()
            };
            request.InputMetadata.Details = new InferenceRequestDetails()
            {
                Type = InferenceRequestType.DicomPatientId,
                PatientId = "123"
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails()
                    {
                        Uri = "http://valid.uri/api",
                        AuthType = ConnectionAuthType.None
                    }
                });
            request.ConfigureTemporaryStorageLocation(storagePath);
            Assert.True(request.IsValid(out string _));

            _dicomToolkit.Setup(p => p.HasValidHeader(It.IsAny<string>()))
                .Returns((string filename) =>
                {
                    if (filename.EndsWith("text.txt"))
                    {
                        return false;
                    }
                    else if (filename.EndsWith("corrupted.dcm"))
                    {
                        return false;
                    }
                    return true;
                });

            _dicomToolkit.Setup(p => p.Open(It.IsAny<string>()))
                .Returns(() => InstanceGenerator.GenerateDicomFile());

            _inferenceRequestStore.SetupSequence(p => p.Take(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock
                .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(() =>
                    {
                        return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("[]") };
                    });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);
            _payloadAssembler.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()));

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _logger.VerifyLoggingMessageBeginsWith($"Restored previously retrieved file", LogLevel.Debug, Times.Exactly(5));
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()), Times.Exactly(5));
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "ProcessRequest - Throws if no files found")]
        public async Task ProcessorRequest_ThrowsIfNoFilesFound()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var storagePath = "/store";
            _fileSystem.Directory.CreateDirectory(storagePath);

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>
                    {
                         new RequestedStudy
                         {
                              StudyInstanceUid = "1",
                              Series = new List<RequestedSeries>
                              {
                                  new RequestedSeries
                                  {
                                       SeriesInstanceUid = "1.1",
                                       Instances = new List<RequestedInstance>
                                       {
                                           new RequestedInstance
                                           {
                                                SopInstanceUid = new List<string>
                                                {
                                                    "1.1.2",
                                                    "1.1.3"
                                                }
                                           }
                                       }
                                  }
                              }
                         }
                    }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            request.ConfigureTemporaryStorageLocation(storagePath);

            _dicomToolkit.Setup(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()));

            _inferenceRequestStore.SetupSequence(p => p.Take(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock
            .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    var content = new MultipartContent("related");
                    content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"application/dicom\""));
                    return new HttpResponseMessage() { Content = new StringContent("[]") };
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(2),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{url}studies/")),
               ItExpr.IsAny<CancellationToken>());

            _dicomToolkit.Verify(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()), Times.Never());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()), Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "ProcessRequest - Shall retrieve via DICOMweb with DICOM UIDs")]
        public async Task ProcessorRequest_ShallRetrieveViaDicomWebWithDicomUid()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var storagePath = "/store";
            _fileSystem.Directory.CreateDirectory(storagePath);

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>
                    {
                         new RequestedStudy
                         {
                              StudyInstanceUid = "1",
                              Series = new List<RequestedSeries>
                              {
                                  new RequestedSeries
                                  {
                                       SeriesInstanceUid = "1.1",
                                       Instances = new List<RequestedInstance>
                                       {
                                           new RequestedInstance
                                           {
                                                SopInstanceUid = new List<string>
                                                {
                                                    "1.1.2",
                                                    "1.1.3"
                                                }
                                           }
                                       }
                                  }
                              }
                         },
                         new RequestedStudy
                         {
                              StudyInstanceUid = "2",
                              Series = new List<RequestedSeries>
                              {
                                  new RequestedSeries
                                  {
                                       SeriesInstanceUid = "2.1"
                                  }
                              }
                         },
                         new RequestedStudy
                         {
                              StudyInstanceUid = "3"
                         },
                    }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            request.ConfigureTemporaryStorageLocation(storagePath);

            _dicomToolkit.Setup(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()));

            _inferenceRequestStore.SetupSequence(p => p.Take(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock
            .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateMultipartResponse();
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(4),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{url}studies/")),
               ItExpr.IsAny<CancellationToken>());

            _dicomToolkit.Verify(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()), Times.Exactly(4));
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()), Times.Exactly(4));
        }

        [RetryFact(5, 250, DisplayName = "ProcessRequest - Shall query by PatientId and retrieve")]
        public async Task ProcessorRequest_ShallQueryByPatientIdAndRetrieve()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var storagePath = "/store";
            _fileSystem.Directory.CreateDirectory(storagePath);

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomPatientId,
                    PatientId = "ABC"
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            request.ConfigureTemporaryStorageLocation(storagePath);

            _dicomToolkit.Setup(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()));

            _inferenceRequestStore.SetupSequence(p => p.Take(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            var studyInstanceUids = new List<string>()
            {
                DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                DicomUIDGenerator.GenerateDerivedFromUUID().UID
            };
            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateQueryResult(DicomTag.PatientID, "ABC", studyInstanceUids);
                });
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => !p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateMultipartResponse();
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.Query.Contains("00100020=ABC")),
               ItExpr.IsAny<CancellationToken>());

            foreach (var studyInstanceUid in studyInstanceUids)
            {
                _handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Once(),
                   ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().StartsWith($"{url}studies/{studyInstanceUid}")),
                   ItExpr.IsAny<CancellationToken>());
            }

            _dicomToolkit.Verify(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()), Times.Exactly(studyInstanceUids.Count));
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()), Times.Exactly(studyInstanceUids.Count));
        }

        [RetryFact(5, 250, DisplayName = "ProcessRequest - Shall query by AccessionNumber and retrieve")]
        public async Task ProcessorRequest_ShallQueryByAccessionNumberAndRetrieve()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var storagePath = "/store";
            _fileSystem.Directory.CreateDirectory(storagePath);

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.AccessionNumber,
                    AccessionNumber = new List<string>() { "ABC" }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            request.ConfigureTemporaryStorageLocation(storagePath);

            _dicomToolkit.Setup(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()));

            _inferenceRequestStore.SetupSequence(p => p.Take(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            var studyInstanceUids = new List<string>()
            {
                DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                DicomUIDGenerator.GenerateDerivedFromUUID().UID
            };
            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateQueryResult(DicomTag.AccessionNumber, "ABC", studyInstanceUids);
                });
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => !p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateMultipartResponse();
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.Query.Contains("00080050=ABC")),
               ItExpr.IsAny<CancellationToken>());

            foreach (var studyInstanceUid in studyInstanceUids)
            {
                _handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Once(),
                   ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().StartsWith($"{url}studies/{studyInstanceUid}")),
                   ItExpr.IsAny<CancellationToken>());
            }

            _dicomToolkit.Verify(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()), Times.Exactly(studyInstanceUids.Count));
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()), Times.Exactly(2));
        }

        [RetryFact(5, 250, DisplayName = "ProcessRequest - Shall retrieve FHIR resources")]
        public async Task ProcessorRequest_ShallRetrieveFhirResources()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var storagePath = "/store";
            _fileSystem.Directory.CreateDirectory(storagePath);

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.FhireResource,
                    Resources = new List<FhirResource>()
                    {
                        new FhirResource
                        {
                            Id = "1",
                            Type = "Patient"
                        }
                    }
                },
                Inputs = new List<InferenceRequestDetails>()
                {
                    new InferenceRequestDetails
                    {
                        Type = InferenceRequestType.FhireResource,
                        Resources = new List<FhirResource>()
                        {
                            new FhirResource
                            {
                                Id = "2",
                                Type = "Observation"
                            }
                        }
                    }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Fhir,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Bearer,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            request.ConfigureTemporaryStorageLocation(storagePath);

            _inferenceRequestStore.SetupSequence(p => p.Take(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{}") });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _logger.Object,
                _fileSystem,
                _dicomToolkit.Object,
                _serviceScopeFactory.Object,
                _payloadAssembler.Object,
                _storageInfoProvider.Object,
                _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.PathAndQuery.Contains("Patient/1")),
               ItExpr.IsAny<CancellationToken>());
            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.PathAndQuery.Contains("Observation/2")),
               ItExpr.IsAny<CancellationToken>());

            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()), Times.Exactly(2));
        }

        private static HttpResponseMessage GenerateQueryResult(DicomTag dicomTag, string queryValue, List<string> studyInstanceUids)
        {
            var set = new List<string>();
            foreach (var studyInstanceUid in studyInstanceUids)
            {
                var dataset = new DicomDataset
                {
                    { dicomTag, queryValue },
                    { DicomTag.StudyInstanceUID, studyInstanceUid }
                };
                set.Add(DicomJson.ConvertDicomToJson(dataset));
            }
            var json = JsonConvert.SerializeObject(set);
            var stringContent = new StringContent(json);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = stringContent };
        }

        private static HttpResponseMessage GenerateMultipartResponse()
        {
            var data = InstanceGenerator.GenerateDicomData();
            var content = new MultipartContent("related");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"application/dicom\""));
            var byteContent = new StreamContent(new MemoryStream(data));
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/dicom");
            content.Add(byteContent);
            return new HttpResponseMessage() { Content = content };
        }

        private static void BlockUntilCancelled(CancellationToken token)
        {
            WaitHandle.WaitAll(new[] { token.WaitHandle });
        }
    }
}
