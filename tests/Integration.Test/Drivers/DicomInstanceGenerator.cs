﻿// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    public class DicomInstanceGenerator
    {
        public class StudyGenerationSpecs
        {
            public List<string> StudyInstanceUids { get; set; }
            public int StudyCount { get; set; }
            public int SeriesPerStudyCount { get; set; }
            public int InstancePerSeries { get; set; }
            public int FileCount { get; set; }
            public List<DicomFile> Files { get; set; }

            public int NumberOfExpectedRequests(string grouping) => grouping switch
            {
                "0020,000D" => StudyCount,
                "0020,000E" => StudyCount * SeriesPerStudyCount,
                "stow_none" => 1, // For DICOMweb STOW-RS
                "stow_study" => 1, // For DICOMweb STOW-RS
                _ => throw new ArgumentException($"Grouping '{grouping} not supported.")
            };

            public int NumberOfExpectedFiles(string grouping) => grouping switch
            {
                "0020,000D" => SeriesPerStudyCount * InstancePerSeries,
                "0020,000E" => InstancePerSeries,
                "stow_none" => FileCount, // For DICOMweb STOW-RS
                "stow_study" => SeriesPerStudyCount * InstancePerSeries, // For DICOMweb STOW-RS
                _ => throw new ArgumentException($"Grouping '{grouping} not supported.")
            };
        }

        private const int Rows = 1024;
        private const int Columns = 1024;

        private readonly int _instance;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly DicomDataset _baseDataset;
        private readonly Random _random;

        public DicomInstanceGenerator(ISpecFlowOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _baseDataset = new DicomDataset();
            _random = new Random();
            _instance = _random.Next();
        }

        public DicomInstanceGenerator SetPatient(string patientId = "")
        {
            _baseDataset.AddOrUpdate(DicomTag.PatientID, patientId);
            _baseDataset.AddOrUpdate(DicomTag.PatientName, patientId);
            _baseDataset.AddOrUpdate(DicomTag.AccessionNumber, patientId.Substring(0, Math.Min(patientId.Length, 16)));
            return this;
        }

        public DicomInstanceGenerator GenerateNewStudy(DateTime datetime = default)
        {
            _baseDataset.AddOrUpdate(DicomTag.StudyDate, datetime);
            _baseDataset.AddOrUpdate(DicomTag.StudyTime, datetime);
            _baseDataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            return this;
        }

        public DicomInstanceGenerator GenerateNewSeries()
        {
            _baseDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            _baseDataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            return this;
        }

        public DicomFile GenerateNewInstance(long size, string sopClassUid = "1.2.840.10008.5.1.4.1.1.11.1")
        {
            var dataset = new DicomDataset();
            _baseDataset.CopyTo(dataset);
            dataset.AddOrUpdate(DicomTag.SOPClassUID, sopClassUid)
                   .AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID())
                   .AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value)
                   .AddOrUpdate(DicomTag.PixelRepresentation, (ushort)PixelRepresentation.Unsigned)
                   .AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)PlanarConfiguration.Interleaved)
                   .AddOrUpdate<ushort>(DicomTag.Rows, Rows)
                   .AddOrUpdate<ushort>(DicomTag.Columns, Columns)
                   .AddOrUpdate<ushort>(DicomTag.BitsAllocated, 8)
                   .AddOrUpdate<ushort>(DicomTag.BitsStored, 8)
                   .AddOrUpdate<ushort>(DicomTag.HighBit, 7)
                   .AddOrUpdate<ushort>(DicomTag.SamplesPerPixel, 1);

            var frames = Math.Max(1, size / Rows / Columns);
            var pixelData = DicomPixelData.Create(dataset, true);
            for (var frame = 0; frame < frames; frame++)
            {
                pixelData.AddFrame(new MemoryByteBuffer(GeneratePixelData(Rows, Columns)));
            }
            return new DicomFile(dataset);
        }

        public StudyGenerationSpecs Generate(string patientId, int studiesPerPatient, string modality, StudySpec studySpec) =>
            Generate(patientId, studiesPerPatient, _random.Next(studySpec.SeriesMin, studySpec.SeriesMax), modality, studySpec);

        public StudyGenerationSpecs Generate(string patientId, int studiesPerPatient, int seriesPerStudy, string modality, StudySpec studySpec)
        {
            if (string.IsNullOrWhiteSpace(patientId)) throw new ArgumentNullException(nameof(patientId));
            if (studySpec is null) throw new ArgumentNullException(nameof(studySpec));

            studySpec.InstanceMin.Should().BeGreaterThan(0);
            studySpec.InstanceMax.Should().BeGreaterThan(0);

            var instancesPerSeries = _random.Next(studySpec.InstanceMin, studySpec.InstanceMax);
            var files = new List<DicomFile>();
            var studyInstanceUids = new List<string>();
            DicomFile dicomFile = null;

            var generator = SetPatient(patientId);

            for (var study = 0; study < studiesPerPatient; study++)
            {
                generator.GenerateNewStudy();
                studyInstanceUids.Add(_baseDataset.GetString(DicomTag.StudyInstanceUID));
                for (var series = 0; series < seriesPerStudy; series++)
                {
                    generator.GenerateNewSeries();
                    for (var instance = 0; instance < instancesPerSeries; instance++)
                    {
                        var size = _random.NextLong(studySpec.SizeMinBytes, studySpec.SizeMaxBytes);
                        dicomFile = generator.GenerateNewInstance(size);
                        files.Add(dicomFile);
                    }
                }
                _outputHelper.WriteLine("DICOM Instance: PID={0}, STUDY={1}",
                    dicomFile?.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, "N/A"),
                    dicomFile?.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "N/A"));
            }

            return new StudyGenerationSpecs
            {
                Files = files,
                InstancePerSeries = instancesPerSeries,
                SeriesPerStudyCount = seriesPerStudy,
                StudyCount = studiesPerPatient,
                FileCount = files.Count,
                StudyInstanceUids = studyInstanceUids
            };
        }

        private byte[] GeneratePixelData(int rows, int columns)
        {
            var bytes = new byte[rows * columns];
            _random.NextBytes(bytes);
            return bytes;
        }
    }
}
