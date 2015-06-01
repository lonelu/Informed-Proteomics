﻿using System;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.TopDown.Scoring;
using NUnit.Framework;

namespace InformedProteomics.Test.FunctionalTests
{
    [TestFixture]
    class TestReadingProMex
    {
        [Test]
        public void TestReadingProMexFile()
        {
            const string rawFilePath = @"H:\Research\Weijun_TopDown\raw\UC4_Intact_plasmaTest_90_6May15_Bane_14-09-01RZ.raw";
            var run = PbfLcMsRun.GetLcMsRun(rawFilePath);

            const string promexFileName = @"H:\Research\Weijun_TopDown\raw\UC4_Intact_plasmaTest_90_6May15_Bane_14-09-01RZ.ms1ft";
            Console.Write("Reading ProMex results...");
            var ms1Filter = new Ms1FtFilter(run, new Tolerance(10), promexFileName);
            Console.WriteLine(string.Join(",", ms1Filter.GetMatchingMs2ScanNums(10266.03)));
        }
    }
}