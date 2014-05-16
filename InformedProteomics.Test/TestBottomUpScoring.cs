﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.Database;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using InformedProteomics.BottomUp.Scoring;
using InformedProteomics.DIA.Search;
using InformedProteomics.TopDown.Scoring;
using NUnit.Framework;

namespace InformedProteomics.Test
{
    [TestFixture]
    public class TestBottomUpScoring
    {
        [Test]
        public void TestLogLikelihoodScoring()
        {
            const string specFilePath = @"C:\cygwin\home\kims336\Data\QCShewQE\QC_Shew_13_04_A_17Feb14_Samwise_13-07-28.raw";
            const string seqStr = "IAHESDDEKGHAAK";
            var composition = Composition.Parse("C(62) H(98) N(20) O(24) S(0)");
            const int charge = 4;
            const int ms2ScanNum = 12901;

            var aaSet = new AminoAcidSet();

            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 0);
            var scorer = new InformedBottomUpScorer(run, aaSet, 1, 2, new Tolerance(10));
            var refinedScore = scorer.GetScores(AminoAcid.PeptideNTerm, seqStr, AminoAcid.PeptideCTerm, composition,
                charge, ms2ScanNum);

            Console.WriteLine("RefinedScores: {0}", refinedScore.Score);            
        }

        [Test]
        public void TestInitialScoring()
        {
            const string icResultPath = @"C:\cygwin\home\kims336\Data\QCShewQE\Ic_NTT2_03_NoMod_NoRescoring\QC_Shew_13_04_A_17Feb14_Samwise_13-07-28_IcTarget.tsv";
            var icParser = new TsvFileParser(icResultPath);
            var icScans = icParser.GetData("Scan").Select(s => Convert.ToInt32(s)).ToArray();
            var icPeptides = icParser.GetData("Sequence");
            var icScore = icParser.GetData("Score").Select(s => Convert.ToInt32(s)).ToArray();
            var map = new Dictionary<string, int>();
            for (var i = 0; i < icParser.NumData; i++)
            {
                map.Add(icScans[i]+":"+icPeptides[i], icScore[i]);
            }

            const string msgfPlusResulPath = @"C:\cygwin\home\kims336\Data\QCShewQE\NoMod.tsv";
            var msgfPlusResults = new MsGfResults(msgfPlusResulPath);
            var matches = msgfPlusResults.GetMatchesAtPsmFdr(0.01);
            //Console.WriteLine("NumMatches: {0}", matches.Count);
            Console.WriteLine("ScanNum\tPeptide\tSpecEValue\tIcScore");
            foreach (var match in matches)
            {
                var scanNum = match.ScanNum;
                var peptide = match.Peptide;
                var specEValue = match.SpecEValue;
                int score;
                if (!map.TryGetValue(scanNum + ":" + peptide, out score)) score = -1;
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", scanNum, peptide, specEValue, score);
            }
        }

        [Test]
        public void TestMs1Filter()
        {
            const string msgfPlusResulPath = @"C:\cygwin\home\kims336\Data\QCShewQE\NoMod.tsv";
            var msgfPlusResults = new MsGfResults(msgfPlusResulPath);

            const string specFilePath = @"C:\cygwin\home\kims336\Data\QCShewQE\QC_Shew_13_04_A_17Feb14_Samwise_13-07-28.raw";
            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 0);
            var ms1Filter = new Ms1IsotopeAndChargeCorrFilter(run, new Tolerance(10), 1, 4,
                400, 5000, 0.3, 0, 0);

            var matches = msgfPlusResults.GetMatchesAtPsmFdr(0.01);
            var aminoAcidSet = new AminoAcidSet();
            var numPsms = 0;
            var numSurvived = 0;
            Console.WriteLine("ScanNum\tPeptide\tSpecEValue\tFilter");
            foreach (var match in matches)
            {
                var scanNum = match.ScanNum;
                var peptide = match.Peptide;
                var specEValue = match.SpecEValue;
                var peptideMass = (new Sequence(peptide, aminoAcidSet).Composition + Composition.H2O).Mass;
                var survive = ms1Filter.GetMatchingMs2ScanNums(peptideMass).Contains(scanNum) ? 1 : 0;
                ++numPsms;
                numSurvived += survive;
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", scanNum, peptide, specEValue, survive);
            }
            Console.WriteLine("SuccessRage: {0}, {1}/{2}", numSurvived/(float)numPsms, numSurvived, numPsms);
        }

        [Test]
        public void TestPsm()
        {
            const string specFilePath = @"C:\cygwin\home\kims336\Data\QCShewQE\QC_Shew_13_04_A_17Feb14_Samwise_13-07-28.raw";
            const string annotation = "K.AGGYDNHHPR.S";
            const int charge = 3;
            const int ms2ScanNum = 879;

            var aaSet = new AminoAcidSet();

            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 0, 0);
            var ms2Scorer = new ProductScorerBasedOnDeconvolutedSpectra(run, 1, 2, 10, 0, 1.1);
            ms2Scorer.DeconvoluteProductSpectra();
            var scorer = ms2Scorer.GetMs2Scorer(ms2ScanNum);

            var graph = SequenceGraph.CreateGraph(aaSet, annotation);
            graph.SetSink(0);
            var score = graph.GetScore(charge, scorer);
            Console.WriteLine("Fast search score: " + score);
            var composition = graph.GetSinkSequenceCompositionWithH2O();

            var informedScorer = new InformedTopDownScorer(run, aaSet, 1, 15, new Tolerance(10));
            var refinedScore = informedScorer.GetScores(AminoAcid.PeptideNTerm, SimpleStringProcessing.GetStringBetweenDots(annotation), AminoAcid.PeptideCTerm
                , composition, charge, ms2ScanNum);
            Console.WriteLine("RefinedScores: {0}", refinedScore);
        }

        [Test]
        public void TestPeptideLevelStats()
        {
            const string resultDir = @"D:\Research\Data\UW\QExactive\Ic_NTT2_03";
            var targetData = new List<string>();
            foreach (var specFilePath in Directory.GetFiles(resultDir, "*DIA*775to900*IcTarget.tsv"))
            {
                targetData.AddRange(File.ReadAllLines(specFilePath).Skip(1));
            }

            var decoyData = new List<string>();
            foreach (var specFilePath in Directory.GetFiles(resultDir, "*DIA*775to900*IcDecoy.tsv"))
            {
                decoyData.AddRange(File.ReadAllLines(specFilePath).Skip(1));
            }

            const string headerStr =
                "Scan\tPre\tSequence\tPost\tModifications\tComposition\tProteinName" +
                "\tProteinDesc\tProteinLength\tStart\tEnd\tCharge\tMostAbundantIsotopeMz" +
                "\tMass\tScore\tIsotopeCorrPrevMs1\tIsotopeCorrNextMs1\tCorrMostAbundantPlusOneIsotope";
            var header = headerStr.Split('\t').ToList();
            if (targetData.Count <= 1 || decoyData.Count <= 1) return;

            var scoreIndex = header.IndexOf("Ms2Score");
            if (scoreIndex < 0) scoreIndex = header.IndexOf("Score");
            if (scoreIndex < 0) scoreIndex = header.IndexOf("#MatchedFragments");
            var sequenceIndex = header.IndexOf("Sequence");
            var preIndex = header.IndexOf("Pre");
            var postIndex = header.IndexOf("Post");
            var proteinIndex = header.IndexOf("ProteinName");
            if (scoreIndex < 0 || sequenceIndex < 0 || preIndex < 0 || postIndex < 0 || proteinIndex < 0) return;

            var target = new Dictionary<string, double>();
            foreach (var r in targetData)
            {
                var columns = r.Split('\t');
                var peptide = 0 + columns[sequenceIndex];
                var curScore = Convert.ToDouble(columns[scoreIndex]);
                double score;
                if (target.TryGetValue(peptide, out score)) target[peptide] = Math.Max(score, curScore);
                else target.Add(peptide, curScore);
            }

            var decoy = new Dictionary<string, double>();
            foreach (var r in decoyData)
            {
                var columns = r.Split('\t');
                var peptide = 1 + columns[sequenceIndex];
                var curScore = Convert.ToDouble(columns[scoreIndex]);
                double score;
                if (decoy.TryGetValue(peptide, out score)) decoy[peptide] = Math.Max(score, curScore);
                else decoy.Add(peptide, curScore);
            }

            var distinctSortedDecoyFirst = decoy.Concat(target).OrderByDescending(keyValuePair => keyValuePair.Value).ToArray();

            // Calculate q values
            var numDecoy = 0;
            var numTarget = 0;
            var fdr = new double[distinctSortedDecoyFirst.Length];
            for (var i = 0; i < distinctSortedDecoyFirst.Length; i++)
            {
                var r = distinctSortedDecoyFirst[i].Key;  // peptide
                if (r.StartsWith("0")) numTarget++;
                else numDecoy++;
                fdr[i] = numDecoy / (double)numTarget;
            }

            var pepQValue = new double[fdr.Length];
            pepQValue[fdr.Length - 1] = fdr[fdr.Length - 1];
            for (var i = fdr.Length - 2; i >= 0; i--)
            {
                pepQValue[i] = Math.Min(pepQValue[i + 1], fdr[i]);
            }

            var numIdentifiedPeptidesDecoyFirst = 0;
            for (var i = 0; i < distinctSortedDecoyFirst.Length; i++)
            {
                if(pepQValue[i] < 0.01) ++numIdentifiedPeptidesDecoyFirst;
            }

            var distinctSortedTargetFirst = target.Concat(decoy).OrderByDescending(keyValuePair => keyValuePair.Value).ToArray();

            // Calculate q values
            numDecoy = 0;
            numTarget = 0;
            fdr = new double[distinctSortedTargetFirst.Length];
            for (var i = 0; i < distinctSortedTargetFirst.Length; i++)
            {
                var r = distinctSortedTargetFirst[i].Key;  // peptide
                if (r.StartsWith("0")) numTarget++;
                else numDecoy++;
                fdr[i] = numDecoy / (double)numTarget;
            }

            pepQValue = new double[fdr.Length];
            pepQValue[fdr.Length - 1] = fdr[fdr.Length - 1];
            for (var i = fdr.Length - 2; i >= 0; i--)
            {
                pepQValue[i] = Math.Min(pepQValue[i + 1], fdr[i]);
            }

            var numIdentifiedPeptidesTargetFirst = 0;
            for (var i = 0; i < distinctSortedTargetFirst.Length; i++)
            {
                if (pepQValue[i] < 0.01) ++numIdentifiedPeptidesTargetFirst;
            }

            Console.WriteLine("NumPeptides: {0}, {1}, {2}", 
                numIdentifiedPeptidesDecoyFirst, 
                numIdentifiedPeptidesTargetFirst,
                0.5*(numIdentifiedPeptidesDecoyFirst + numIdentifiedPeptidesTargetFirst)
                );
        }

        [Test]
        public void CompareIpaIc()
        {
            const string resultDir = @"D:\Research\Data\UW\QExactive\Ic_NTT2_03";
            var targetPeptides = new HashSet<string>();
            foreach (var icResultFilePath in Directory.GetFiles(resultDir, "*DIA*IcTarget.tsv"))
            {
                var icParser = new TsvFileParser(icResultFilePath);
                foreach (var peptide in icParser.GetData("Sequence")) targetPeptides.Add(peptide);
            }

            const string ipaResultPath = @"D:\Research\Data\UW\QExactive\DIA_All_Summary.tsv";
            var parser = new TsvFileParser(ipaResultPath);
            var ipaPeptides = parser.GetPeptides(0.005).Select(p => p.Replace("C+57.021", "C"));
            var ipaOnly = 0;
            var both = 0;
            foreach (var ipaPeptide in ipaPeptides)
            {
                if (targetPeptides.Contains(ipaPeptide)) ++both;
                else
                {
                    ++ipaOnly;
                    Console.WriteLine(ipaPeptide);
                }
            }

            Console.WriteLine("Both: {0}, IpaOnly: {1}, Sum: {2}", both, ipaOnly, both+ipaOnly);
        }
    }
}
