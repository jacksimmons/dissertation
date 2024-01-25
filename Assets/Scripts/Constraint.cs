using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
///Legend:
/// - OF = Objective Function
/// - x = Value of parameter [value]
/// - S = Steepness
/// - T = Tolerance either side of k (y tends to infinity at x = L - T or x = L + T)
/// - L = Limit (Maximum, or Convergence point)
///</summary>
public abstract class Constraint
{
    public readonly float Limit;
    public readonly float Weight;


    public Constraint(float limit, float weight)
    {
        Limit = limit;
        Weight = weight;
    }


    public abstract float _GetFitness(float value);
}


/// <summary>
/// A negative-exponential constraint which encourages minimisation or maximisation.
/// Graph: y = -1 - (k/(x-k)), x < k
/// </summary>
public class MinimiseConstraint : Constraint
{
    public MinimiseConstraint(float limit, float weight)
        : base(limit, weight)
    {
    }


    public override float _GetFitness(float value)
    {
        // A negative-exponential graph gives an OF which tends to infinity
        // as you approach the limit, L.
        // Graph: y = -1 - (L/(x-L)), x < L (minimise)
        if (value >= Limit) return float.PositiveInfinity; // Infinitely high fitness
        return -1 - Limit / (value - Limit);
    }
}

/// <summary>
/// A constraint which encourages convergence on a specific value (the limit), and
/// tolerates deviation up to the tolerance parameter. Fitness increase as the tolerance
/// limits are approached can be controlled by the steepness.
/// Graph: y = -L^2/(ST^2) - L^2/S[(x-(L-T))(x-(L+T))], L - T < x < L + T
/// </summary>
public class ConvergeConstraint : Constraint
{
    public readonly float Steepness;
    public readonly float Tolerance;


    public ConvergeConstraint(float goal, float weight, float steepness, float tolerance)
        : base(goal, weight)
    {
        Steepness = steepness;
        Tolerance = tolerance;
    }


    public override float _GetFitness(float value)
    {
        if (value <= Limit - Tolerance || value >= Limit + Tolerance) return float.PositiveInfinity;

        float f_a = -MathF.Pow(Limit / Tolerance, 2) / Steepness;
        float f_b_num = -MathF.Pow(Limit, 2);
        float f_b_denom = Steepness * (value - (Limit - Tolerance)) * (value - (Limit + Tolerance));

        return f_a + f_b_num / f_b_denom;
    }
}
