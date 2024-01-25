using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum ProximateType
{
    Protein,
    Fat,
    Carbs,
    Kcal,
    Sugar,
    SatFat,
    TransFat
}


public enum ConstraintType
{
    Minimise,
    Converge,
    Range
}


public readonly struct Constraint
{
    public readonly ConstraintType Type;
    public readonly float Weight;
    public readonly float Goal;


    public Constraint(ConstraintType type, float weight, float goal)
    {
        Type = type;
        Weight = weight;
        Goal = goal;
    }


    public float GetFitness(float value)
    {
        float f;
        float goal_pow = MathF.Pow(Goal, 4);
        switch (Type)
        {
            // Note that only x > 0 is considered for these graphs.

            // Legend:
            // - OF = Objective Function
            // - k = Critical point (Maximum, or Convergence point)

            case ConstraintType.Minimise:
                // A negative-exponential graph gives an OF which gives a very high negative value
                // as you approach the critical point, k.
                // Graph: y = 1 + (1/k) - (1/(k-x)), x < k
                f = 1 + (1 / Goal) - 1 / (Goal - value);
                if (value >= Goal) return float.MinValue;
                return f;

            case ConstraintType.Converge:
                // (y = 1-(1/h^4)(x-h)^4 graph)
                // Where h is the critical point.
                f = 1 - MathF.Pow(value - Goal, 4) / goal_pow;
                if (f < 0) return 0;
                return f;
        }

        return 0;
    }
}
