using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

namespace MiscUtil.Compression.Vcdiff.Test
{
    [TestFixture]
    public class VcdiffDecoderFixture
    {
        public static IEnumerable<TestCaseData> XdeltaTestCases
        {
            get
            {
                string currentDirectory = Path.GetDirectoryName(typeof(VcdiffDecoderFixture).Assembly.Location);
                string xdeltaTestDataPath = "TestData\\xdelta";
                foreach (string dir in Directory.EnumerateDirectories(Path.Combine(currentDirectory, xdeltaTestDataPath)))
                {
                    yield return new TestCaseData(dir).SetArgDisplayNames(Path.Combine(xdeltaTestDataPath, Path.GetFileName(dir)));
                }
            }
        }

        [TestCaseSource(typeof(VcdiffDecoderFixture), "XdeltaTestCases")]
        public void DecodeXdeltaPatch(string testCasePath)
        {
            string dictionaryPath = Path.Combine(testCasePath, "dictionary");
            string deltaPath = Path.Combine(testCasePath, "delta");
            string targetPath = Path.Combine(testCasePath, "target");
            byte[] decoded;
            try
            {
                using (FileStream dictionary = File.OpenRead(dictionaryPath))
                using (FileStream delta = File.OpenRead(deltaPath))
                using (MemoryStream decodedStream = new MemoryStream())
                {
                    VcdiffDecoder.Decode(dictionary, delta, decodedStream);
                    decoded = decodedStream.ToArray();
                }
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception during delta application - " + e.Message);
                return;
            }

            byte[] target = File.ReadAllBytes(targetPath);
            Assert.IsTrue(target.SequenceEqual(decoded), "Delta applicaiton result does not match the expected target file.");
        }
    }
}
