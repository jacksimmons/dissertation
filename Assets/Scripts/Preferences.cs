using System;


public enum WeightGoal
{
    MaintainWeight,
    LoseWeight,
    GainWeight
}


public enum AssignedSex
{
    Male,
    Female
}


/// <summary>
/// A class which saves the user's preferences when serialized into a file.
/// </summary>
[Serializable]
public class Preferences : ICached
{
    public static Preferences Saved { get; private set; } = new();
    public void Cache() { Saved = this; }


    // Food Groups
    public bool eatsLandMeat; // Carnivore, Lactose-Intolerant
    public bool eatsSeafood; // Carnivore, Pescatarian, LI
    public bool eatsAnimalProduce; // Vegetarian, LI
    public bool eatsLactose; // Vegetarian, i.e. no Milk


    // Body
    public WeightGoal weightGoal;
    public float weightInKG;
    public float heightInCM;
    public AssignedSex assignedSex;


    // By default, the user's settings should permit every food type - this
    // best fits the average person.
    public Preferences()
    {
        eatsLandMeat = true;
        eatsSeafood = true;
        eatsAnimalProduce = true;
        eatsLactose = true;
        weightGoal = WeightGoal.MaintainWeight;
        weightInKG = 70;
        heightInCM = 170;
        assignedSex = AssignedSex.Male;
    }
}