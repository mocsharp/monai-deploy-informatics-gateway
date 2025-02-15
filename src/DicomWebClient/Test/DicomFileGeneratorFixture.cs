﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Monai.Deploy.InformaticsGateway.DicomWebClient.Test
{
    public class DicomFileGeneratorFixture
    {
        public const string MimeApplicationDicomJson = "application/dicom+json";

        internal static async Task<HttpContent> GenerateInstances(
            int count,
            DicomUID studyUid,
            DicomUID seriesUid = null,
            DicomUID instanceUid = null,
            DicomTransferSyntax transferSynx = null)
        {
            var multipartContent = new MultipartContent("related");
            for (int i = 0; i < count; i++)
            {
                var bytes = await GenerateInstance(studyUid, seriesUid, instanceUid, transferSynx: transferSynx);
                multipartContent.Add(new ByteArrayContent(bytes));
            }
            return multipartContent;
        }

        internal static HttpContent GenerateInstancesAsJson(
            int count,
            DicomUID studyUid,
            DicomUID seriesUid = null,
            DicomUID instanceUid = null)
        {
            var jsonArray = new JArray();
            for (int i = 0; i < count; i++)
            {
                var json = GenerateInstancesAsJson(studyUid, seriesUid, instanceUid);
                jsonArray.Add(JToken.Parse(json));
            }
            return new StringContent(jsonArray.ToString(Formatting.Indented), Encoding.UTF8, MimeApplicationDicomJson);
        }

        internal static List<DicomFile> GenerateDicomFiles(int count, DicomUID studyUid)
        {
            var files = new List<DicomFile>();

            for (int i = 0; i < count; i++)
            {
                files.Add(new DicomFile(GenerateDicomDataset(studyUid, null, null, null)));
            }

            return files;
        }

        private static string GenerateInstancesAsJson(DicomUID studyUid, DicomUID seriesUid = null, DicomUID instanceUid = null)
        {
            var dicomDataset = GenerateDicomDataset(studyUid, seriesUid, instanceUid, null);
            return DicomJson.ConvertDicomToJson(dicomDataset);
        }

        private static async Task<byte[]> GenerateInstance(DicomUID studyUid, DicomUID seriesUid = null, DicomUID instanceUid = null, DicomTransferSyntax transferSynx = null)
        {
            var dicomDataset = GenerateDicomDataset(studyUid, seriesUid, instanceUid, transferSynx);
            var dicomFile = new DicomFile(dicomDataset);

            using (var ms = new MemoryStream())
            {
                await dicomFile.SaveAsync(ms);
                return ms.ToArray();
            }
        }

        private static DicomDataset GenerateDicomDataset(DicomUID studyUid, DicomUID seriesUid, DicomUID instanceUid, DicomTransferSyntax transferSynx)
        {
            if (seriesUid is null)
            {
                seriesUid = DicomUIDGenerator.GenerateDerivedFromUUID();
            }

            if (instanceUid is null)
            {
                instanceUid = DicomUIDGenerator.GenerateDerivedFromUUID();
            }

            if (transferSynx is null)
            {
                transferSynx = DicomTransferSyntax.ExplicitVRLittleEndian;
            }

            var dicomDataset = new DicomDataset(transferSynx ?? DicomTransferSyntax.ExplicitVRLittleEndian)
            {
                { DicomTag.PatientID, "TEST" },
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.StudyInstanceUID, studyUid },
                { DicomTag.SeriesInstanceUID, seriesUid },
                { DicomTag.SOPInstanceUID, instanceUid }
            };
            return dicomDataset;
        }

        internal static HttpContent GenerateByteData()
        {
            var multipartContent = new MultipartContent("related");

            var random = new Random();
            var data = new byte[10];
            random.NextBytes(data);
            multipartContent.Add(new ByteArrayContent(data));

            return multipartContent;
        }
    }
}
