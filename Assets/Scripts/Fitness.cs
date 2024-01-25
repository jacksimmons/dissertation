public enum FitnessLevel
{
    Day = 1,
    Portion = 2,
}


public readonly struct Fitness
{
    // The "level" of fitness a multiplier on the fitness value
    // A higher level indicates the fitness was evaluated at a lower class level
    public readonly FitnessLevel Level;
    private readonly float _rawValue;
    public readonly float Value
    {
        get
        {
            return _rawValue * (int)Level;
        }
    }


    public Fitness(FitnessLevel level, float rawValue)
    {
        Level = level;
        _rawValue = rawValue;
    }
}