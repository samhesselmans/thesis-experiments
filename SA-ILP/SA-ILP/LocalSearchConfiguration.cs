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

        public bool AdjustEarlyArrivalToTWStart { get; set; }


        public bool CheckOperatorScores { get; set; }

        public bool SaveRoutesBeforeOperator { get; set; }

        public bool SaveColumnsAfterWorse { get; set; }

        public double SaveColumnThreshold { get; set; }
        public bool PrintExtendedInfo { get; set; }

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
            BaseEarlyArrivalPenalty = 0,
            BaseLateArrivalPenalty = 0,

            BaseRemovedCustomerPenalty = 5,
            BaseRemovedCustomerPenaltyPow = 1,
            Alpha = 0.99,
            SaveColumnsAfterAllImprovements = true,
            PenalizeEarlyArrival = true,
            PenalizeLateArrival = true,
            AdjustEarlyArrivalToTWStart = true,
            CheckOperatorScores = false,
            SaveRoutesBeforeOperator = false,
            SaveColumnsAfterWorse = true,
            SaveColumnThreshold = 0.2,
            PrintExtendedInfo = false
        };

        public static LocalSearchConfiguration VRPLTTDebug { get { var config = VRPLTT; config.PrintExtendedInfo = true; return config; } }

        public static LocalSearchConfiguration VRPTW => new LocalSearchConfiguration
        {
            InitialTemperature = 10,
            AllowEarlyArrivalDuringSearch = true,
            AllowLateArrivalDuringSearch = false,
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
            AdjustEarlyArrivalToTWStart = true,
            CheckOperatorScores = true,
            SaveRoutesBeforeOperator = false,
            SaveColumnsAfterWorse = true,
            SaveColumnThreshold = 0.1
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
