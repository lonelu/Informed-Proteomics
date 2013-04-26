﻿using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.IMS;
using Feature = InformedProteomics.Backend.IMS.Feature;

namespace InformedProteomics.Backend.IMSScoring
{
    public class IsotopomerFeatures : List<Feature>
    {
        private readonly float[] _theoreticalIsotopomerEnvelope;
        private readonly int _maxIntensityIndex;// max index for _theoreticalIsotopomerEnvelope, not fir this

        private IsotopomerFeatures(ImsDataCached imsData, Ion ion, Feature precursorFeature, bool isPrecurosr)
        {
            _theoreticalIsotopomerEnvelope = ion.Composition.GetApproximatedIsotopomerEnvelop();
            _maxIntensityIndex = GetMaximumIndex(_theoreticalIsotopomerEnvelope);

            for (var i = -1; i < 3;i++ )
            {
                var mz = ion.GetIsotopeMz(i + _maxIntensityIndex);
                Add(isPrecurosr? 
                    imsData.GetPrecursorFeature(mz, precursorFeature) : imsData.GetFramentFeature(mz, precursorFeature));
            }
        }

        static public IsotopomerFeatures GetFramentIsotopomerFeatures(ImsDataCached imsData, Composition cutComposition, IonType fragmentIonClassBase, Feature precursorFeature)
        {
            return new IsotopomerFeatures(imsData, fragmentIonClassBase.GetIon(cutComposition), precursorFeature, false);
        }

        static public IsotopomerFeatures GetPrecursorIsotopomerFeatures(ImsDataCached imsData, Ion precursorIon, Feature precursorFeature)
        {
            return new IsotopomerFeatures(imsData, precursorIon, precursorFeature, true);
        }

        public Feature GetNthFeatureFromTheoreticallyMostIntenseFeature(int n)
        {
            return this[Math.Min(Count - 1, n + _maxIntensityIndex + 1)]; // this[_maxIntensityIndex] corresponds to the max intensity istope - one isotope
        }

        public float GetTheoreticalIntensityOfNthFeature(int n)
        {
            if (n < 0) return 0f;
            return _theoreticalIsotopomerEnvelope[Math.Min(_theoreticalIsotopomerEnvelope.Length - 1, n + _maxIntensityIndex)];
        }

        static private int GetMaximumIndex(float[] e)
        {
            var maxIndex = 0;
            var max = float.NegativeInfinity;
            for (var i = 0; i < e.Length; i++)
            {
                if (!(e[i] > max)) continue;
                max = e[i];
                maxIndex = i;
            }
            return maxIndex;
        }
    }
}
