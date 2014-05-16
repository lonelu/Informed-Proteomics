﻿using System;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.TopDown.Scoring;
using NUnit.Framework;

namespace InformedProteomics.Test.FunctionalTests
{
    [TestFixture]
    class TestTopDownScoring
    {
        [Test]
        public void PrintAllScorers()
        {
            //            Console.WriteLine(Convert.ToDouble("0"));
            var scoringModel = new LikelihoodScoringModel(@"C:\cygwin\home\kims336\Data\TopDown\raw\CorrScores_SBEP.txt");
            scoringModel.PrintAllScores();
        }

        [Test]
        public void TestLikelihoodScorer()
        {
//            Console.WriteLine(Convert.ToDouble("0"));
            var scoringModel = new LikelihoodScoringModel(@"C:\cygwin\home\kims336\Data\TopDown\raw\CorrScores_Filtration_2.txt");
            Console.WriteLine("Score: {0}", scoringModel.GetScore(BaseIonType.Y, 0.99, 1200));
        }

        [Test]
        public void TestMatchedPeakCounter()
        {
            // Parameters
            var precursorIonTolerance = new Tolerance(15);
            var productIonTolerance = new Tolerance(15);

            var sw = new System.Diagnostics.Stopwatch();

            var aaSet = new AminoAcidSet();

            const string protAnnotation = "_.MFQQEVTITAPNGLHTRPAAQFVKEAKGFTSEITVTSNGKSASAKSLFKLQTLGLTQGTVVTISAEGEDEQKAVEHLVKLMAELE._";

            // Create a sequence graph
            var seqGraph = SequenceGraph.CreateGraph(aaSet, protAnnotation);
            if (seqGraph == null)
            {
                Console.WriteLine("Invalid sequence: {0}", protAnnotation);
                return;
            }

            const string specFilePath = @"\\protoapps\UserData\Sangtae\TestData\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            sw.Start();
            var precursorFilter = new Ms1ContainsIonFilter(run, precursorIonTolerance);

            var seqCompositionArr = seqGraph.GetSequenceCompositions();
            Console.WriteLine("Length: {0}\tNumCompositions: {1}", protAnnotation.Length - 4, seqCompositionArr.Length);

            const int charge = 6;
            const int modIndex = 0;
            const int ms2ScanNum = 4448;

            var seqComposition = seqCompositionArr[modIndex];
            var peptideComposition = seqComposition + Composition.H2O;
            peptideComposition.GetIsotopomerEnvelope();

            Console.WriteLine("Composition: {0}, AveragineMass: {1}", seqComposition, seqComposition.Mass);
            seqGraph.SetSink(modIndex);

            var precursorIon = new Ion(peptideComposition, charge);

            Assert.True(precursorFilter.IsValid(precursorIon, ms2ScanNum));

            var spec = run.GetSpectrum(ms2ScanNum) as ProductSpectrum;
            Assert.True(spec != null);

            var scorer = new MatchedPeakCounter(spec, productIonTolerance, 1, 10);
            var score = seqGraph.GetScore(charge, scorer);

            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", protAnnotation, charge, precursorIon.GetMostAbundantIsotopeMz(), ms2ScanNum, score);

            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }

        [Test]
        public void TestCosineScoring()
        {
            const string protAnnotation = "_.GTVPQKHTPPVAMPGPQIMAVLGKTVKRGFQAHPELFLGAITAANQMVQKTGVVDQGKAAGVGREAVPAAVNIADPGAEGL._";
            const int charge = 11;
            const int ms2ScanNum = 2798;

            // Parameters
            var precursorIonTolerance = new Tolerance(15);
            var productIonTolerance = new Tolerance(15);

            var sw = new System.Diagnostics.Stopwatch();

            var aaSet = new AminoAcidSet();

            // Create a sequence graph
            var seqGraph = SequenceGraph.CreateGraph(aaSet, protAnnotation);
            if (seqGraph == null)
            {
                Console.WriteLine("Invalid sequence: {0}", protAnnotation);
                return;
            }

            const string specFilePath = @"\\protoapps\UserData\Sangtae\TestData\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            sw.Start();
            var seqCompositionArr = seqGraph.GetSequenceCompositions();
            Console.WriteLine("Length: {0}\tNumCompositions: {1}", protAnnotation.Length - 4, seqCompositionArr.Length);

            const int modIndex = 0;
            var scoringModel = new LikelihoodScoringModel(@"C:\cygwin\home\kims336\Data\TopDown\raw\cidCosineMatches.txt");

            var seqComposition = seqCompositionArr[modIndex];
            var peptideComposition = seqComposition + Composition.H2O;
            peptideComposition.GetIsotopomerEnvelope();

            Console.WriteLine("Composition: {0}, AveragineMass: {1}", seqComposition, seqComposition.Mass);
            seqGraph.SetSink(modIndex);

            var precursorIon = new Ion(peptideComposition, charge);

            //                                 if (run.CheckMs1Signature(precursorIon, ms2ScanNum, precursorTolerance) == false)

            //Assert.True(precursorFilter.GetMs2Matches(precursorIon, ms2ScanNum));
            Assert.True(run.CheckMs1Signature(precursorIon, ms2ScanNum, precursorIonTolerance));

            var spec = run.GetSpectrum(ms2ScanNum) as ProductSpectrum;
            Assert.True(spec != null);

            var scorer = new LikelihoodScorer(scoringModel, spec, productIonTolerance, 1, 10);
            var score = seqGraph.GetScore(charge, scorer);

            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", protAnnotation, charge, precursorIon.GetMostAbundantIsotopeMz(), ms2ScanNum, score);

            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }

        [Test]
        public void TestCorrMatchedPeakCounter()
        {
            // Parameters
            var precursorIonTolerance = new Tolerance(10);
            var productIonTolerance = new Tolerance(10);

            var sw = new System.Diagnostics.Stopwatch();

            var aaSet = new AminoAcidSet();

            const string protAnnotation = "_.TMNITSKQMEITPAIRQHVADRLAKLEKWQTHLINPHIILSKEPQGFIADATINTPNGHLVASAKHEDMYTAINELINKLERQLNKVQHKGEAR._";

            // Create a sequence graph
            var seqGraph = SequenceGraph.CreateGraph(aaSet, protAnnotation);
            if (seqGraph == null)
            {
                Console.WriteLine("Invalid sequence: {0}", protAnnotation);
                return;
            }

            const string specFilePath = @"\\protoapps\UserData\Sangtae\TestData\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            sw.Start();
            var precursorFilter = new Ms1ContainsIonFilter(run, precursorIonTolerance);

            var seqCompositionArr = seqGraph.GetSequenceCompositions();
            Console.WriteLine("Length: {0}\tNumCompositions: {1}", protAnnotation.Length - 4, seqCompositionArr.Length);

            const int charge = 9;
            const int modIndex = 0;
            const int ms2ScanNum = 3633;

            var seqComposition = seqCompositionArr[modIndex];
            var peptideComposition = seqComposition + Composition.H2O;
            peptideComposition.GetIsotopomerEnvelope();

            Console.WriteLine("Composition: {0}, AveragineMass: {1}", seqComposition, seqComposition.Mass);
            seqGraph.SetSink(modIndex);

            var precursorIon = new Ion(peptideComposition, charge);

            Assert.True(precursorFilter.IsValid(precursorIon, ms2ScanNum));

            var spec = run.GetSpectrum(ms2ScanNum) as ProductSpectrum;
            Assert.True(spec != null);

            //var scorer = new MatchedPeakCounter(spec, productIonTolerance, 1, 10);
            var scorer = new CorrMatchedPeakCounter(spec, productIonTolerance, 1, 10);
            var score = seqGraph.GetScore(charge, scorer);

            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", protAnnotation, charge, precursorIon.GetMostAbundantIsotopeMz(), ms2ScanNum, score);

            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }
    }
}
