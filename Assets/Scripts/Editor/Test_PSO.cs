using NUnit.Framework;

public class Test_PSO : Test_HasPopulation
{
    private static AlgPSO PSO() => (AlgPSO)Algorithm.Build(typeof(AlgPSO));


    [Test]
    public void RunTests()
    {
        Assert.True(false);
    }
}