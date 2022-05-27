﻿using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    public struct LocalSearchConfiguration
    {
        public double InitialTemperature { get; set; }

        public bool AllowLateArrivalDuringSearch { get; set; }
        public bool AllowEarlyArrivalDuringSearch { get; set; }

        public bool AllowLateArrival { get; set; }
        public bool AllowEarlyArrival { get; set; }

        public double BaseRemovedCustomerPenalty { get; set; }
        public double BaseRemovedCustomerPenaltyPow { get; set; }
        public double BaseEarlyArrivalPenalty { get; set; }
        public double BaseLateArrivalPenalty { get; set; }

        public double Alpha { get; set; }

        public bool SaveColumnsAfterAllImprovements { get; set; }

        public bool PenalizeEarlyArrival { get; set; }
        public bool PenalizeLateArrival { get; set; }

        public bool AdjustDeterministicEarlyArrivalToTWStart { get; set; }

        //Only used in simmulations so far
        public bool AdjustEarlyArrivalToTWStart { get; set; }

        public bool CheckOperatorScores { get; set; }

        public bool SaveRoutesBeforeOperator { get; set; }

        public bool SaveColumnsAfterWorse { get; set; }

        public double SaveColumnThreshold { get; set; }
        public bool PrintExtendedInfo { get; set; }
        public bool SaveScoreDevelopment { get; set; }

        public double ExpectedEarlinessPenalty { get; set; }
        public double ExpectedLatenessPenalty { get; set; }

        public bool UseMeanOfDistributionForTravelTime { get; set; }
        public bool ScaleEarlinessPenaltyWithTemperature { get; set; }
        public bool ScaleLatenessPenaltyWithTemperature { get; set; }

        public bool UseMeanOfDistributionForScore { get; set; }
        public bool IgnoreWaitingDuringDistributionAddition { get; set; }

        public double WindSpeed { get; set; }

        public double[] WindDirection { get; set; }

        public int NumRestarts { get; set; }

        public int IterationsPerAlphaChange { get; set; }

        public int NumIterationsOfNoChangeBeforeRestarting { get; set; }

        public double RestartTemperatureBound { get; set; }

        public IContinuousDistribution DefaultDistribution { get; set; }
        public override string ToString()
        {
            LocalSearchConfiguration obj = this;
            return GetType().GetProperties()

        .Select(info =>
        {

            return (info.Name, Value: info.GetValue(obj, null) ?? "(null)");
        })
        .Aggregate(
            new StringBuilder(),
            (sb, pair) => sb.AppendLine($"{pair.Name}: {pair.Value}"),
            sb => sb.ToString());
        }

    }

    public static class LocalSearchConfigs
    {
        public static LocalSearchConfiguration VRPLTT => new LocalSearchConfiguration
        {
            InitialTemperature = 1,

            //THESE OPTIONS ARE NOT YET CORRECTLY IMPLEMENTED
            AllowEarlyArrivalDuringSearch = true,
            AllowLateArrivalDuringSearch = true,


            AllowEarlyArrival = false,
            AllowLateArrival = false,
            BaseEarlyArrivalPenalty = 2,
            BaseLateArrivalPenalty = 2,

            BaseRemovedCustomerPenalty = 0.1,
            BaseRemovedCustomerPenaltyPow = 2,
            Alpha = 0.99,

            SaveColumnsAfterAllImprovements = true,
            SaveColumnsAfterWorse = true,
            SaveColumnThreshold = 0.2,


            PenalizeEarlyArrival = true,
            PenalizeLateArrival = true,
            AdjustDeterministicEarlyArrivalToTWStart = true,
            AdjustEarlyArrivalToTWStart = true,
            CheckOperatorScores = true,
            SaveRoutesBeforeOperator = false,

            PrintExtendedInfo = false,
            SaveScoreDevelopment = false,
            ExpectedEarlinessPenalty = 0,
            ExpectedLatenessPenalty = 0,
            UseMeanOfDistributionForTravelTime = false,
            ScaleEarlinessPenaltyWithTemperature = true,
            ScaleLatenessPenaltyWithTemperature = true,
            IgnoreWaitingDuringDistributionAddition = true,
            IterationsPerAlphaChange = 10000,
            NumIterationsOfNoChangeBeforeRestarting = 600000,
            RestartTemperatureBound = 0.02,
            NumRestarts = 7,
            WindSpeed = 0
        };

        public static LocalSearchConfiguration VRPSLTTWithoutWaiting
        {
            get
            {
                var config = VRPLTT;
                config.ExpectedEarlinessPenalty = 10;
                config.ExpectedLatenessPenalty = 10;

                config.AllowEarlyArrival = false;
                config.PenalizeEarlyArrival = true;


                config.CheckOperatorScores = false;
                config.SaveRoutesBeforeOperator = false;
                config.ScaleEarlinessPenaltyWithTemperature = true;
                config.ScaleLatenessPenaltyWithTemperature = true;

                //This does not work with the checks currently!
                config.UseMeanOfDistributionForTravelTime = false;
                config.UseMeanOfDistributionForScore = false;

                config.IgnoreWaitingDuringDistributionAddition = true;


                config.AdjustDeterministicEarlyArrivalToTWStart = false;
                config.AdjustEarlyArrivalToTWStart = true;


                //config.DefaultDistribution = new Gamma(0, 10);//new Normal(0, 0);
                config.DefaultDistribution = new Normal(0, 0);


                return config;
            }
        }

        public static LocalSearchConfiguration VRPSLTTWithWaiting
        {
            get
            {
                var config = VRPSLTTWithoutWaiting;
                config.AllowEarlyArrival = true;
                config.PenalizeEarlyArrival = false;
                config.ExpectedEarlinessPenalty = 0;

                return config;
            }
        }


        public static LocalSearchConfiguration VRPLTTWithWind
        {
            get
            {
                var config = VRPLTT;
                config.WindDirection = new double[] { 1, 0 };
                config.WindSpeed = 10;

                return config;
            }
        }


        public static LocalSearchConfiguration VRPLTTDebug { get { var config = VRPLTT; config.PrintExtendedInfo = true; return config; } }

        public static LocalSearchConfiguration VRPTW => new LocalSearchConfiguration
        {
            InitialTemperature = 10,
            AllowEarlyArrivalDuringSearch = true,
            AllowLateArrivalDuringSearch = true,
            AllowEarlyArrival = true,
            AllowLateArrival = false,
            BaseEarlyArrivalPenalty = 100,
            BaseLateArrivalPenalty = 100,

            BaseRemovedCustomerPenalty = 400,

            BaseRemovedCustomerPenaltyPow = 1.5,
            Alpha = 0.992,
            SaveColumnsAfterAllImprovements = false,
            PenalizeEarlyArrival = false,
            PenalizeLateArrival = true,
            AdjustDeterministicEarlyArrivalToTWStart = true,
            CheckOperatorScores = true,
            SaveRoutesBeforeOperator = false,
            SaveColumnsAfterWorse = true,
            SaveColumnThreshold = 0.1,
            SaveScoreDevelopment = false,
            ExpectedEarlinessPenalty = 0,
            ExpectedLatenessPenalty = 0,
            UseMeanOfDistributionForTravelTime = false,
            ScaleEarlinessPenaltyWithTemperature = true,
            ScaleLatenessPenaltyWithTemperature = true,
            IgnoreWaitingDuringDistributionAddition = true
        };


        //public static List<Operator> SimpleOperators => new List<Func<List<Route>, List<int>, Random, List<Customer>, double, (double, Action?)>>() {Operators.AddRandomRemovedCustomer,Operators. };

        //public static (List<Operator>, List<double>) SimpleOperators
        //{
        //    get
        //    {
        //        return CreateSimpleOperators();
        //    }
        //}

        //private static (List<Operator>, List<double>) CreateSimpleOperators()
        //{

        //}

    }
}
