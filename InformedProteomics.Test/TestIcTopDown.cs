﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.Database;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.TopDown.Scoring;
using MathNet.Numerics;
using NUnit.Framework;

namespace InformedProteomics.Test
{
    [TestFixture]
    internal class TestIcTopDown
    {
        [Test]
        public void TestSbepSearch()
        {
            // Search parameters
            const int maxNumNTermCleavages = 1;  // 30
            const int minLength = 7;    // 7
            const int maxLength = 100; // 1000
            const int minPrecursorIonCharge = 3; // 3
            const int maxPrecursorIonCharge = 40;// 67
            const int minProductIonCharge = 1; // 1
            const int maxProductIonCharge = 10;// 10
            const int numMaxModsPerProtein = 0; // 6

            var precursorTolerance = new Tolerance(15);
            var productIonTolerance = new Tolerance(15);

            //const string dbFilePath = @"..\..\..\TestFiles\BSA.fasta";
            //const string dbFilePath =
            //    @"C:\cygwin\home\kims336\Data\TopDown\ID_003558_56D73071.fasta";
            const string dbFilePath = @"C:\cygwin\home\kims336\Data\TopDown\databases\ID_003525_320963E9.fasta";
            //            const string dbFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\P01031.fasta";

            //            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDown\E_coli_iscU_60_mock.raw";
            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\SBEP_STM_001_02222012_Aragon.raw";

            // Configure amino acid set
            //var pyroGluQ = new SearchModification(Modification.PyroGluQ, 'Q', SequenceLocation.Everywhere, false);
            var dehydroC = new SearchModification(Modification.PyroGluQ, 'C', SequenceLocation.Everywhere, false);
            //var cysteinylC = new SearchModification(Modification.CysteinylC, 'C', SequenceLocation.Everywhere, false);
            //var glutathioneC = new SearchModification(Modification.GlutathioneC, 'C', SequenceLocation.Everywhere, false);
            var oxM = new SearchModification(Modification.Oxidation, 'M', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
            {
                //pyroGluQ,
                dehydroC,
                //cysteinylC,
                //glutathioneC,
                oxM
            };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            TestTopDownSearch(dbFilePath, specFilePath, aaSet, minLength, maxLength, maxNumNTermCleavages,
                minPrecursorIonCharge, maxPrecursorIonCharge,
                minProductIonCharge, maxProductIonCharge, precursorTolerance, productIonTolerance, false, false);
        }

        [Test]
        public void TestQcShewSearch()
        {
            // Search parameters
            const int maxNumNTermCleavages = 1;  // 30
            const int minLength = 7;    // 7
            const int maxLength = 1000; // 1000
            const int minPrecursorIonCharge = 3; // 3
            const int maxPrecursorIonCharge = 40;// 67
            const int minProductIonCharge = 1; // 1
            const int maxProductIonCharge = 10;// 10
            const int numMaxModsPerProtein = 0; // 6

            var precursorTolerance = new Tolerance(10);
            var productIonTolerance = new Tolerance(10);

            //const string dbFilePath = @"..\..\..\TestFiles\BSA.fasta";
            //const string dbFilePath =
            //    @"C:\cygwin\home\kims336\Data\TopDown\ID_003558_56D73071.fasta";
            const string dbFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\Sigma48_UPS2_2012-11-02.fasta";
            //            const string dbFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\P01031.fasta";

            //            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDown\E_coli_iscU_60_mock.raw";
            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\Ron_UPS48_2_test_1ug.raw";

            // Configure amino acid set
            //var pyroGluQ = new SearchModification(Modification.PyroGluQ, 'Q', SequenceLocation.Everywhere, false);
            var dehydroC = new SearchModification(Modification.PyroGluQ, 'C', SequenceLocation.Everywhere, false);
            //var cysteinylC = new SearchModification(Modification.CysteinylC, 'C', SequenceLocation.Everywhere, false);
            //var glutathioneC = new SearchModification(Modification.GlutathioneC, 'C', SequenceLocation.Everywhere, false);
            var oxM = new SearchModification(Modification.Oxidation, 'M', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
            {
                //pyroGluQ,
                dehydroC,
                //cysteinylC,
                //glutathioneC,
                oxM
            };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            TestTopDownSearch(dbFilePath, specFilePath, aaSet, minLength, maxLength, maxNumNTermCleavages,
                minPrecursorIonCharge, maxPrecursorIonCharge,
                minProductIonCharge, maxProductIonCharge, precursorTolerance, productIonTolerance, false, false);
        }

        [Test]
        public void TestTrainingScoringParameters()
        {
            var specFiles = Directory.GetFiles(@"D:\Research\Data\TopDown\raw", "*.raw");
            var resultFiles = Directory.GetFiles(@"D:\Research\Data\TopDown\results", "*_ResultTable.txt");

            var numFiles = 0;
            foreach (var resultFile in resultFiles)
            {
                var resultFileName = Path.GetFileName(resultFile);
                var hasSpec = false;
                foreach (var specFile in specFiles)
                {
                    var specFileNameNoExt = Path.GetFileNameWithoutExtension(specFile);
                    if (specFileNameNoExt == null) continue;
                    if (resultFile.Contains(specFileNameNoExt+"_MSA"))
                    {
                        Console.WriteLine("{0}\t{1}", specFileNameNoExt, resultFileName);
                        ++numFiles;
                        hasSpec = true;
                        break;
                    }
                }
                if (!hasSpec)
                {
                    throw new FileNotFoundException("No raw file found for {0}", resultFile);
                }
            }
            Console.WriteLine("NumFilesWithResults: {0}", numFiles);
        }


        [Test]
        public void TestHistonSearch()
        {
            const bool isDecoy = true;
            // Search parameters
            const int maxNumNTermCleavages = 1;
            const int minLength = 7;    // 7
            const int maxLength = 1000; // 1000
            const int minPrecursorIonCharge = 3; // 3
            const int maxPrecursorIonCharge = 40;// 67
            const int minProductIonCharge = 1; // 1
            const int maxProductIonCharge = 10;// 10
            const int numMaxModsPerProtein = 11; // 6

            var precursorTolerance = new Tolerance(15);
            var productIonTolerance = new Tolerance(15);

            //const string dbFilePath = @"..\..\..\TestFiles\BSA.fasta";
            //const string dbFilePath =
            //    @"C:\cygwin\home\kims336\Data\TopDown\ID_003558_56D73071.fasta";
            //const string dbFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\Sigma48_UPS2_2012-11-02.fasta";
            const string dbFilePath = @"D:\Research\Data\TopDownHistone\HistoneH4.fasta";

            //            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDown\E_coli_iscU_60_mock.raw";
            const string specFilePath = @"D:\Research\Data\TopDownHistone\071210_070610His0Gy070210H4_H061010A.raw";

            var acetylR = new SearchModification(Modification.Acetylation, 'R', SequenceLocation.Everywhere, false);
            var acetylK = new SearchModification(Modification.Acetylation, 'K', SequenceLocation.Everywhere, false);
            var methylR = new SearchModification(Modification.Methylation, 'R', SequenceLocation.Everywhere, false);
            var methylK = new SearchModification(Modification.Methylation, 'K', SequenceLocation.Everywhere, false);
            var diMethylR = new SearchModification(Modification.DiMethylation, 'R', SequenceLocation.Everywhere, false);
            var diMethylK = new SearchModification(Modification.DiMethylation, 'K', SequenceLocation.Everywhere, false);
            var triMethylR = new SearchModification(Modification.TriMethylation, 'R', SequenceLocation.Everywhere, false);
            var phosphoS = new SearchModification(Modification.Phosphorylation, 'S', SequenceLocation.Everywhere, false);
            var phosphoT = new SearchModification(Modification.Phosphorylation, 'T', SequenceLocation.Everywhere, false);
            var phosphoY = new SearchModification(Modification.Phosphorylation, 'Y', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
                {
                    acetylR, acetylK,
                    methylR, methylK,
                    diMethylR, diMethylK,
                    triMethylR,
                    phosphoS, phosphoT, phosphoY
                };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            TestTopDownSearch(dbFilePath, specFilePath, aaSet, minLength, maxLength, maxNumNTermCleavages,
                minPrecursorIonCharge, maxPrecursorIonCharge,
                minProductIonCharge, maxProductIonCharge, precursorTolerance, productIonTolerance, true, isDecoy);            
        }

        [Test]
        public void TestSigma48Search()
        {
            // Search parameters
            const int maxNumNTermCleavages = 1;  // 30
            const int minLength = 7;    // 7
            const int maxLength = 1000; // 1000
            const int minPrecursorIonCharge = 3; // 3
            const int maxPrecursorIonCharge = 40;// 67
            const int minProductIonCharge = 1; // 1
            const int maxProductIonCharge = 10;// 10
            const int numMaxModsPerProtein = 0; // 6

            var precursorTolerance = new Tolerance(10);
            var productIonTolerance = new Tolerance(10);

            //const string dbFilePath = @"..\..\..\TestFiles\BSA.fasta";
            //const string dbFilePath =
            //    @"C:\cygwin\home\kims336\Data\TopDown\ID_003558_56D73071.fasta";
            const string dbFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\Sigma48_UPS2_2012-11-02.fasta";
//            const string dbFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\P01031.fasta";

            //            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDown\E_coli_iscU_60_mock.raw";
            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDownSigma48\Ron_UPS48_2_test_1ug.raw";

            // Configure amino acid set
            //var pyroGluQ = new SearchModification(Modification.PyroGluQ, 'Q', SequenceLocation.Everywhere, false);
            var dehydroC = new SearchModification(Modification.PyroGluQ, 'C', SequenceLocation.Everywhere, false);
            //var cysteinylC = new SearchModification(Modification.CysteinylC, 'C', SequenceLocation.Everywhere, false);
            //var glutathioneC = new SearchModification(Modification.GlutathioneC, 'C', SequenceLocation.Everywhere, false);
            var oxM = new SearchModification(Modification.Oxidation, 'M', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
            {
                //pyroGluQ,
                dehydroC,
                //cysteinylC,
                //glutathioneC,
                oxM
            };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            TestTopDownSearch(dbFilePath, specFilePath, aaSet, minLength, maxLength, maxNumNTermCleavages,
                minPrecursorIonCharge, maxPrecursorIonCharge,
                minProductIonCharge, maxProductIonCharge, precursorTolerance, productIonTolerance, false, false);
        }

        [Test]
        public void TestTopDownSearch(
            string dbFilePath, string specFilePath, AminoAcidSet aaSet,
            int minLength, int maxLength, int maxNumNTermCleavages,
            int minPrecursorIonCharge, int maxPrecursorIonCharge,
            int minProductIonCharge, int maxProductIonCharge,
            Tolerance precursorTolerance, Tolerance productIonTolerance,
            bool ultraMod,
            bool isDecoy
            )
        {

            var sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            Console.Write("Reading raw file...");
            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
            sw.Stop();
            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);


            var targetDb = new FastaDatabase(dbFilePath);
            targetDb.Read();

            var db = !isDecoy ? targetDb : targetDb.Decoy(null, true);  // shuffled decoy

            var indexedDb = new IndexedDatabase(db);
            
            var annotationsAndOffsets = indexedDb.AnnotationsAndOffsets(minLength, maxLength);

            var numProteins = 0;
            long totalProtCompositions = 0;
            long numPrecursorIons = 0;
            long numPrecursorIonsPassingFilter = 0;
            var prsmDictionary = new Dictionary<int, double>();

            sw.Reset();
            sw.Start();

            var icExtension = !isDecoy ? ".icresult" : "decoy.icresult";
            var outputFilePath = Path.ChangeExtension(specFilePath, icExtension);
            using (var writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine("Annotation\tProtein\tProteinDesc\tComposition\tCharge\tBaseIsotopeMz\tScanNum\tNumMatches");
                foreach (var annotationAndOffset in annotationsAndOffsets)
                {
                    ++numProteins;

                    var annotation = annotationAndOffset.Annotation;
                    var offset = annotationAndOffset.Offset;

//                    Console.WriteLine(annotation);
                    if (numProteins % 100 == 0)
                    {
                        Console.WriteLine("Processing {0}{1} proteins", numProteins, 
                            numProteins == 1 ? "st" : numProteins == 2 ? "nd" : numProteins == 3 ? "rd" : "th");
                        if (numProteins != 0)
                        {
                            sw.Stop();
                            sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
                            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
                            sw.Reset();
                            sw.Start();
                        }
                        //if (numProteins == 10) break;
                    }

                    //Console.WriteLine(protAnnotation);

                    var seqGraph = SequenceGraph.CreateGraph(aaSet, annotation);
                    if (seqGraph == null)
                    {
                        Console.WriteLine("Ignoring illegal protein: {0}", annotation);
                        continue;
                    }

                    for (var numNTermCleavage = 0; numNTermCleavage <= maxNumNTermCleavages; numNTermCleavage++)
                    {
                        //var compSet = new HashSet<Composition>();
                        var protCompositions = seqGraph.GetSequenceCompositionsWithNTermCleavage(numNTermCleavage);
                        if(ultraMod) Console.WriteLine("#NTermCleavages: {0}, #ProteinCompositions: ", numNTermCleavage, protCompositions.Length);
                        for (var modIndex = 0; modIndex < protCompositions.Length; modIndex++)
                        {
                            if (ultraMod)
                            {
                                if(modIndex % 100 == 0) Console.WriteLine("ModIndex: " + modIndex);
//                                if (modIndex >= 100) break;
                            }

                            seqGraph.SetSink(modIndex, numNTermCleavage);
                            var protCompositionWithH2O = seqGraph.GetSinkSequenceCompositionWithH2O();
                            //if (compSet.Contains(protCompositionWithH2O)) continue;
                            //compSet.Add(protCompositionWithH2O);

                            totalProtCompositions++;
                            for (var charge = minPrecursorIonCharge; charge <= maxPrecursorIonCharge; charge++)
                            {
                                numPrecursorIons++;
                                var precursorIon = new Ion(protCompositionWithH2O, charge);

                                var bestScore = Double.NegativeInfinity;
                                var bestScanNum = -1;
                                //Console.WriteLine("Debug: Charge {0}, MonoMz: {1}, MostAbundantMz: {2}"
                                //    , charge, precursorIon.GetMonoIsotopicMz(), precursorIon.GetMostAbundantIsotopeMz());

                                foreach (var ms2ScanNum in run.GetFragmentationSpectraScanNums(precursorIon))
                                {
                                    //if (!precursorFilter.IsValid(precursorIon, ms2ScanNum)) continue;
                                    if (run.CheckMs1Signature(precursorIon, ms2ScanNum, precursorTolerance) == false) continue;

                                    numPrecursorIonsPassingFilter++;
                                    var spec = run.GetSpectrum(ms2ScanNum) as ProductSpectrum;
                                    if (spec == null) continue;
                                    var scorer = new MatchedPeakCounter(spec, productIonTolerance, minProductIonCharge, maxProductIonCharge);
                                    var score = seqGraph.GetScore(charge, scorer);

                                    if (score > bestScore)
                                    {
                                        bestScore = score;
                                        bestScanNum = ms2ScanNum;
                                    }

                                    if (score >= 10)
                                    {
                                        double prevScore;
                                        if (!prsmDictionary.TryGetValue(ms2ScanNum, out prevScore) || score > prevScore)
                                        {
                                            prsmDictionary[ms2ScanNum] = score;
                                        }
                                    }
                                }
                                if (bestScore > 0)
                                {
                                    writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", 
                                        annotation.Substring(numNTermCleavage + 2, annotation.Length-4-numNTermCleavage),
                                        targetDb.GetProteinName(offset),
                                        targetDb.GetProteinDescription(offset),
                                        precursorIon.Composition,
                                        charge, 
                                        precursorIon.GetMostAbundantIsotopeMz(), 
                                        bestScanNum, 
                                        bestScore);
                                }
                            }
                        }
                    }
                }
            }

            sw.Stop();
            Console.WriteLine("NumProteins: {0}", numProteins);
            Console.WriteLine("NumProteinCompositions: {0}", totalProtCompositions);
            Console.WriteLine("NumPrecursorIons: {0}", numPrecursorIons);
            Console.WriteLine("NumPrecursorIonsWithEvidence: {0}", numPrecursorIonsPassingFilter);
            Console.WriteLine("NumTransitions: {0}", MatchedPeakCounter.GetNumTransitions());
            Console.WriteLine("Identified PrSMs (#IDs: {0}):", prsmDictionary.Count);
            foreach (var idedScan in prsmDictionary.Keys)
            {
                Console.WriteLine("{0}\t{1}", idedScan, prsmDictionary[idedScan]);
            }

            sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }

        [Test]
        public void TestComputingIsotopomerEnvelop()
        {
            const string sequence =
                "MRLNTLSPAEGSKKAGKRLGRGIGSGLGKTGGRGHKGQKSRSGGGVRRGFEGGQMPLYRRLPKFGFTSRKAAITAEIRLSDLAKVEGGVVDLNTLKAANIIGIQIEFAKVILAGEVTTPVTVRGLRVTKGARAAIEAAGGKIEE";
            const int charge = 17;

            var aaSet = new AminoAcidSet();
            var composition = aaSet.GetComposition(sequence);

            var precursorIon = new Ion(composition + Composition.H2O, charge);

            Console.WriteLine("{0}\t{1}\t{2}", precursorIon.Composition, charge, precursorIon.GetMonoIsotopicMz());
            foreach (var isotope in precursorIon.GetIsotopes(relativeIntensityThreshold: 0.05))
            {
                Console.WriteLine("{0}: {1}\t{2}", isotope.Index, precursorIon.GetIsotopeMz(isotope.Index), isotope.Ratio);
            }
        }

        [Test]
        public void TestTopDownSearchOneProtein()
        {
            // Parameters
            const int minCharge = 3;    // 3
            const int maxCharge = 67;   // 67
            var precursorIonTolerance = new Tolerance(10);
            var productIonTolerance = new Tolerance(10);

            var sw = new System.Diagnostics.Stopwatch();

            // Configure amino acids
            //var aaSet = new AminoAcidSet();


            // Configure amino acid set
            const int numMaxModsPerProtein = 6;
            var pyroGluQ = new SearchModification(Modification.PyroGluQ, 'Q', SequenceLocation.Everywhere, false);
            var dehydro = new SearchModification(Modification.PyroGluQ, 'C', SequenceLocation.Everywhere, false);
            var cysteinylC = new SearchModification(Modification.CysteinylC, 'C', SequenceLocation.Everywhere, false);
            var glutathioneC = new SearchModification(Modification.GlutathioneC, 'C', SequenceLocation.Everywhere, false);
            var oxM = new SearchModification(Modification.Oxidation, 'M', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
            {
                pyroGluQ,
                //dehydro,
                //cysteinylC,
                //glutathioneC,
                //oxM
            };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            const string protAnnotation = "A.HAHLTHQYPAANAQVTAAPQAITLNFSEGVETGFSGAKITGPKNENIKTLPAKRNEQDQKQLIVPLADSLKPGTYTVDWHVVSVDGHKTKGHYTFSVK._";
            //const string protAnnotation =
            //    "_.MKLYNLKDHNEQVSFAQAVTQGLGKNQGLFFPHDLPEFSLTEIDEMLKLDFVTRSAKILSAFIGDEIPQEILEERVRAAFAFPAPVANVESDVGCLELFHGPTLAFKDFGGRFMAQMLTHIAGDKPVTILTATSGDTGAAVAHAFYGLPNVKVVILYPRGKISPLQEKLFCTLGGNIETVAIDGDFDACQALVKQAFDDEELKVALGLNSANSINISRLLAQICYYFEAVAQLPQETRNQLVVSVPSGNFGDLTAGLLAKSLGLPVKRFIAATNVNDTVPRFLHDGQWSPKATQATLSNAMDVSQPNNWPRVEELFRRKIWQLKELGYAAVDDETTQQTMRELKELGYTSEPHAAVAYRALRDQLNPGEYGLFLGTAHPAKFKESVEAILGETLDLPKELAERADLPLLSHNLPADFAALRKLMMNHQ._";

            // Create a sequence graph
            var seqGraph = SequenceGraph.CreateGraph(aaSet, protAnnotation);
            if (seqGraph == null)
            {
                Console.WriteLine("Invalid sequence: {0}", protAnnotation);
                return;
            }

            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDown\E_coli_iscU_60_mock.raw";
            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            sw.Start();
            var precursorFilter = new PrecursorFilter(run, precursorIonTolerance);

            var seqCompositionArr = seqGraph.GetSequenceCompositions();
            Console.WriteLine("Length: {0}\tNumCompositions: {1}", protAnnotation.Length-4, seqCompositionArr.Length);

            for (var modIndex = 0; modIndex < seqCompositionArr.Length; modIndex++)
            {
                var seqComposition = seqCompositionArr[modIndex];
                var peptideComposition = seqComposition + Composition.H2O;
                peptideComposition.GetIsotopomerEnvelop();

                Console.WriteLine("Composition: {0}, Mass: {1}", seqComposition, seqComposition.Mass);

                //if (Math.Abs(seqComposition.GetMass() - 47162.1844822) > 0.001) continue;

                for (var charge = minCharge; charge <= maxCharge; charge++)
                {
                    var precursorIon = new Ion(peptideComposition, charge);

                    var bestScore = Double.NegativeInfinity;
                    var bestScanNum = -1;
                    foreach (var ms2ScanNum in run.GetFragmentationSpectraScanNums(precursorIon))
                    {
                        if (!precursorFilter.IsValid(precursorIon, ms2ScanNum)) continue;

                        var spec = run.GetSpectrum(ms2ScanNum) as ProductSpectrum;
                        if (spec == null) continue;
                        var scorer = new MatchedPeakCounter(spec, productIonTolerance, 1, 6);
                        var score = seqGraph.GetScore(charge, scorer);

                        //Console.WriteLine("{0}\t{1}\t{2}\t{3}", precursorIon.GetMostAbundantIsotopeMz(), precursorIon.Charge, ms2ScanNum, score);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestScanNum = ms2ScanNum;
                        }
                    }
                    if (bestScore > 10)
                    {
                        Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", protAnnotation, charge, precursorIon.GetMostAbundantIsotopeMz(), bestScanNum, bestScore);
                    }
                }
            }
            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

        }


        [Test]
        public void CountProteins()
        {
            const string filePath = @"C:\cygwin\home\kims336\Data\TopDown\E_coli_iscU_60_mock.icresult";

            var protSet = new HashSet<string>();
            foreach (var line in File.ReadLines(filePath))
            {
                var token = line.Split('\t');
                if (token.Length != 4) continue;
                protSet.Add(token[0]);
            }
            Console.WriteLine("NumProteins: {0}", protSet.Count);
        }

        [Test]
        public void TestGeneratingAllXics()
        {
            // Search parameters
            const int numNTermCleavages = 1;  // 30
            const int minLength = 7;
            const int maxLength = 1000;
            const int minCharge = 3; // 3
            const int maxCharge = 67; // 67
            const int numMaxModsPerProtein = 0; // 6
            var precursorTolerance = new Tolerance(20);
            //const string dbFilePath = @"..\..\..\TestFiles\BSA.fasta";
            const string dbFilePath =
                @"C:\cygwin\home\kims336\Data\TopDown\ID_003558_56D73071.fasta";

            var sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            Console.Write("Reading raw file...");
            const string specFilePath = @"C:\cygwin\home\kims336\Data\TopDown\E_coli_iscU_60_mock.raw";
            var run = LcMsRun.GetLcMsRun(specFilePath, MassSpecDataType.XCaliburRun);

            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);

            // Configure amino acid set
            //            var pyroGluQ = new SearchModification(Modification.PyroGluQ, 'Q', SequenceLocation.ProteinNTerm, false);
            var dehydro = new SearchModification(Modification.PyroGluQ, 'C', SequenceLocation.Everywhere, false);
            var cysteinylC = new SearchModification(Modification.CysteinylC, 'C', SequenceLocation.Everywhere, false);
            var glutathioneC = new SearchModification(Modification.GlutathioneC, 'C', SequenceLocation.Everywhere, false);
            //            var oxM = new SearchModification(Modification.Oxidation, 'M', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
                {
                    //pyroGluQ,
                    dehydro,
                    cysteinylC,
                    glutathioneC,
                    //oxM
                };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            var targetDb = new FastaDatabase(dbFilePath);
            var indexedDb = new IndexedDatabase(targetDb);

            var numProteins = 0;
            long totalProtCompositions = 0;
            long numXics = 0;

            sw.Reset();
            sw.Start();
            Console.WriteLine("Generating XICs...");

            foreach (var protAnnotationAndOffset in indexedDb.AnnotationsAndOffsets(minLength, maxLength))
            {
                ++numProteins;

                if (numProteins % 1000 == 0)
                {
                    Console.WriteLine("Processed {0} proteins", numProteins);
                }

                //Console.WriteLine(protAnnotation);

                var seqGraph = SequenceGraph.CreateGraph(aaSet, protAnnotationAndOffset.Annotation);
                if (seqGraph == null) continue;

                for (var nTermCleavages = 0; nTermCleavages <= numNTermCleavages; nTermCleavages++)
                {
                    var protCompositions = seqGraph.GetSequenceCompositionsWithNTermCleavage(nTermCleavages);
                    foreach (var protComposition in protCompositions)
                    {
                        totalProtCompositions++;
                        var mostAbundantIsotopeIndex = protComposition.GetMostAbundantIsotopeZeroBasedIndex();
                        for (var charge = minCharge; charge <= maxCharge; charge++)
                        {
                            numXics++;
                            var precursorIon = new Ion(protComposition + Composition.H2O, charge);
                            run.GetExtractedIonChromatogram(precursorIon.GetIsotopeMz(mostAbundantIsotopeIndex), precursorTolerance);
                        }
                    }
                }
            }

            sw.Stop();
            Console.WriteLine("NumProteins: {0}", numProteins);
            Console.WriteLine("NumProteinCompositions: {0}", totalProtCompositions);
            Console.WriteLine("NumXics: {0}", numXics);
            sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }
        
        [Test]
        public void TestGeneratingXics()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            const string specFilePath = @"C:\workspace\TopDown\E_coli_iscU_60_mock.raw";
            var run = new LcMsRun(new XCaliburReader(specFilePath));
            const string protAnnotation = "A.HAHLTHQYPAANAQVTAAPQAITLNFSEGVETGFSGAKITGPKNENIKTLPAKRNEQDQKQLIVPLADSLKPGTYTVDWHVVSVDGHKTKGHYTFSVK.-";
            var aaSet = new AminoAcidSet();

            var precursorTolerance = new Tolerance(10);

            // Create a sequence graph
            var protSeq = protAnnotation.Substring(2, protAnnotation.Length - 4);
            var seqGraph = SequenceGraph.CreateGraph(aaSet, protSeq);
            foreach (var protComposition in seqGraph.GetSequenceCompositions())
            {
                var mostAbundantIsotopeIndex = protComposition.GetMostAbundantIsotopeZeroBasedIndex();
                Console.WriteLine("Composition\t{0}", protComposition);
                Console.WriteLine("MostAbundantIsotopeIndex\t{0}", mostAbundantIsotopeIndex);
                Console.WriteLine();

                for (var charge = 10; charge <= 14; charge++)
                {
                    var precursorIon = new Ion(protComposition+Composition.H2O, charge);
                    var xic = run.GetExtractedIonChromatogram(precursorIon.GetIsotopeMz(mostAbundantIsotopeIndex), precursorTolerance);
                    //Console.WriteLine(xic[0].ScanNum + " " + xic[1].ScanNum);
                    
                    Console.WriteLine("ScanNum\t{0}", string.Join("\t", xic.Select(p => p.ScanNum.ToString())));
                    Console.WriteLine("Charge " + charge + "\t" + string.Join("\t", xic.Select(p => p.Intensity.ToString())));
                }

                Console.WriteLine("\nCharge\tm/z");
                for (var charge = 9; charge <= 18; charge++)
                {
                    var precursorIon = new Ion(protComposition + Composition.H2O, charge);
                    Console.WriteLine("{0}\t{1}", charge, precursorIon.GetIsotopeMz(mostAbundantIsotopeIndex));
                }
            }

            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }



        [Test]
        public void TestHistonEnumeration()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            const int numNTermCleavages = 0;
            const int numMaxModsPerProtein = 11;
            const string protAnnotation = "_.MSGRGKGGKGLGKGGAKRHRKVLRDNIQGITKPAIRRLARRGGVKRISGLIYEETRGVLKVFLENVIRDAVTYTEHAKRKTVTAMDVVYALKRQGRTLYGFGG._";  // Histone H4
            //const string protAnnotation =
            //    "_.MARTKQTARKSTGGKAPRKQLATKAARKSAPSTGGVKKPHRYRPGTVALREIRRYQKSTELLIRKLPFQRLVREIAQDFKTDLRFQSAAIGALQEASEAYLVGLFEDTNLCAIHAKRVTIMPKDIQLARRIRGERA._"; // Histone H3.2

            var acetylR = new SearchModification(Modification.Acetylation, 'R', SequenceLocation.Everywhere, false);
            var acetylK = new SearchModification(Modification.Acetylation, 'K', SequenceLocation.Everywhere, false);
            var methylR = new SearchModification(Modification.Methylation, 'R', SequenceLocation.Everywhere, false);
            var methylK = new SearchModification(Modification.Methylation, 'K', SequenceLocation.Everywhere, false);
            var diMethylR = new SearchModification(Modification.DiMethylation, 'R', SequenceLocation.Everywhere, false);
            var diMethylK = new SearchModification(Modification.DiMethylation, 'K', SequenceLocation.Everywhere, false);
            var triMethylR = new SearchModification(Modification.TriMethylation, 'R', SequenceLocation.Everywhere, false);
            var phosphoS = new SearchModification(Modification.Phosphorylation, 'S', SequenceLocation.Everywhere, false);
            var phosphoT = new SearchModification(Modification.Phosphorylation, 'T', SequenceLocation.Everywhere, false);
            var phosphoY = new SearchModification(Modification.Phosphorylation, 'Y', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
                {
                    acetylR, acetylK,
                    methylR, methylK,
                    diMethylR, diMethylK,
                    triMethylR,
                    phosphoS, phosphoT, phosphoY
                };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            var numPossiblyModifiedResidues = 0;
            var numR = 0;
            var numK = 0;
            var numSTY = 0;
            foreach (var aa in protAnnotation.Substring(2, protAnnotation.Length - 4))
            {
                var numMods = aaSet.GetModificationIndices(aa).Length;
                if (aa == 'S' || aa == 'T' || aa == 'Y') numSTY++;
                if (aa == 'R') numR++;
                if (aa == 'K') numK++;
                if (numMods >= 1)
                {
                    numPossiblyModifiedResidues += 1;
                }
            }

            var numProteoforms = 0.0;
            for (var numMods = 0; numMods <= numMaxModsPerProtein; numMods++)
            {
                for (var i = 0; i <= numMods; i++)
                {
                    for (var j = 0; i + j <= numMods; j++)
                    {
                        var k = numMods - i - j;
                        numProteoforms += SpecialFunctions.Binomial(numR, i)*Math.Pow(4, i)
                                          *SpecialFunctions.Binomial(numK, j)*Math.Pow(3, j)
                                          *SpecialFunctions.Binomial(numSTY, k);
                    }
                }
            }
            Console.WriteLine("#Proteoforms: {0:E2}", numProteoforms);
            Console.WriteLine("#PossiblyModifiedResidues: {0}", numPossiblyModifiedResidues);
            Console.WriteLine("#STY: {0}", numSTY);
            Console.WriteLine("#K: {0}", numK);
            Console.WriteLine("#R: {0}", numR);
            Console.WriteLine("5 choose 2: {0}", SpecialFunctions.Binomial(5, 2));

            var seqGraph = SequenceGraph.CreateGraph(aaSet, protAnnotation);
            if (seqGraph == null)
            {
                Console.WriteLine("Invalid sequence: {0}", protAnnotation);
                return;
            }

            Console.WriteLine("Num sequence compositions: {0}, {1}", seqGraph.GetNumSequenceCompositions(), seqGraph.GetNumDistinctSequenceCompositions()
                );

            Console.WriteLine("Num product compositions: {0}", seqGraph.GetNumFragmentCompositions()
                );

            sw.Stop();
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }

        [Test]
        public void TestTopDownEnumeration()
        {
            // Search parameters
            const int numNTermCleavages = 30;
            const int minLength = 7;
            const int maxLength = 1000;
            const int numMaxModsPerProtein = 6;
            //const string dbFilePath = @"..\..\..\TestFiles\BSA.fasta";
            const string dbFilePath =
                @"..\..\..\TestFiles\H_sapiens_Uniprot_SPROT_2013-05-01_withContam.fasta";

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Configure amino acid set
//            var pyroGluQ = new SearchModification(Modification.PyroGluQ, 'Q', SequenceLocation.ProteinNTerm, false);
            var dehydro = new SearchModification(Modification.PyroGluQ, 'C', SequenceLocation.Everywhere, false);
            var cysteinylC = new SearchModification(Modification.CysteinylC, 'C', SequenceLocation.Everywhere, false);
            var glutathioneC = new SearchModification(Modification.GlutathioneC, 'C', SequenceLocation.Everywhere, false);
//            var oxM = new SearchModification(Modification.Oxidation, 'M', SequenceLocation.Everywhere, false);

            var searchModifications = new List<SearchModification>
                {
                    //pyroGluQ,
                    dehydro,
                    cysteinylC,
                    glutathioneC,
                    //oxM
                };
            var aaSet = new AminoAcidSet(searchModifications, numMaxModsPerProtein);

            var targetDb = new FastaDatabase(dbFilePath);
            var indexedDb = new IndexedDatabase(targetDb);

            var numProteins = 0;
            long totalProtCompositions = 0;
            foreach (var protAnnotationAndOffset in indexedDb.AnnotationsAndOffsets(minLength, maxLength))
            {
                ++numProteins;

                if (numProteins % 1000 == 0)
                {
                    Console.WriteLine("Processed {0} proteins", numProteins);
                }

                var seqGraph = SequenceGraph.CreateGraph(aaSet, protAnnotationAndOffset.Annotation);
                if (seqGraph == null) continue;

                for (var nTermCleavage = 0; nTermCleavage <= numNTermCleavages; nTermCleavage++)
                {
                    totalProtCompositions += seqGraph.GetNumSequenceCompositionsWithNTermCleavage(nTermCleavage);
                }
            }

            sw.Stop();
            Console.WriteLine("NumProteins: {0}", numProteins);
            Console.WriteLine("NumProteinCompositions: {0}", totalProtCompositions);
            var sec = (double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }
    }
}
