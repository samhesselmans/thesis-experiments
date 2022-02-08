using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    internal static class VRPLTT
    {
        public static double CalculateTravelTime(double heightDiff, double length, double vehicleMass,double powerInput)
        {
            double speed = 25;
            double slope = Math.Atan(heightDiff / length) * Math.PI / 180;
            double requiredPow = CalcRequiredForce(speed / 3.6, vehicleMass, slope);
            double orignalPow = requiredPow;
            if (powerInput >= requiredPow)
            {
                    return speed;
            }
            while (speed > 0)
            {
                if (powerInput >= requiredPow)
                {
                    if (orignalPow + requiredPow - 2 * powerInput < 0)
                        speed += 0.01;
                    return speed;
                }

                speed -= 0.01;
                requiredPow = CalcRequiredForce(speed / 3.6, vehicleMass, slope);
            }
            return 0;


        }


        public static double CalcRequiredForce(double v, double mass, double slope)
        {
            double Cd = 1.18;
            double A = 0.83;
            double Ro = 1.18;
            double Cr = 0.01;
            double g = 9.81;

            return ((Cd * A * Ro * Math.Pow(v, 2) / 2) + Cr * mass * g * Math.Cos(Math.Atan(slope)) + mass * g * Math.Sin(Math.Atan(slope))) * v / 0.95;
        }
    }
}
