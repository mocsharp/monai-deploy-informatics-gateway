// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public abstract class MessageBase
    {
        public static readonly string InformaticsGatewayApplicationId = "16988a78-87b5-4168-a5c3-2cfc2bab8e54";

        /// <summary>
        /// UUID for the message formatted with hyphens.
        /// xxxxxxxx-xxxx-Mxxx-Nxxx-xxxxxxxxxxxx
        /// </summary>
        public string MessageId { get; init; }

        /// <summary>
        /// Content or MIME type of the message body.
        /// </summary>
        public string ContentType { get; init; }

        /// <summary>
        /// UUID of the application, in this case, the Informatics Gateway.
        /// The UUID of Informatics Gateway is <code>16988a78-87b5-4168-a5c3-2cfc2bab8e54</code>.
        /// </summary>
        public string ApplicationId { get; init; }

        /// <summary>
        /// Correlation ID of the message.
        /// For DIMSE connections, the ID generated during association is used.
        /// For ACR inference requests, the Transaction ID provided in the request is used.
        /// </summary>
        public string CorrelationId { get; init; }

        /// <summary>
        /// Datetime the message is created.
        /// </summary>
        public DateTime CreationDateTime { get; init; }

        /// <summary>
        /// A short description of the type serialized in the message body.
        /// </summary>
        public string MessageDescription { get; init; }

        /// <summary>
        /// Gets or set the delivery tag/acknoweldge token for the message.
        /// </summary>
        public string DeliveryTag { get; init; }
    }
}