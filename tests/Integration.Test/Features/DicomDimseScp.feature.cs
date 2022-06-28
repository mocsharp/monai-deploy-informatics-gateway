﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.9.0.0
//      SpecFlow Generator Version:3.9.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Monai.Deploy.InformaticsGateway.Integration.Test.Features
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public partial class DICOMDIMSESCPServicesFeature : object, Xunit.IClassFixture<DICOMDIMSESCPServicesFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private static string[] featureTags = ((string[])(null));
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "DicomDimseScp.feature"
#line hidden
        
        public DICOMDIMSESCPServicesFeature(DICOMDIMSESCPServicesFeature.FixtureData fixtureData, Monai_Deploy_InformaticsGateway_Integration_Test_XUnitAssemblyFixture assemblyFixture, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Features", "DICOM DIMSE SCP Services", @"    This feature tests the DIMSE services provided by the Informatics Gateway as a SCP.
    Requirements covered:
    - [REQ-DCM-01] MIG SHALL respond to Verification Requests (C-ECHO)
    - [REQ-DCM-02] MIG SHALL respond to DICOM Store Requests (C-STORE)
    - [REQ-DCM-06] MIG SHALL allow users to configure the SCP AE TItle
    - [REQ-DCM-10] MIG MUST accept DICOM data from multiple sources
    - [REQ-FNC-02] MIG SHALL notify other subsystems when data is ready for processing
    - [REQ-FNC-03] MIG SHALL wait for data to arrive before submitting a job
    - [REQ-FNC-04] MIG SHALL make DICOM data available to other subsystems by grouping them into patient, study, or series
    - [REQ-FNC-05] MIG SHALL notify users of system events", ProgrammingLanguage.CSharp, featureTags);
            testRunner.OnFeatureStart(featureInfo);
        }
        
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public void TestInitialize()
        {
        }
        
        public void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Xunit.Abstractions.ITestOutputHelper>(_testOutputHelper);
        }
        
        public void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        public virtual void FeatureBackground()
        {
#line 18
    #line hidden
#line 19
        testRunner.Given("a calling AE Title \'TEST-RUNNER\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
        }
        
        void System.IDisposable.Dispose()
        {
            this.TestTearDown();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Response to C-ECHO-RQ")]
        [Xunit.TraitAttribute("FeatureTitle", "DICOM DIMSE SCP Services")]
        [Xunit.TraitAttribute("Description", "Response to C-ECHO-RQ")]
        public void ResponseToC_ECHO_RQ()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Response to C-ECHO-RQ", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 21
    this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 18
    this.FeatureBackground();
#line hidden
#line 22
        testRunner.Given("a called AE Title named \'C-ECHO-TEST\' that groups by \'0020,000D\' for 5 seconds", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
#line 23
        testRunner.When("a C-ECHO-RQ is sent to \'C-ECHO-TEST\' from \'TEST-RUNNER\' with timeout of 30 second" +
                        "s", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 24
        testRunner.Then("a successful response should be received", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableTheoryAttribute(DisplayName="Respond to C-STORE-RQ and group data by Study Instance UID")]
        [Xunit.TraitAttribute("FeatureTitle", "DICOM DIMSE SCP Services")]
        [Xunit.TraitAttribute("Description", "Respond to C-STORE-RQ and group data by Study Instance UID")]
        [Xunit.TraitAttribute("Category", "messaging_workflow_request")]
        [Xunit.TraitAttribute("Category", "messaging")]
        [Xunit.InlineDataAttribute("MR", "1", new string[0])]
        [Xunit.InlineDataAttribute("CT", "1", new string[0])]
        [Xunit.InlineDataAttribute("MG", "2", new string[0])]
        [Xunit.InlineDataAttribute("US", "1", new string[0])]
        public void RespondToC_STORE_RQAndGroupDataByStudyInstanceUID(string modality, string count, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "messaging_workflow_request",
                    "messaging"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            string[] tagsOfScenario = @__tags;
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            argumentsOfScenario.Add("modality", modality);
            argumentsOfScenario.Add("count", count);
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Respond to C-STORE-RQ and group data by Study Instance UID", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 27
    this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 18
    this.FeatureBackground();
#line hidden
#line 28
        testRunner.Given("a called AE Title named \'C-STORE-STUDY\' that groups by \'0020,000D\' for 3 seconds", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
#line 29
        testRunner.And(string.Format("{0} {1} studies", count, modality), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 30
        testRunner.When("a C-STORE-RQ is sent to \'Informatics Gateway\' with AET \'C-STORE-STUDY\' from \'TEST" +
                        "-RUNNER\' with timeout of 300 seconds", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 31
        testRunner.Then("a successful response should be received", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 32
        testRunner.And(string.Format("{0} workflow requests sent to message broker", count), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 33
        testRunner.And("studies are uploaded to storage service", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 34
        testRunner.And("the temporary data directory has been cleared", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableTheoryAttribute(DisplayName="Respond to C-STORE-RQ and group data by Series Instance UID")]
        [Xunit.TraitAttribute("FeatureTitle", "DICOM DIMSE SCP Services")]
        [Xunit.TraitAttribute("Description", "Respond to C-STORE-RQ and group data by Series Instance UID")]
        [Xunit.TraitAttribute("Category", "messaging_workflow_request")]
        [Xunit.TraitAttribute("Category", "messaging")]
        [Xunit.InlineDataAttribute("MR", "1", "2", new string[0])]
        [Xunit.InlineDataAttribute("CT", "1", "2", new string[0])]
        [Xunit.InlineDataAttribute("MG", "1", "3", new string[0])]
        [Xunit.InlineDataAttribute("US", "1", "2", new string[0])]
        public void RespondToC_STORE_RQAndGroupDataBySeriesInstanceUID(string modality, string study_Count, string series_Count, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "messaging_workflow_request",
                    "messaging"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            string[] tagsOfScenario = @__tags;
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            argumentsOfScenario.Add("modality", modality);
            argumentsOfScenario.Add("study_count", study_Count);
            argumentsOfScenario.Add("series_count", series_Count);
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Respond to C-STORE-RQ and group data by Series Instance UID", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 44
    this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 18
    this.FeatureBackground();
#line hidden
#line 45
        testRunner.Given("a called AE Title named \'C-STORE-SERIES\' that groups by \'0020,000E\' for 3 seconds" +
                        "", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
#line 46
        testRunner.And(string.Format("{0} {1} studies with {2} series per study", study_Count, modality, series_Count), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 47
        testRunner.When("a C-STORE-RQ is sent to \'Informatics Gateway\' with AET \'C-STORE-SERIES\' from \'TES" +
                        "T-RUNNER\' with timeout of 300 seconds", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 48
        testRunner.Then("a successful response should be received", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 49
        testRunner.And(string.Format("{0} workflow requests sent to message broker", series_Count), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 50
        testRunner.And("studies are uploaded to storage service", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 51
        testRunner.And("the temporary data directory has been cleared", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                DICOMDIMSESCPServicesFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                DICOMDIMSESCPServicesFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
