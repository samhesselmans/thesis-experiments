global using Operator = System.Func<System.Collections.Generic.List<SA_ILP.Route>, System.Collections.Generic.List<int>, System.Random, System.Collections.Generic.List<SA_ILP.Customer>, SA_ILP.LocalSearch, (double, System.Action?)>;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    internal class OperatorSelector
    {


        Random random;
        List<Operator> operators;
        List<double> weights;
        List<String> labels;
        List<double> threshHolds;
        private int last = -1;

        List<String> operatorHistory;

        public List<String> OperatorList => labels.ConvertAll(x => x);

        public String LastOperator { get; private set; }
        public OperatorSelector(Random random)
        {
            this.random = random;
            operators = new List<Operator>();
            weights = new List<double>();
            threshHolds = new List<double>();
            labels = new List<string>();
            LastOperator = "none";
            operatorHistory = new List<string>();
        }


        public void Add(Operator op, double weight, String label = "unnamed-operator")
        {
            operators.Add(op);
            weights.Add(weight);
            labels.Add(label);

            threshHolds = new List<double>();
            double totalWeight = weights.Sum();

            double cumulative = 0;
            foreach( double w in weights)
            {
                cumulative += w;
                threshHolds.Add(cumulative / totalWeight);
            }

        }


        //public string Last()
        //{
        //    if (last == -1)
        //        return "none";
        //    else
        //        return labels[last];
        //}

        public void Add(List<Operator>operators,List<double> weights)
        {
            if (operators.Count != weights.Count)
                throw new Exception("List counts must match");

            for(int i =0; i< operators.Count; i++)
                Add(operators[i], weights[i]);
        }

        public Operator Next()
        {
            var p = random.NextDouble();
            for(int i=0; i< threshHolds.Count; i++)
            {
                if (p <= threshHolds[i])
                {
                    LastOperator = labels[i];
                    operatorHistory.Add(labels[i]);
                    return operators[i];
                }
            }

            throw new Exception("Threshold error");
        }





    }
}
