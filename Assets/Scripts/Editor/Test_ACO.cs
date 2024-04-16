using NUnit.Framework;

public class Test_ACO : Test_HasPopulation
{
    private static AlgACO Alg
    {
        get
        {
            AlgACO val = (AlgACO)Algorithm.Build(typeof(AlgACO));
            val.Init();
            return val;
        }
    }


    [Test]
    public void RunTests()
    {
        Preferences.Instance.Reset();

        PheroTest(Alg);
        UpdateSearchSpaceTest(Alg);
        FitnessTest(Alg);
    }


    private void PheroTest(AlgACO aco)
    {
        Assert.True(false);
    }


    private void UpdateSearchSpaceTest(AlgACO aco)
    {
    }
}