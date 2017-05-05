﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using InformedProteomics.Backend.Utils;
using InformedProteomics.Tests.Base;
using NUnit.Framework;

namespace InformedProteomics.Tests.FunctionalTests
{
    [TestFixture]
    public class TestFdrCalculation
    {
        [Test]
        public void TestIcTopDownFdr()
        {
            var methodName = MethodBase.GetCurrentMethod().Name;
            Utils.ShowStarting(methodName);

            var targetResultPath = Utils.GetTestFile(methodName, @"\\proto-2\UnitTest_Files\InformedProteomics_TestFiles\IdFiles\QC_Shew_Intact_26Sep14_Bane_C2Column3_IcTarget.tsv");

            var decoyResultPath = Utils.GetTestFile(methodName, @"\\proto-2\UnitTest_Files\InformedProteomics_TestFiles\IdFiles\QC_Shew_Intact_26Sep14_Bane_C2Column3_IcDecoy.tsv");

            if (targetResultPath.DirectoryName == null)
                Assert.Ignore("Cannot determine the parent directory of " + targetResultPath.FullName);

            var tdaResultPath = Path.Combine(targetResultPath.DirectoryName, "QC_Shew_Intact_26Sep14_Bane_C2Column3_result.tsv");

            //const string decoyResultPath = @"H:\Research\Charles\TopDown\Mod_M1\SBEP_STM_001_02272012_Aragon_IcDecoy.tsv";
            //const string tdaResultPath = @"H:\Research\Charles\TopDown\Mod_M1\SBEP_STM_001_02272012_Aragon_IcTda.tsv";
            //const string targetResultPath = @"C:\cygwin\home\kims336\Data\TopDown\raw\SBEP_STM_001_02272012_Aragon.icresult";
            //const string decoyResultPath = @"C:\cygwin\home\kims336\Data\TopDown\raw\SBEP_STM_001_02272012_Aragon.decoy.icresult";
            var fdrCalculator = new FdrCalculator(targetResultPath.FullName, decoyResultPath.FullName);
            if (fdrCalculator.HasError())
            {
                throw new Exception(@"Error computing FDR: " + fdrCalculator.ErrorMessage);
            }

            fdrCalculator.WriteTo(tdaResultPath);

            using (var reader = new StreamReader(new FileStream(tdaResultPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (reader.EndOfStream)
                    Assert.Fail("Result file is empty: " + tdaResultPath);

                var headerLine = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(headerLine))
                    Assert.Fail("Header line is empty: " + tdaResultPath);

                var headerColumns = headerLine.Split('\t');

                if (headerColumns.Length < 21)
                {
                    Assert.Fail("Header line col count is less than 21: " + tdaResultPath);
                }

                var headerMap = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

                for (var i = 0; i < headerColumns.Length; i++)
                {
                    headerMap.Add(headerColumns[i], i);
                }

                Console.WriteLine("Headers: " + string.Join("  ", headerColumns));

                if (!headerMap.TryGetValue("QValue", out var qvalueColIndex))
                {
                    Assert.Fail("QValue not found in header line: " + tdaResultPath);
                }

                if (reader.EndOfStream)
                    Assert.Fail("Result file has a header line but no results: " + tdaResultPath);

                var qValueByScan = new Dictionary<int, string>();

                while (!reader.EndOfStream)
                {
                    var result = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(result))
                        continue;

                    var dataColumns = result.Split('\t');

                    if (dataColumns.Length < headerMap.Count)
                    {
                        Assert.Fail("Incomplete result line: " + result);
                    }

                    if (!int.TryParse(dataColumns[0], out var scanNumber))
                    {
                        Assert.Fail("Scan number is non-numeric: " + dataColumns[0]);
                    }

                    qValueByScan.Add(scanNumber, dataColumns[qvalueColIndex]);
                }

                Console.WriteLine("Result count: " + 2655);

                Assert.AreEqual(2655, qValueByScan.Count, "Result count {0} does not match expected count", qValueByScan.Count);

                VerifyQValue(tdaResultPath, qValueByScan, 1808, 9.99e-308);
                VerifyQValue(tdaResultPath, qValueByScan, 2565, 1.323038e-38);
                VerifyQValue(tdaResultPath, qValueByScan, 1682, 1.912647e-12);
                VerifyQValue(tdaResultPath, qValueByScan, 3045, 0.010666);
                VerifyQValue(tdaResultPath, qValueByScan, 2668, 0.113394);

            }
            Console.WriteLine(@"Done, see " + tdaResultPath);
        }

        private void VerifyQValue(string tdaResultPath, IReadOnlyDictionary<int, string> qValueByScan, int scanNumber, double expectedValue)
        {
            if (!qValueByScan.TryGetValue(scanNumber, out var qValueText))
                Assert.Fail("Scan number {0} not found in results: {1}", scanNumber, tdaResultPath);

            if (!double.TryParse(qValueText, out var qValue))
                Assert.Fail("QValue not numeric for scan {0}: {1}", scanNumber, tdaResultPath);

            var tolerance = expectedValue / 1000;
            if (tolerance < double.Epsilon)
                tolerance = double.Epsilon;

            Console.WriteLine("QValue for scan {0} is {1:E2}", scanNumber, qValue);

            Assert.AreEqual(expectedValue, qValue, tolerance, "Unexpected QValue for scan {0} in {1}", scanNumber, tdaResultPath);

        }

        [Ignore("File Missing, test obsolete, or long test")]
        public void MergeTargetDecoyFiles()
        {
            const string dir = @"C:\cygwin\home\kims336\Data\TopDown\raw\Cache";
            var rawFileNames = new HashSet<string>();
            foreach (var f in Directory.GetFiles(dir, "*.icresult"))
            {
                rawFileNames.Add(f.Substring(0, f.IndexOf('.')));
            }

            foreach (var rawFileName in rawFileNames)
            {
                var targetResultFilePath = rawFileName + ".icresult";
                var decoyResultFilePath = rawFileName + ".decoy.icresult";
                var mergedResultFilePath = rawFileName + ".tsv";

                Console.Write(@"Creating {0}...", mergedResultFilePath);
                var fdrCalculator = new FdrCalculator(targetResultFilePath, decoyResultFilePath);
                if (fdrCalculator.HasError())
                {
                    throw new Exception(@"Error computing FDR: " + fdrCalculator.ErrorMessage);
                }

                fdrCalculator.WriteTo(mergedResultFilePath);
                Console.WriteLine(@"Done");
            }
        }
    }
}
