// SPDX-FileCopyrightText: � 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: � 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    /// <summary>
    /// IWadoService provides APIs to retrieve data according to
    /// DICOMweb WADO-RS specs.
    /// </summary>
    public interface IWadoService : IServiceBase
    {
        /// <summary>
        /// Retrieves all DICOM instances specified in the study.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A list of <see href="https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomFile.cs">DicomFile</see> containing DICOM instances for the study.</returns>
        IAsyncEnumerable<DicomFile> Retrieve(
            string studyInstanceUid,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves all DICOM instances secified in the series.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="seriesInstanceUid">Series Instance UID</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A list of <see href="https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomFile.cs">DicomFile</see> containing DICOM instances for the series.</returns>
        IAsyncEnumerable<DicomFile> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves a DICOM instance.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="seriesInstanceUid">Series Instance UID</param>
        /// <param name="sopInstanceUid">SOP Instance UID</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A <see href="https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomFile.cs">DicomFile</see> representing the DICOM instance.</returns>
        Task<DicomFile> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves one or more frames from a multi-frame DICOM instance.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="seriesInstanceUid">Series Instance UID</param>
        /// <param name="sopInstanceUid">SOP Instance UID</param>
        /// <param name="frameNumbers">The frames to retrieve within a multi-frame instance. (One-based indices)</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A <see href="https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomFile.cs">DicomFile</see> representing the DICOM instance.</returns>
        Task<DicomFile> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            IReadOnlyList<uint> frameNumbers,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves bulkdata in a DICOM instance.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="seriesInstanceUid">Series Instance UID</param>
        /// <param name="sopInstanceUid">SOP Instance UID</param>
        /// <param name="dicomTag">DICOM tag containing to bulkdata</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes to be used to encode the bulkdata. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A byte array containing the bulkdata.</returns>
        Task<byte[]> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            DicomTag dicomTag,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves a specific range of bulkdata in a DICOM instance.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="seriesInstanceUid">Series Instance UID</param>
        /// <param name="sopInstanceUid">SOP Instance UID</param>
        /// <param name="dicomTag">DICOM tag containing to bulkdata</param>
        /// <param name="byteRange">Range of data to retrieve.
        /// Entire range if <c>null</c>.
        /// If <c>byteRange.Item2</c> is not specified then value specified in <c>byteRange.Item1</c>(start) to the end is retrieved.</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes to be used to encode the bulkdata. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A byte array containing the bulkdata.</returns>
        Task<byte[]> Retrieve(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            DicomTag dicomTag,
            Tuple<int, int?> byteRange = null,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves bulkdata in a DICOM instance.
        /// </summary>
        /// <param name="bulkdataUri">URI to the instance.  The DICOM tag to retrieve must specified in the URI.</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes to be used to encode the bulkdata. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A byte array containing the bulkdata.</returns>
        Task<byte[]> Retrieve(
            Uri bulkdataUri,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves a specific range of bulkdata in a DICOM instance.
        /// </summary>
        /// <param name="bulkdataUri">URI to the instance.  The DICOM tag to retrieve must specified in the URI.</param>
        /// <param name="byteRange">Range of data to retrieve.
        /// Entire range if <c>null</c>.
        /// If <c>byteRange.Item2</c> is not specified then value specified in <c>byteRange.Item1</c>(start) to the end is retrieved.</param>
        /// <param name="transferSyntaxes">An array of supported transfer syntaxes to be used to encode the bulkdata. Default set to Explicit VR Little Endian (1.2.840.10008.1.2.1)</param>
        /// <returns>A byte array containing the bulkdata.</returns>
        Task<byte[]> Retrieve(
            Uri bulkdataUri,
            Tuple<int, int?> byteRange = null,
            params DicomTransferSyntax[] transferSyntaxes);

        /// <summary>
        /// Retrieves the metadata of all instances specified in the study.
        /// </summary>
        /// <typeparam name="T">T must be type of <see href="https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs">DicomDataset</see> or <see href="https://docs.microsoft.com/dotnet/api/system.string">System.String</see>.</typeparam>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <returns>An enumerable of <c>T</c> containing DICOM instances for the study.</returns>
        IAsyncEnumerable<T> RetrieveMetadata<T>(
            string studyInstanceUid);

        /// <summary>
        /// Retrieves the metadata of all instances specified in the study.
        /// </summary>
        /// <typeparam name="T">T must be type of <see href="https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs">DicomDataset</see> or <see href="https://docs.microsoft.com/dotnet/api/system.string">System.String</see>.</typeparam>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="seriesInstanceUid">Series Instance UID</param>
        /// <returns>An enumerable of <c>T</c> containing DICOM instances for the series.</returns>
        IAsyncEnumerable<T> RetrieveMetadata<T>(
            string studyInstanceUid,
            string seriesInstanceUid);

        /// <summary>
        /// Retrieves the metadata of the specified DICOM instance.
        /// </summary>
        /// <typeparam name="T">T must be type of <see href="https://github.com/fo-dicom/fo-dicom/blob/development/FO-DICOM.Core/DicomDataset.cs">DicomDataset</see> or <see href="https://docs.microsoft.com/dotnet/api/system.string">System.String</see>.</typeparam>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="seriesInstanceUid">Series Instance UID</param>
        /// <param name="sopInstanceUid">SOP Instance UID</param>
        /// <returns>A <c>T</c> containing DICOM metadata for the instance.</returns>
        Task<T> RetrieveMetadata<T>(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid);
    }
}
