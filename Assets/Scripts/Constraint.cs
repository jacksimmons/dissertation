﻿/// A constraint nudges (or forces, if a hard constraint is used) the algorithm towards a more
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
[Serializable]
public class ConstraintData : IVerbose
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


    public string Verbose()
    {
        return $"Min: {Min,8:F3} Max: {Max,8:F3} Goal: {Goal,8:F3} Weight: {Weight,8:F3} Type: {Type,20}\n";
    }
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
        {
            Logger.Error($"Invalid Constraint type: {data.Type}.");
            return new NullConstraint();
        }

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
        return weight * GetUnweightedFitness(amount);
    }


    public abstract float GetUnweightedFitness(float amount);
}


/// <summary>
/// A constraint which gives a fitness of 0 if in a specified range, (min, max).
/// Otherwise, gives an infinite fitness.
/// </summary>
public class HardConstraint : Constraint, IVerbose
{
    /// <summary>
    /// (Exclusive) Minimum value.
    /// </summary>
    public readonly float min;
    /// <summary>
    /// (Exclusive) Maximum value.
    /// </summary>
    public readonly float max;

    public readonly EFitnessFunc fitnessFuncType;

    public override float BestValue => (min + max) / 2;
    public override float WorstValue => float.PositiveInfinity;


    public HardConstraint(float min, float max, float weight) : base(weight)
    {
        this.min = min;
        this.max = max;

        Init();
    }


    public HardConstraint(ConstraintData data) : base(data.Weight)
    {
        min = data.Min;
        max = data.Max;

        Init();
    }


    /// <summary>
    /// Do not inherit this. Base constructor is called first, so HardConstraint will check the params of
    /// the inherited class before the inherited constructor has begun.
    /// </summary>
    protected void Init()
    {
        if (max < min)
            Logger.Error($"Argument max ({max}) was less than argument min ({min}).");
        if (weight < 0)
            Logger.Error($"Argument weight ({weight}) was < 0.");

        switch (fitnessFuncType)
        {
            case EFitnessFunc.Exponential:
            case EFitnessFunc.ManhattanDist:
                break;
            default:
                Logger.Error($"Argument fitnessFuncType ({Preferences.Instance.fitnessFunc}) was invalid.");
                break;
        }
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


    public virtual string Verbose()
    {
        return $"Hard constraint [{min}, {max}]";
    }


    protected static float ExponentialFitness(float amount, float min, float max, float goal)
    {
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
        float speed = 1 / tolerance;

        float f_a = -MathF.Pow(goal / tolerance, 2) / speed;
        float f_b_num = -MathF.Pow(goal, 2);
        float f_b_denom = speed * (amount - (goal - tolerance)) * (amount - (goal + tolerance));

        // If amount has reached the limit, return Infinity. Removing this leads to -Infinity possibility.
        if (MathTools.Approx(f_b_denom, 0)) return float.PositiveInfinity;

        return f_a + f_b_num / f_b_denom;
    }


    protected static float ManhattanFitness(float amount, float goal)
    {
        return Math.Abs(amount - goal);
    }
}


/// <summary>
/// A constraint which encourages convergence on a specific value (the limit), and acts as a
/// hard constraint at the same time. Fitness increases linearly with deviation from convergence
/// point, and becomes infinity if outside the specified bounds.
/// Graph: y = -L^2/(ST^2) - L^2/S[(x-(L-T))(x-(L+T))], L - T < x < L + T
/// </summary>
public class ConvergeConstraint : HardConstraint, IVerbose
{
    public readonly float goal;


    public override float BestValue => goal;
    public override float WorstValue => float.PositiveInfinity;


    public ConvergeConstraint(float goal, float min, float max, float weight)
        : base(min, max, weight)
    {
        this.goal = goal;

        Init();
    }


    public ConvergeConstraint(ConstraintData data) : base(data)
    {
        goal = data.Goal;

        Init();
    }


    private new void Init()
    {
        base.Init();

        if (goal < 0 || goal < min || goal > max)
            Logger.Error($"Goal: {goal} is out of range. It must be >= 0, >= min ({min}) and <= max ({max}).");
    }


    public override float GetUnweightedFitness(float amount)
    {
        if (float.IsPositiveInfinity(base.GetUnweightedFitness(amount))) return float.PositiveInfinity;
        // If amount == max, sometimes this can give -Infinity, so assign a fitness of Infinity in this case.

        switch (Preferences.Instance.fitnessFunc)
        {
            case EFitnessFunc.Exponential:
                return ExponentialFitness(amount, min, max, goal);
            case EFitnessFunc.ManhattanDist:
                return ManhattanFitness(amount, goal);
            default:
                // Unlikely to reach this due to exception handling in HardConstraint.
                return float.NegativeInfinity;
        }
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
public class MinimiseConstraint : HardConstraint, IVerbose
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

        return Preferences.Instance.fitnessFunc switch
        {
            // Shift the graph to the right by max, as ConvergeConstraint's graph breaks down approaching goal == 0.
            // Then calculate the fitness with amount + max.
            EFitnessFunc.Exponential => ExponentialFitness(amount + max, min + max, max + max, max),
            EFitnessFunc.ManhattanDist => ManhattanFitness(amount, 0),
            _ => float.NegativeInfinity, // Unlikely to reach this due to exception handling in HardConstraint.
        };
    }


    public override string Verbose()
    {
        return base.Verbose() + $": Minimise";
    }
}


/// <summary>
/// A constraint which renders a nutrient "unconsidered". Returns a fitness of 0 for all values provided.
/// </summary>
public class NullConstraint : HardConstraint, IVerbose
{
    public NullConstraint() : base(float.NegativeInfinity, float.PositiveInfinity, 1) { }
    public NullConstraint(ConstraintData _) : base(float.NegativeInfinity, float.PositiveInfinity, 1) { }
}