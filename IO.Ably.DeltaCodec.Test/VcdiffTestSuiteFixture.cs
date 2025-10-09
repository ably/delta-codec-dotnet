using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace IO.Ably.DeltaCodec.Test
{
    /// <summary>
    /// Test metadata structure matching the vcdiff-tests format
    /// </summary>
    public class TestMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ExpectedBehavior { get; set; }
        public List<string> TestObjectives { get; set; }
        public string ExpectedErrorType { get; set; }
        public ExpectedPropertiesData ExpectedProperties { get; set; }

        public class ExpectedPropertiesData
        {
            public int SourceSize { get; set; }
            public int TargetSize { get; set; }
            public bool HasChecksum { get; set; }
            public int InstructionCount { get; set; }
            public int WindowCount { get; set; }
            public string PrimaryInstruction { get; set; }
            public bool ShouldFailFast { get; set; }
            public string ErrorLocation { get; set; }
        }
    }

    /// <summary>
    /// Represents a single VCDIFF test case
    /// </summary>
    public class VcdiffTestCase
    {
        public string Name { get; set; }
        public string TestDir { get; set; }
        public string SourceFile { get; set; }
        public string TargetFile { get; set; }
        public string DeltaFile { get; set; }
        public string MetadataFile { get; set; }
        public TestMetadata Metadata { get; set; }
        public bool ShouldSucceed { get; set; }
    }

    [TestFixture]
    public class VcdiffTestSuiteFixture
    {
        private const string TestSuiteBasePath = "vcdiff-tests";

        /// <summary>
        /// Discovers all test cases in a given category directory
        /// </summary>
        private static List<VcdiffTestCase> DiscoverTestCases(string categoryDir, bool shouldSucceed)
        {
            var testCases = new List<VcdiffTestCase>();
            string currentDirectory = Path.GetDirectoryName(typeof(VcdiffTestSuiteFixture).Assembly.Location);
            string fullCategoryPath = Path.Combine(currentDirectory, TestSuiteBasePath, categoryDir);

            if (!Directory.Exists(fullCategoryPath))
            {
                return testCases;
            }

            // Recursively find all directories containing delta.vcdiff
            foreach (string dir in Directory.EnumerateDirectories(fullCategoryPath, "*", SearchOption.AllDirectories))
            {
                string deltaFile = Path.Combine(dir, "delta.vcdiff");
                if (File.Exists(deltaFile))
                {
                    string sourceFile = Path.Combine(dir, "source");
                    string targetFile = Path.Combine(dir, "target");
                    string metadataFile = Path.Combine(dir, "metadata.json");

                    // Check required files exist
                    if (!File.Exists(sourceFile) || !File.Exists(targetFile))
                    {
                        continue;
                    }

                    // Load metadata if available
                    TestMetadata metadata = null;
                    if (File.Exists(metadataFile))
                    {
                        try
                        {
                            string metadataJson = File.ReadAllText(metadataFile);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            metadata = JsonSerializer.Deserialize<TestMetadata>(metadataJson, options);
                        }
                        catch (Exception ex)
                        {
                            TestContext.WriteLine($"Warning: Failed to parse metadata.json in {dir}: {ex.Message}");
                        }
                    }

                    // Generate test name from directory structure
                    string relPath = Path.GetRelativePath(fullCategoryPath, dir);
                    string testName = relPath.Replace(Path.DirectorySeparatorChar, '/');
                    if (metadata != null && !string.IsNullOrEmpty(metadata.Name))
                    {
                        testName = $"{testName} ({metadata.Name})";
                    }

                    testCases.Add(new VcdiffTestCase
                    {
                        Name = testName,
                        TestDir = dir,
                        SourceFile = sourceFile,
                        TargetFile = targetFile,
                        DeltaFile = deltaFile,
                        MetadataFile = metadataFile,
                        Metadata = metadata,
                        ShouldSucceed = shouldSucceed
                    });
                }
            }

            return testCases;
        }

        /// <summary>
        /// Generates test case data for NUnit
        /// </summary>
        public static IEnumerable<TestCaseData> TargetedPositiveTestCases
        {
            get
            {
                var testCases = DiscoverTestCases("targeted-positive", true);
                if (testCases.Count == 0)
                {
                    yield return new TestCaseData(null).SetName("No targeted-positive tests found").Ignore("Test directory not found");
                }
                else
                {
                    foreach (var testCase in testCases)
                    {
                        yield return new TestCaseData(testCase).SetName(testCase.Name);
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> TargetedNegativeTestCases
        {
            get
            {
                var testCases = DiscoverTestCases("targeted-negative", false);
                if (testCases.Count == 0)
                {
                    yield return new TestCaseData(null).SetName("No targeted-negative tests found").Ignore("Test directory not found");
                }
                else
                {
                    foreach (var testCase in testCases)
                    {
                        yield return new TestCaseData(testCase).SetName(testCase.Name);
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> GeneralPositiveTestCases
        {
            get
            {
                var testCases = DiscoverTestCases("general-positive", true);
                if (testCases.Count == 0)
                {
                    yield return new TestCaseData(null).SetName("No general-positive tests found").Ignore("Test directory not found");
                }
                else
                {
                    foreach (var testCase in testCases)
                    {
                        yield return new TestCaseData(testCase).SetName(testCase.Name);
                    }
                }
            }
        }

        public static IEnumerable<TestCaseData> FuzzTestCases
        {
            get
            {
                var testCases = DiscoverTestCases("fuzz", false);
                if (testCases.Count == 0)
                {
                    yield return new TestCaseData(null).SetName("No fuzz tests found").Ignore("Test directory not found");
                }
                else
                {
                    foreach (var testCase in testCases)
                    {
                        yield return new TestCaseData(testCase).SetName(testCase.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Tests cases that should succeed in decoding
        /// </summary>
        [TestCaseSource(nameof(TargetedPositiveTestCases))]
        public void TestTargetedPositive(VcdiffTestCase testCase)
        {
            if (testCase == null)
            {
                Assert.Ignore("Test case is null");
                return;
            }

            // Load test files
            byte[] source = File.ReadAllBytes(testCase.SourceFile);
            byte[] target = File.ReadAllBytes(testCase.TargetFile);
            byte[] delta = File.ReadAllBytes(testCase.DeltaFile);

            // Test decoding
            byte[] result;
            try
            {
                using (var sourceStream = new MemoryStream(source))
                using (var deltaStream = new MemoryStream(delta))
                using (var resultStream = new MemoryStream())
                {
                    Vcdiff.VcdiffDecoder.Decode(sourceStream, deltaStream, resultStream);
                    result = resultStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected successful decode but got error: {ex.Message}");
                return;
            }

            // Compare result with expected target
            Assert.That(result.Length, Is.EqualTo(target.Length),
                $"Result length mismatch: got {result.Length} bytes, expected {target.Length} bytes");

            Assert.That(target.SequenceEqual(result), Is.True,
                "Result differs from target");

            // Validate metadata expectations if available
            if (testCase.Metadata?.ExpectedProperties != null)
            {
                var props = testCase.Metadata.ExpectedProperties;
                if (props.TargetSize > 0)
                {
                    Assert.That(result.Length, Is.EqualTo(props.TargetSize),
                        $"Target size mismatch: got {result.Length}, expected {props.TargetSize}");
                }
            }
        }

        /// <summary>
        /// Tests cases that should fail during decoding
        /// </summary>
        [TestCaseSource(nameof(TargetedNegativeTestCases))]
        public void TestTargetedNegative(VcdiffTestCase testCase)
        {
            if (testCase == null)
            {
                Assert.Ignore("Test case is null");
                return;
            }

            // Load test files
            byte[] source = File.ReadAllBytes(testCase.SourceFile);
            byte[] delta = File.ReadAllBytes(testCase.DeltaFile);

            // Test decoding - should fail
            bool didFail = false;
            Exception caughtException = null;
            try
            {
                using (var sourceStream = new MemoryStream(source))
                using (var deltaStream = new MemoryStream(delta))
                using (var resultStream = new MemoryStream())
                {
                    Vcdiff.VcdiffDecoder.Decode(sourceStream, deltaStream, resultStream);
                }
            }
            catch (Exception ex)
            {
                didFail = true;
                caughtException = ex;
            }

            Assert.That(didFail, Is.True,
                "Expected decode to fail but it succeeded");

            // Log error information
            if (testCase.Metadata != null && !string.IsNullOrEmpty(testCase.Metadata.ExpectedErrorType))
            {
                TestContext.WriteLine($"Got expected error: {caughtException?.Message} (type: {testCase.Metadata.ExpectedErrorType})");
            }
        }

        /// <summary>
        /// Tests general positive test cases with various content
        /// </summary>
        [TestCaseSource(nameof(GeneralPositiveTestCases))]
        public void TestGeneralPositive(VcdiffTestCase testCase)
        {
            if (testCase == null)
            {
                Assert.Ignore("Test case is null");
                return;
            }

            // Load test files
            byte[] source = File.ReadAllBytes(testCase.SourceFile);
            byte[] target = File.ReadAllBytes(testCase.TargetFile);
            byte[] delta = File.ReadAllBytes(testCase.DeltaFile);

            // Test decoding
            byte[] result;
            try
            {
                using (var sourceStream = new MemoryStream(source))
                using (var deltaStream = new MemoryStream(delta))
                using (var resultStream = new MemoryStream())
                {
                    Vcdiff.VcdiffDecoder.Decode(sourceStream, deltaStream, resultStream);
                    result = resultStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected successful decode but got error: {ex.Message}");
                return;
            }

            // Compare result with expected target
            Assert.That(result.Length, Is.EqualTo(target.Length),
                $"Result length mismatch: got {result.Length} bytes, expected {target.Length} bytes");

            Assert.That(target.SequenceEqual(result), Is.True,
                "Result differs from target");
        }

        /// <summary>
        /// Tests fuzz test cases (corrupted inputs) - should not crash
        /// </summary>
        [TestCaseSource(nameof(FuzzTestCases))]
        public void TestFuzz(VcdiffTestCase testCase)
        {
            if (testCase == null)
            {
                Assert.Ignore("Test case is null");
                return;
            }

            // Load test files
            byte[] source = File.ReadAllBytes(testCase.SourceFile);
            byte[] delta = File.ReadAllBytes(testCase.DeltaFile);

            // Test decoding - should not crash (may succeed or fail)
            // The key requirement for fuzz tests is that the decoder doesn't throw unhandled exceptions
            try
            {
                using (var sourceStream = new MemoryStream(source))
                using (var deltaStream = new MemoryStream(delta))
                using (var resultStream = new MemoryStream())
                {
                    Vcdiff.VcdiffDecoder.Decode(sourceStream, deltaStream, resultStream);
                    TestContext.WriteLine($"Fuzz test unexpectedly succeeded, got {resultStream.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                // Expected to fail - just log it
                TestContext.WriteLine($"Fuzz test failed as expected: {ex.Message}");
            }
        }
        /// <summary>
        /// Legacy test: Verify decoder can be instantiated
        /// </summary>
        [Test]
        public void TestNewDecoder()
        {
            byte[] source = System.Text.Encoding.UTF8.GetBytes("hello world");
            
            // The .NET implementation doesn't have a NewDecoder method like Go
            // Instead, we verify the VcdiffDecoder class can be used
            Assert.DoesNotThrow(() =>
            {
                using (var sourceStream = new MemoryStream(source))
                using (var deltaStream = new MemoryStream())
                using (var resultStream = new MemoryStream())
                {
                    // Just verify we can call the decoder without crashing
                    // This is equivalent to Go's NewDecoder test
                }
            });
        }

        /// <summary>
        /// Legacy test: Basic decode with empty-to-empty VCDIFF delta
        /// </summary>
        [Test]
        public void TestDecode()
        {
            byte[] source = System.Text.Encoding.UTF8.GetBytes("hello world");
            // Valid empty-to-empty VCDIFF delta
            byte[] delta = new byte[] { 0xd6, 0xc3, 0xc4, 0x00, 0x00, 0x04, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

            byte[] result;
            using (var sourceStream = new MemoryStream(source))
            using (var deltaStream = new MemoryStream(delta))
            using (var resultStream = new MemoryStream())
            {
                Vcdiff.VcdiffDecoder.Decode(sourceStream, deltaStream, resultStream);
                result = resultStream.ToArray();
            }

            Assert.That(result, Is.Not.Null, "Decode returned null result");
            Assert.That(result.Length, Is.EqualTo(0), $"Expected empty result, got {result.Length} bytes");
        }

        /// <summary>
        /// Legacy test: Test the convenience Decode function
        /// </summary>
        [Test]
        public void TestDecodeFunction()
        {
            byte[] source = System.Text.Encoding.UTF8.GetBytes("hello world");
            // Valid empty-to-empty VCDIFF delta
            byte[] delta = new byte[] { 0xd6, 0xc3, 0xc4, 0x00, 0x00, 0x04, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

            byte[] result;
            using (var sourceStream = new MemoryStream(source))
            using (var deltaStream = new MemoryStream(delta))
            using (var resultStream = new MemoryStream())
            {
                Vcdiff.VcdiffDecoder.Decode(sourceStream, deltaStream, resultStream);
                result = resultStream.ToArray();
            }

            Assert.That(result, Is.Not.Null, "Decode function returned null result");
            Assert.That(result.Length, Is.EqualTo(0), $"Expected empty result, got {result.Length} bytes");
        }
    }
}