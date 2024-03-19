/// A constraint nudges (or forces, if a hard constraint is used) the algorithm towards a more
/// desirable value for a parameter.
/// In this project they represent 2D curves of a nutrient (x) against its optimality (y = f(x)).
/// 
/// The lower the value outputted by the curve function, the more optimal the value (x)
/// is for this nutrient.
/// 
/// Abbreviations:
/// - OF = Objective Function
/// - x = Value of parameter [value]
/// - S = Steepness
/// - T = Tolerance either side of k (y tends to infinity at x = L - T or x = L + T)
/// - L = Limit (Maximum, or Convergence point)
using System;


/// <summary>
/// Stores parameters for any constraint. Note these are not Properties, to allow each parameter
/// to be changed by ref.
/// </summary>
public class ConstraintData
{
    /// <summary>
    /// (Optional, default: 0) Any value below this gives a fitness of Infinity.
    /// </summary>
    public float Min;

    /// <summary>
    /// Any value above this gives a fitness of Infinity.
    /// </summary>
    public float Max;

    /// <summary>
    /// (Optional, default: unused) The target value to reach. Gives a fitness of 0 for ConvergeConstraint.
    /// </summary>
    public float Goal;

    /// <summary>
    /// The importance of this constraint. The fitness is directly multiplied by this.
    /// </summary>
    public float Weight;

    /// <summary>
    /// The full name of the type of constraint to instantiate (using reflection; Type.GetType(Type))
    /// </summary>
    public string Type = "";
}


/// <summary>
/// Abstract base class for Constraints.
/// </summary>
public abstract class Constraint
{
    public abstract float BestValue { get; }
    public abstract float WorstValue { get; }

    public readonly float weight;


    public static Constraint Build(ConstraintData data)
    {
        Type type = Type.GetType(data.Type)!;
        if (!type.IsSubclassOf(typeof(Constraint)))
            Logger.Error($"Invalid Constraint type: {data.Type}.");

        return (Constraint)Activator.CreateInstance(Type.GetType(data.Type)!, data)!;
    }


    public Constraint(float weight)
    {
        this.weight = weight;
    }


    /// <summary>
    /// Calculates fitness by multiplying the weight by the unweighted fitness value.
    /// </summary>
    public float GetFitness(float amount)
    {
        if (amount < 0) Logger.Error("Amount was negative.");
        return weight * GetUnweightedFitness(amount);
    }


    public abstract float GetUnweightedFitness(float amount);


    public abstract string Verbose();
}


/// <summary>
/// A constraint which gives a fitness of 0 if in a specified range.
/// Otherwise, gives an infinite fitness.
/// </summary>
public class HardConstraint : Constraint
{
    /// <summary>
    /// Minimum value.
    /// </summary>
    public readonly float min;
    /// <summary>
    /// Maximum value.
    /// </summary>
    public readonly float max;


    public override float BestValue => (min + max) / 2;
    public override float WorstValue => float.PositiveInfinity;


    public HardConstraint(float min, float max, float weight) : base(weight)
    {
        this.min = min;
        this.max = max;

        CheckParams();
    }


    public HardConstraint(ConstraintData data) : base(data.Weight)
    {
        min = data.Min;
        max = data.Max;

        CheckParams();
    }


    /// <summary>
    /// Do not inherit this. Base constructor is called first, so HardConstraint will check the params of
    /// the inherited class before the inherited constructor has begun.
    /// </summary>
    protected void CheckParams()
    {
        if (max < min)
            Logger.Log($"Argument max ({max}) was less than argument min ({min}).", Severity.Error);
        if (weight < 0)
            Logger.Error($"Argument weight ({weight}) was < 0.");
    }


    public override float GetUnweightedFitness(float amount)
    {
        // Use approx less than to ensure the minimum and maximum values can be used in the range.
        // Using standard < and > doesn't account for floating point inaccuracies, which can make
        // Approx(amount, min) or Approx(amount, max) yield fitness == Infinity.
        if (MathTools.ApproxLessThan(amount, min) || MathTools.ApproxLessThan(max, amount))
        {
            return float.PositiveInfinity;
        }
        return 0;
    }


    public override string Verbose()
    {
        return $"Hard constraint [{min}, {max}]";
    }
}


/// <summary>
/// A constraint which encourages convergence on a specific value (the limit), and acts as a
/// hard constraint at the same time. Fitness increases linearly with deviation from convergence
/// point, and becomes infinity if outside the specified bounds.
/// Graph: y = -L^2/(ST^2) - L^2/S[(x-(L-T))(x-(L+T))], L - T < x < L + T
/// </summary>
public class ConvergeConstraint : HardConstraint
{
    public readonly float goal;


    public override float BestValue => goal;
    public override float WorstValue => float.PositiveInfinity;


    public ConvergeConstraint(float goal, float min, float max, float weight)
        : base(min, max, weight)
    {
        this.goal = goal;

        CheckParams();
    }


    public ConvergeConstraint(ConstraintData data) : base(data)
    {
        goal = data.Goal;

        CheckParams();
    }


    private new void CheckParams()
    {
        base.CheckParams();

        if (goal < 0 || goal < min || goal > max)
            Logger.Log($"Goal: {goal} is out of range. It must be >= 0, >= min ({min}) and <= max ({max}).", Severity.Error);
    }


    public override float GetUnweightedFitness(float amount)
    {
        if (float.IsPositiveInfinity(base.GetUnweightedFitness(amount))) return float.PositiveInfinity;
        // If amount == max, sometimes this can give -Infinity, so assign a fitness of Infinity in this case.

        float tolerance;
        if (MathTools.Approx(amount, goal)) return 0;

        if (amount > goal)
        {
            // RHS
            tolerance = max - goal;
        }
        else
        {
            // LHS
            tolerance = goal - min;
        }

        // Represents the magnitude of the derivative of the graph.
        // Needs to change with tolerance.
        // Take for example: 0 min < 30 goal < 50 max.
        // The graph represented by 0 min < 300 goal < 500 max should look the same on a graphing calculator (provided you zoom out
        // 10x), and the same for 0 < 3000 < 5000. This leads to reasonable convergence for all graphs.
        float speed = 1/tolerance;

        float f_a = -MathF.Pow(goal / tolerance, 2) / speed;
        float f_b_num = -MathF.Pow(goal, 2);
        float f_b_denom = speed * (amount - (goal - tolerance)) * (amount - (goal + tolerance));

        // If amount has reached the limit, return Infinity. Removing this leads to -Infinity possibility.
        if (MathTools.Approx(f_b_denom, 0)) return float.PositiveInfinity;

        return f_a + f_b_num / f_b_denom;
    }


    public override string Verbose()
    {
        return base.Verbose() + $": Converge at {goal}";
    }
}


/// <summary>
/// A negative-exponential constraint which encourages minimisation and is a hard constraint
/// at the max value.
/// </summary>
public class MinimiseConstraint : HardConstraint
{
    public override float BestValue => 0;
    public override float WorstValue => float.PositiveInfinity;

    public readonly float offset;


    public MinimiseConstraint(float max, float weight) : base(0, max, weight)
    {
        offset = max;
    }


    public MinimiseConstraint(ConstraintData data) : base(data)
    {
        offset = data.Max;
    }


    //public override float GetUnweightedFitness(float amount)
    //{
    //    // A negative-exponential graph gives an OF which tends to infinity
    //    // as you approach the limit, L.
    //    // Graph: y = -1 - (L/(x-L)), x <= L (minimise)
    //    if (float.IsPositiveInfinity(base.GetUnweightedFitness(amount)))
    //    {
    //        return float.PositiveInfinity;
    //    }

    //    float f = MathF.Abs(-1 - max / (amount - max));

    //    // If amount == max, sometimes this can give -Infinity, so assign a fitness of Infinity in this case.
    //    if (float.IsNegativeInfinity(f)) return float.PositiveInfinity;

    //    return f;
    //}


    public override float GetUnweightedFitness(float amount)
    {
        if (float.IsPositiveInfinity(base.GetUnweightedFitness(amount))) return float.PositiveInfinity;
        if (MathTools.Approx(amount, 0)) return 0;

        // Shift the graph to the right by max, as ConvergeConstraint's graph breaks down approaching goal == 0.
        // Then calculate the fitness with amount + max.
        float goal = 0;
        amount += max;
        goal += max;
        float tolerance = max; // max

        // Represents the magnitude of the derivative of the graph.
        // Needs to change with tolerance.
        // Take for example: 0 min < 30 goal < 50 max.
        // The graph represented by 0 min < 300 goal < 500 max should look the same on a graphing calculator (provided you zoom out
        // 10x), and the same for 0 < 3000 < 5000. This leads to reasonable convergence for all graphs.
        float speed = 1 / tolerance;

        float f_a = -MathF.Pow(goal / tolerance, 2) / speed;
        float f_b_num = -MathF.Pow(goal, 2);
        float f_b_denom = speed * (amount - (goal - tolerance)) * (amount - (goal + tolerance));

        // If amount has reached the limit, return Infinity. Removing this leads to -Infinity possibility.
        if (MathTools.Approx(f_b_denom, 0)) return float.PositiveInfinity;

        float f = f_a + f_b_num / f_b_denom;
        return f;
    }


    public override string Verbose()
    {
        return base.Verbose() + $": Minimise";
    }
}