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
/// Abstract base class for Constraints.
/// </summary>
public abstract class Constraint
{
    public abstract float BestValue { get; }
    public abstract float WorstValue { get; }


    public static Constraint Build(ConstraintData data)
    {
        return (Constraint)Activator.CreateInstance(Type.GetType(data.type), data)!;
    }


    public abstract float GetFitness(float amount);


    public abstract string Verbose();
}


public struct ConstraintData
{
    public float min;
    public float max;
    public float goal;
    public string type;
}


/// <summary>
/// A constraint which gives a fitness of 0 if in a specified range.
/// Otherwise, gives an infinite fitness.
/// Graph is a conditional function:
///     { infinity, x < Min
/// y = { infinity, x > Max
///     { 0, otherwise
///     
/// It is constructed as 
public class HardConstraint : Constraint
{
    public readonly float min;
    public readonly float max;


    public override float BestValue => (min + max) / 2;
    public override float WorstValue => float.PositiveInfinity;


    public HardConstraint(float min, float max) : base()
    {
        this.min = min;
        this.max = max;

        if (max < min)
            Logger.Log($"Argument max ({max}) was less than argument min ({min}).", Severity.Error);
    }


    public HardConstraint(ConstraintData data) : base()
    {
        min = data.min;
        max = data.max;

        if (max < min)
            Logger.Log($"Argument max ({max}) was less than argument min ({min}).", Severity.Error);
    }


    public override float GetFitness(float amount)
    {
        if (amount < min || amount > max)
            return float.PositiveInfinity;
        return 0;
    }


    public override string Verbose()
    {
        return $"Hard constraint [{min}, {max}]";
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


    public MinimiseConstraint(float max) : base(0, max)
    {
        if (max <= 0)
            Logger.Log($"Argument max ({max}) was <= 0.", Severity.Error);
    }


    public MinimiseConstraint(ConstraintData data) : base(data) { }


    public override float GetFitness(float amount)
    {
        // A negative-exponential graph gives an OF which tends to infinity
        // as you approach the limit, L.
        // Graph: y = -1 - (L/(x-L)), x <= L (minimise)
        if (float.IsPositiveInfinity(base.GetFitness(amount))) return float.PositiveInfinity;

        // If x == max, sometimes this can give -Infinity.
        return MathF.Abs(-1 - max / (amount - max));
    }


    public override string Verbose()
    {
        return base.Verbose() + $": Minimise";
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


    public ConvergeConstraint(float goal, float min, float max)
        : base(min, max)
    {
        this.goal = goal;

        if (goal <= 0)
            Logger.Log($"Goal: {goal} is out of range. It must be > 0.", Severity.Error);
    }


    public ConvergeConstraint(ConstraintData data) : base(data)
    {
        goal = data.goal;

        if (goal <= 0 || goal < min || goal > max)
            Logger.Log($"Goal: {goal} is out of range. It must be > 0, > min ({min}) and < max ({max}).", Severity.Error);
    }


    public override float GetFitness(float amount)
    {
        if (float.IsPositiveInfinity(base.GetFitness(amount))) return float.PositiveInfinity;

        //if (amount > goal)
        //{
        //    // Use graph for > goal:
        //    // y = (max/(goal-max)) - (max/(x-max)).
        //    // Constant first term was calculated with a boundary condition: -k - (max/(goal-max)) = 0.

        //    return max / (goal - max) - max / (amount - max);
        //}
        //else if (amount < goal)
        //{
        //    // Use graph for < goal:
        //    // y = (min/(x-min)) - (min/(goal-min))
        //    // Constant second term calculated with a similar boundary condition.

        //    return min / (amount - min) - min / (goal - min);
        //}

        //return 0;

        float tolerance = MathF.Max(goal - min, max - goal);
        float steepness = 0.0001f;

        float f_a = -MathF.Pow(goal / tolerance, 2) / steepness;
        float f_b_num = -MathF.Pow(goal, 2);
        float f_b_denom = steepness * (amount - (goal - tolerance)) * (amount - (goal + tolerance));

        return f_a + f_b_num / f_b_denom;
    }


    public override string Verbose()
    {
        return base.Verbose() + $": Converge at {goal}";
    }
}