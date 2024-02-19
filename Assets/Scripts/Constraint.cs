using System;


/// <summary>
/// A constraint nudges the algorithm towards a more desirable value for a parameter.
/// In this project they represent 2D curves of a nutrient (x) against its optimality (y = f(x)).
/// 
/// The lower the value outputted by the curve function, the more optimal the value (x)
/// is for this nutrient.
/// 
/// The curve functions are not specified by the user - the user picks from a selection of pre-
/// designed curves which should fit every optimisation "type" they could want.
/// 
/// Abbreviations:
/// - OF = Objective Function
/// - x = Value of parameter [value]
/// - S = Steepness
/// - T = Tolerance either side of k (y tends to infinity at x = L - T or x = L + T)
/// - L = Limit (Maximum, or Convergence point)
/// - Min = Minimum of a range
/// - Max = Maximum of a range
///</summary>
public abstract class Constraint
{
    public readonly float Limit;

    public virtual float BestValue
    {
        get
        {
            return Limit;
        }
    }
    public virtual float WorstValue
    {
        get
        {
            return float.PositiveInfinity;
        }
    }


    public Constraint(float limit)
    {
        if (limit < 0)
            ThrowArgumentException("limit", limit);

        Limit = limit;
    }


    protected static void ThrowArgumentException(string name, float value)
    {
        throw new ArgumentOutOfRangeException(name, $"Value: {value}");
    }


    /// <summary>
    /// Get the fitness of a quantity value for a Day of food.
    /// Can be apnutrientd for individual portions, by multiplying by IdealNumPortionsPerDay.
    /// </summary>
    /// <param name="value">The quantity value for the whole Day.</param>
    /// <returns>The fitness of the quantity value.</returns>
    public abstract float _GetFitness(float value);
}


/// <summary>
/// A negative-exponential constraint which encourages minimisation.
/// Graph: y = -1 - (k/(x-k)), x < k
/// </summary>
public class MinimiseConstraint : Constraint
{
    public override float BestValue
    {
        get { return 0; }
    }


    public MinimiseConstraint(float limit)
        : base(limit)
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


    public ConvergeConstraint(float goal, float steepness, float tolerance)
        : base(goal)
    {
        if (steepness <= 0)
            ThrowArgumentException("steepness", steepness);
        if (tolerance <= 0)
            ThrowArgumentException("tolerance", tolerance);

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


/// <summary>
/// A constraint which gives a fitness of 0 if in a specified range.
/// Otherwise, gives an infinite fitness.
/// Graph is a conditional function:
///     { infinity, x <= Min
/// y = { infinity, x >= Max
///     { 0, otherwise
///     
/// It is constructed as an infinitely steep ConvergeConstraint.
/// </summary>
public class RangeConstraint : ConvergeConstraint
{
    // Creates a ConvergeConstraint centered at the mean of min and max.
    // Then makes it infinitely steep with a negligable weight of 1.
    // The tolerance either side is just half the difference between the max and the min.
    public RangeConstraint(float min, float max) : base((min + max) / 2, 1, (max - min)/2) { }
}


/// <summary>
/// A "non-constraint" which is not taken into consideration. Gives a fitness of 0 always.
/// </summary>
public class NullConstraint : Constraint
{
    public NullConstraint() : base(float.PositiveInfinity) { }


    public override float _GetFitness(float value) { return 0; }
}
