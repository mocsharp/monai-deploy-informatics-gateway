﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Common;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Status of an inference request.
    /// </summary>
    public enum InferenceRequestStatus
    {
        Unknown,
        Success,
        Fail
    }

    /// <summary>
    /// State of a inference request.
    /// </summary>
    public enum InferenceRequestState
    {
        /// <summary>
        /// Indicates that an inference request is currently queued for data retrieval.
        /// </summary>
        Queued,

        /// <summary>
        /// The inference request is being processing by the MONAI Deploy Informatics Gateway.
        /// </summary>
        InProcess,

        /// <summary>
        /// Indicates MONAI Deploy Informatics Gateway has downloaded all the specified resources and uploaded to the MONAI Workload Manager.
        /// </summary>
        Completed,
    }

    /// <summary>
    /// Structure that represents an inference request based on ACR's Platform-Model Communication for AI.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "transactionID": "ABCDEF123456",
    ///     "priority": "255",
    ///     "inputMetadata": { ... },
    ///     "inputResources": [ ... ],
    ///     "outputResources": [ ... ]
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Refer to [ACR DSI Model API](https://www.acrdsi.org/-/media/DSI/Files/ACR-DSI-Model-API.pdf)
    /// for more information.
    /// <para><c>transactionID></c> is required.</para>
    /// <para><c>inputMetadata></c> is required.</para>
    /// <para><c>inputResources></c> is required.</para>
    /// </remarks>
    public class InferenceRequest
    {
        /// <summary>
        /// Gets or set the transaction ID of a request.
        /// </summary>
        [JsonPropertyName("transactionID")]
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the priority of a request.
        /// </summary>
        /// <remarks>
        /// <para>Default value is <c>128</c> which maps to <c>JOB_PRIORITY_NORMAL</c>.</para>
        /// <para>Any value lower than <c>128</c> is map to <c>JOB_PRIORITY_LOWER</c>.</para>
        /// <para>Any value between <c>129-254</c> (inclusive) is set to <c>JOB_PRIORITY_HIGHER</c>.</para>
        /// <para>Value of <c>255</c> maps to <c>JOB_PRIORITY_IMMEDIATE</c>.</para>
        /// </remarks>
        [JsonPropertyName("priority")]
        public byte Priority { get; set; } = 128;

        /// <summary>
        /// Gets or sets the details of the data associated with the inference request.
        /// </summary>
        [JsonPropertyName("inputMetadata")]
        public InferenceRequestMetadata InputMetadata { get; set; }

        /// <summary>
        /// Gets or set a list of data sources to query/retrieve data from.
        /// When multiple data sources are specified, the system will query based on
        /// the order the list was received.
        /// </summary>
        [JsonPropertyName("inputResources")]
        public IList<RequestInputDataResource> InputResources { get; set; }

        /// <summary>
        /// Gets or set a list of data sources to export results to.
        /// In order to export via DICOMweb, an Export Sink must be created and connected to
        /// the deployed application via the MONAI Workload Manager.
        /// Followed by registering the results using the MONAI App SDK.
        /// </summary>
        [JsonPropertyName("outputResources")]
        public IList<RequestOutputDataResource> OutputResources { get; set; }

        #region Internal Use Only

        /// <summary>
        /// Unique identity for the request.
        /// </summary>
        public Guid InferenceRequestId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Internal use only - get or sets the state of a inference request.
        /// </summary>
        /// <remarks>
        /// Internal use only.
        /// </remarks>
        [JsonPropertyName("state")]
        public InferenceRequestState State { get; set; } = InferenceRequestState.Queued;

        /// <summary>
        /// Internal use only - get or sets the status of a inference request.
        /// </summary>
        /// <remarks>
        /// Internal use only.
        /// </remarks>
        [JsonPropertyName("status")]
        public InferenceRequestStatus Status { get; set; } = InferenceRequestStatus.Unknown;

        /// <summary>
        /// Internal use only - get or sets the status of a inference request.
        /// </summary>
        /// <remarks>
        /// Internal use only.
        /// </remarks>
        [JsonPropertyName("storagePath")]
        public string StoragePath { get; set; }

        /// <summary>
        /// Internal use only - get or sets number of retries performed.
        /// </summary>
        /// <remarks>
        /// Internal use only.
        /// </remarks>
        [JsonPropertyName("tryCount")]
        public int TryCount { get; set; } = 0;

        [JsonIgnore]
        public InputConnectionDetails Application
        {
            get
            {
                return InputResources.FirstOrDefault(predicate => predicate.Interface == InputInterfaceType.Algorithm)?.ConnectionDetails;
            }
        }

        #endregion Internal Use Only

        public InferenceRequest()
        {
            InputResources = new List<RequestInputDataResource>();
            OutputResources = new List<RequestOutputDataResource>();
        }

        /// <summary>
        /// Configures temporary storage location used to store retrieved data.
        /// </summary>
        /// <param name="temporaryStorageRoot">Root path to the temporary storage location.</param>
        public void ConfigureTemporaryStorageLocation(string storagePath)
        {
            Guard.Against.NullOrWhiteSpace(storagePath, nameof(storagePath));
            if (!string.IsNullOrWhiteSpace(StoragePath))
            {
                throw new InferenceRequestException("StoragePath already configured.");
            }

            StoragePath = storagePath;
        }

        public bool IsValid(out string details)
        {
            Preprocess();
            return Validate(out details);
        }

        private void Preprocess()
        {
            if (InputMetadata is null)
            {
                InputMetadata = new InferenceRequestMetadata();
            }

            if (InputMetadata.Inputs is null)
            {
                InputMetadata.Inputs = new List<InferenceRequestDetails>();
            }

            if (InputMetadata.Details is not null)
            {
                InputMetadata.Inputs.Add(InputMetadata.Details);
                InputMetadata.Details = null;
            }
        }

        private bool Validate(out string details)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TransactionId))
            {
                errors.Add("'transactionId' is required.");
            }

            if (Application is null)
            {
                errors.Add("No algorithm defined or more than one algorithms defined in 'inputResources'.  'inputResources' must include one algorithm/pipeline for the inference request.");
            }

            ValidateInputResources(errors);
            ValidateInputMetadata(errors);
            ValidateOUtputResources(errors);

            details = string.Join(" ", errors);
            return errors.Count == 0;
        }

        private void ValidateOUtputResources(List<string> errors)
        {
            Guard.Against.Null(errors, nameof(errors));

            if (InputMetadata.Inputs.IsNullOrEmpty())
            {
                errors.Add("Request has no `inputMetadata` defined. At least one `inputs` or `inputMetadata` required.");
            }
            else
            {
                foreach (var inputDetails in InputMetadata.Inputs)
                {
                    CheckInputMetadataDetails(inputDetails, errors);
                }
            }
        }

        private void ValidateInputMetadata(List<string> errors)
        {
            Guard.Against.Null(errors, nameof(errors));

            foreach (var output in OutputResources)
            {
                if (output.Interface == InputInterfaceType.DicomWeb)
                {
                    CheckDicomWebConnectionDetails("outputResources", errors, output.ConnectionDetails);
                }
                else if (output.Interface == InputInterfaceType.Fhir)
                {
                    CheckFhirConnectionDetails("outputResources", errors, output.ConnectionDetails);
                }
            }
        }

        private void ValidateInputResources(List<string> errors)
        {
            Guard.Against.Null(errors, nameof(errors));

            if (InputResources.IsNullOrEmpty() ||
                !InputResources.Any(predicate => predicate.Interface != InputInterfaceType.Algorithm))
            {
                errors.Add("No 'inputResources' specified.");
            }

            foreach (var input in InputResources)
            {
                if (input.Interface == InputInterfaceType.DicomWeb)
                {
                    CheckDicomWebConnectionDetails("inputResources", errors, input.ConnectionDetails);
                }
                else if (input.Interface == InputInterfaceType.Fhir)
                {
                    CheckFhirConnectionDetails("inputResources", errors, input.ConnectionDetails);
                }
            }
        }

        private static void CheckInputMetadataDetails(InferenceRequestDetails details, List<string> errors)
        {
            switch (details.Type)
            {
                case InferenceRequestType.DicomUid:
                    CheckInputMetadataWithTypDicomUid(details, errors);
                    break;

                case InferenceRequestType.DicomPatientId:
                    if (string.IsNullOrWhiteSpace(details.PatientId))
                    {
                        errors.Add("Request type is set to `DICOM_PATIENT_ID` but `PatientID` is not defined.");
                    }
                    break;

                case InferenceRequestType.AccessionNumber:
                    if (details.AccessionNumber.IsNullOrEmpty())
                    {
                        errors.Add("Request type is set to `ACCESSION_NUMBER` but no `accessionNumber` defined.");
                    }
                    break;

                case InferenceRequestType.FhireResource:
                    CheckInputMetadataWithTypeFhirResource(details, errors);
                    break;

                default:
                    errors.Add($"'inputMetadata' does not yet support type '{details.Type}'.");
                    break;
            }
        }

        private static void CheckInputMetadataWithTypeFhirResource(InferenceRequestDetails details, List<string> errors)
        {
            Guard.Against.Null(details, nameof(details));
            Guard.Against.Null(errors, nameof(errors));

            if (details.Resources.IsNullOrEmpty())
            {
                errors.Add("Request type is set to `FHIR_RESOURCE` but no FHIR `resources` defined.");
            }
            else
            {
                errors.AddRange(details.Resources.Where(resource => string.IsNullOrWhiteSpace(resource.Type)).Select(resource => "A FHIR resource type cannot be empty."));
            }
        }

        private static void CheckInputMetadataWithTypDicomUid(InferenceRequestDetails details, List<string> errors)
        {
            Guard.Against.Null(details, nameof(details));
            Guard.Against.Null(errors, nameof(errors));

            if (details.Studies.IsNullOrEmpty())
            {
                errors.Add("Request type is set to `DICOM_UID` but no `studies` defined.");
            }
            else
            {
                foreach (var study in details.Studies)
                {
                    if (string.IsNullOrWhiteSpace(study.StudyInstanceUid))
                    {
                        errors.Add("`StudyInstanceUID` cannot be empty.");
                    }

                    if (study.Series is null) continue;
                    CheckInputMetadataWithTypeDicomSeries(errors, study);
                }
            }
        }

        private static void CheckInputMetadataWithTypeDicomSeries(List<string> errors, RequestedStudy study)
        {
            foreach (var series in study.Series)
            {
                if (string.IsNullOrWhiteSpace(series.SeriesInstanceUid))
                {
                    errors.Add("`SeriesInstanceUID` cannot be empty.");
                }

                if (series.Instances is null) continue;
                errors.AddRange(
                    series.Instances
                        .Where(
                            instance => instance.SopInstanceUid.IsNullOrEmpty() ||
                            instance.SopInstanceUid.Any(p => string.IsNullOrWhiteSpace(p)))
                        .Select(instance => "`SOPInstanceUID` cannot be empty."));
            }
        }

        private static void CheckFhirConnectionDetails(string source, List<string> errors, DicomWebConnectionDetails connection)
        {
            if (!Uri.IsWellFormedUriString(connection.Uri, UriKind.Absolute))
            {
                errors.Add($"The provided URI '{connection.Uri}' in `{source}` is not well formed.");
            }
        }

        private static void CheckDicomWebConnectionDetails(string source, List<string> errors, DicomWebConnectionDetails connection)
        {
            if (connection.AuthType != ConnectionAuthType.None && string.IsNullOrWhiteSpace(connection.AuthId))
            {
                errors.Add($"One of the '{source}' has authType of '{connection.AuthType:F}' but does not include a valid value for 'authId'");
            }

            if (!Uri.IsWellFormedUriString(connection.Uri, UriKind.Absolute))
            {
                errors.Add($"The provided URI '{connection.Uri}' is not well formed.");
            }
        }
    }
}
