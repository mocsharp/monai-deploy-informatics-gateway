﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    /// <summary>
    /// Represents <c>dicom>scu</c> section of the configuration file.
    /// </summary>
    public class ScuConfiguration
    {
        /// <summary>
        /// Gets or sets the AE Title for SCU service.
        /// </summary>
        [ConfigurationKeyName("aeTitle")]
        public string AeTitle { get; set; } = "MONAISCU";

        /// <summary>
        /// Gets or sets the (postfix) name of the DIMSE export agent used for receiving messages.
        /// The agent name is combine with <see cref="MessageBrokerConfigurationKeys.ExportRequestPrefix"/>
        /// for subscribing messages from the message broker service.
        /// </summary>
        [ConfigurationKeyName("agentName")]
        public string AgentName { get; set; } = "monaiscu";

        /// <summary>
        /// Gets or sets whether or not to write message to log for each P-Data-TF PDU sent or received.
        /// </summary>
        [ConfigurationKeyName("logDataPDUs")]
        public bool LogDataPdus { get; set; } = false;

        /// <summary>
        /// Gets or sets whether or not to write command and data datasets to the log.
        /// </summary>
        [ConfigurationKeyName("logDimseDatasets")]
        public bool LogDimseDatasets { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of simultaneous DICOM associations for the SCU service.
        /// </summary>
        [ConfigurationKeyName("maximumNumberOfAssociations")]
        public int MaximumNumberOfAssociations { get; set; } = 8;

        public ScuConfiguration()
        {
        }
    }
}
