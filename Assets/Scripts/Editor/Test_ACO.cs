using NUnit.Framework;

public class Test_ACO : Test_HasPopulation
{
    [Test]
    public void RunTests()
    {
        Preferences.Instance.CalculateDefaultConstraints();

        AlgACO aco = (AlgACO)Algorithm.Build(typeof(AlgACO));
        aco.Init();

        PheroTest(aco);
        FitnessTest(aco);
    }


    private void PheroTest(AlgACO aco)
    {
        Assert.True(false);
    }


    public void BoundaryTest()
    {
    }


    public void ErroneousTest()
    {
    }
}