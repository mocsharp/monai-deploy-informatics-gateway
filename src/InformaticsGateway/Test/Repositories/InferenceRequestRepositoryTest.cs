﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Repositories
{
    public class InferenceRequestRepositoryTest
    {
        private readonly Mock<ILogger<InferenceRequestRepository>> _logger;
        private readonly Mock<IInformaticsGatewayRepository<InferenceRequest>> _inferenceRequestRepository;

        public InferenceRequestRepositoryTest()
        {
            _logger = new Mock<ILogger<InferenceRequestRepository>>();
            _inferenceRequestRepository = new Mock<IInformaticsGatewayRepository<InferenceRequest>>();
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "Constructor")]
        public void ConstructorTest()
        {
            Assert.Throws<ArgumentNullException>(() => new InferenceRequestRepository(null, null));
            Assert.Throws<ArgumentNullException>(() => new InferenceRequestRepository(_logger.Object, null));

            _ = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
        }

        [RetryFact(5, 250, DisplayName = "Add - Shall retry on failure")]
        public async Task Add_ShallRetryOnFailure()
        {
            _inferenceRequestRepository.Setup(p => p.AddAsync(It.IsAny<InferenceRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);

            await Assert.ThrowsAsync<Exception>(async () => await store.Add(inferenceRequest));

            _logger.VerifyLoggingMessageBeginsWith($"Error saving inference request", LogLevel.Error, Times.Exactly(3));
            _inferenceRequestRepository.Verify(p => p.AddAsync(It.IsAny<InferenceRequest>(), It.IsAny<CancellationToken>()), Times.AtLeast(3));
        }

        [RetryFact(5, 250, DisplayName = "Add - Shall add new job")]
        public async Task Add_ShallAddJob()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            await store.Add(inferenceRequest);

            _inferenceRequestRepository.Verify(p => p.AddAsync(It.IsAny<InferenceRequest>(), It.IsAny<CancellationToken>()), Times.Once());
            _inferenceRequestRepository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Inference request saved.", LogLevel.Debug, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall retry on failure")]
        public async Task UpdateSuccess_ShallRetryOnFailure()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            _inferenceRequestRepository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);

            await Assert.ThrowsAsync<Exception>(() => store.Update(inferenceRequest, InferenceRequestStatus.Success));

            _logger.VerifyLoggingMessageBeginsWith($"Error while updating inference request", LogLevel.Error, Times.Exactly(3));
            _inferenceRequestRepository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(3));
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall mark job as filed and save")]
        public async Task UpdateSuccess_ShallFailAndSave()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                TryCount = 3
            };

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);

            await store.Update(inferenceRequest, InferenceRequestStatus.Fail);

            _logger.VerifyLogging($"Updating inference request.", LogLevel.Debug, Times.Once());
            _logger.VerifyLogging($"Inference request updated.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Exceeded maximum retries.", LogLevel.Warning, Times.Once());
            _inferenceRequestRepository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Update - Shall save")]
        public async Task UpdateSuccess_ShallSave()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);

            await store.Update(inferenceRequest, InferenceRequestStatus.Fail);

            _logger.VerifyLogging($"Updating inference request.", LogLevel.Debug, Times.Once());
            _logger.VerifyLogging($"Inference request updated.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Will retry later.", LogLevel.Information, Times.Once());
            _inferenceRequestRepository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Take - Shall return next queued")]
        public async Task Take_ShallReturnQueuedItem()
        {
            var inferenceRequest = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString()
            };
            var cancellationSource = new CancellationTokenSource();

            _inferenceRequestRepository.SetupSequence(p => p.FirstOrDefault(It.IsAny<Func<InferenceRequest, bool>>()))
                .Returns(inferenceRequest);

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);

            _ = await store.Take(cancellationSource.Token);

            _logger.VerifyLogging($"Updating request {inferenceRequest.TransactionId} to InProgress.", LogLevel.Debug, Times.AtLeastOnce());
        }

        [RetryFact(5, 250, DisplayName = "Take - Shall throw when cancelled")]
        public async Task Take_ShallThrowWhenCancelled()
        {
            var cancellationSource = new CancellationTokenSource();
            _inferenceRequestRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<InferenceRequest, bool>>()))
                .Returns(default(InferenceRequest));

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            cancellationSource.CancelAfter(100);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await store.Take(cancellationSource.Token));
        }

        [RetryFact(5, 250, DisplayName = "Exists - throws if no arguments provided")]
        public void Exists_ThrowsIfNoArgumentsProvided()
        {
            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            Assert.Throws<ArgumentException>(() => store.Exists(string.Empty));
        }

        [RetryFact(5, 250, DisplayName = "Exists - returns true")]
        public void Exists_ReturnsTrue()
        {
            _inferenceRequestRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<InferenceRequest, bool>>()))
                .Returns(new InferenceRequest());
            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            Assert.True(store.Exists("abc"));
            _inferenceRequestRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<InferenceRequest, bool>>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Get transationId - throws if no arguments provided")]
        public void GetTransactionId_ThrowsIfNoArgumentsProvided()
        {
            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            Assert.Throws<ArgumentException>(() => store.GetInferenceRequest(string.Empty));
        }

        [RetryFact(5, 250, DisplayName = "Get inferenceRequestId - throws if no arguments provided")]
        public async Task GetInferenceRequestId_ThrowsIfNoArgumentsProvided()
        {
            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            await Assert.ThrowsAsync<ArgumentException>(async () => await store.GetInferenceRequest(Guid.Empty));
        }

        [RetryFact(5, 250, DisplayName = "Get - retrieves by transationId")]
        public void Get_RetrievesByJobId()
        {
            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            _ = store.GetInferenceRequest("id");
            _inferenceRequestRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<InferenceRequest, bool>>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Get - retrieves by inferenceRequestId")]
        public void Get_RetrievesByPayloadId()
        {
            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            var id = Guid.NewGuid();
            _ = store.GetInferenceRequest(id);
            _inferenceRequestRepository.Verify(p => p.FindAsync(It.IsAny<object[]>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Status - retrieves by transaction id")]
        public async Task Status_RetrievesByTransactionId()
        {
            _inferenceRequestRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<InferenceRequest, bool>>()))
                .Returns(new InferenceRequest
                {
                    TransactionId = "My Transaction ID",
                });

            var store = new InferenceRequestRepository(_logger.Object, _inferenceRequestRepository.Object);
            var id = Guid.NewGuid().ToString();
            var status = await store.GetStatus(id);

            Assert.Equal("My Transaction ID", status.TransactionId);

            _inferenceRequestRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<InferenceRequest, bool>>()), Times.Once());
        }
    }
}
