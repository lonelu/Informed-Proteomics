﻿using System;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using InformedProteomics.Scoring.LikelihoodScoring;

namespace InformedProteomics.BottomUp.Execution
{
    public class IcBottomUpRescorer
    {
        //public IcBottomUpRescorer(string specFilePath, string icResultFilePath, string outputFilePath, AminoAcidSet aaSet, Tolerance tolerance)
        //{
        //    _run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
        //    _scorer = new RankScore[4];
        //    for (var charge = 1; charge <= 4; charge++)
        //    {
        //        var scoringParamPath =
        //            @"C:\cygwin\home\kims336\Data\QCShewQE\Scoring\HCD_QCShew_RankProbabilities_Charge" + charge +
        //            ".txt";
        //        _scorer[charge-1] = new RankScore(scoringParamPath, 2);
        //    }
        //    Rescore(icResultFilePath, outputFilePath);
        //}

        //private readonly LcMsRun _run;
        //private readonly RankScore[] _scorer;

        //private void Rescore(string icResultFilePath, string outputFilePath)
        //{
        //    var parser = new TsvFileParser(icResultFilePath);
        //    var sequences = parser.GetData("Sequence");
        //    var scanNums = parser.GetData("Scan").Select(s => Convert.ToInt32(s)).ToArray();
        //    var charges = parser.GetData("Charge").Select(c => Convert.ToInt32(c)).ToArray();
        //    var compositions = parser.GetData("Composition").Select(Composition.Parse).ToArray();
        //    var modIndex = parser.GetHeaders().IndexOf("Modifications");

        //    var rows = parser.GetRows();
        //    var headers = parser.GetHeaders();

        //    using (var writer = new StreamWriter(outputFilePath))
        //    {
        //        writer.WriteLine("{0}\tLogLikelihoodScore", string.Join("\t", headers));
        //        for (var i = 0; i < parser.NumData; i++)
        //        {
        //            var row = rows[i];
        //            var seqStr = sequences[i];
        //            var charge = charges[i];
        //            var scanNum = scanNums[i];
        //            var composition = compositions[i];

        //            var scorer = charge <= _scorer.Length ? _scorer[charge - 1] : _scorer[_scorer.Length - 1];

        //            var scores = scorer.GetScores(AminoAcid.ProteinNTerm, seqStr, AminoAcid.ProteinCTerm, composition, charge, scanNum);

        //            var token = row.Split('\t');
        //            for (var j = 0; j < token.Length; j++)
        //            {
        //                if (j != modIndex) writer.Write(token[j] + "\t");
        //                else writer.Write("[" + scores.Modifications + "]" + "\t");
        //            }
        //            writer.WriteLine(scores);
        //        }
        //    }
        //}
    }
}
