using NUnit.Framework;
using static AlgPSO;
using System.Reflection;
using static AlgACO;
using System;


/// <summary>
/// Performs tests on the vector calculation methods used in PSO.
/// </summary>
public class Test_PSO : Test_HasPopulation
{
    [Test]
    public void RunTests()
    {
        AlgPSO pso = (AlgPSO)Algorithm.Build(typeof(AlgPSO));
        pso.Init();

        NormalTest();
        BoundaryTest();
        ErroneousTest();

        FitnessTest(pso);
    }


    private void NormalTest()
    {
        ParticleVector a = new(new float[] { 1, 2, 3, 4, 5 });
        ParticleVector b = new(new float[] { 6, 7, 8, 9, 10 });
        float[] expected = new float[] { 19, 23, 27, 31, 35 };
        MultAndAddTest(a, b, 3, expected);
    }


    private void BoundaryTest()
    {
        ParticleVector a = new(new float[] { 1, 2, 3, 4, 5 });
        ParticleVector b = new(new float[] { 6, 7, 8, 9, 10 });
        float[] expected = new float[] { 1, 2, 3, 4, 5 };
        MultAndAddTest(a, b, 0, expected);

        // Negative scalar; normalising gives positive version of the output
        expected = new float[] { -5, -5, -5, -5, -5 };
        ParticleVector output = MultAndAddTest(a, b, -1, expected);
        output.Normalise();
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] < 0)
            {
                Assert.That(output[i] == 0);
            }
            else
            {
                Assert.That(MathTools.Approx(expected[i], output[i]));
            }
        }

        // Negative vector; normalising gives positive version of the output
        output = MultAndAddTest(a, new(new float[] { -6, -7, -8, -9, -10 }), 1, expected);
        output.Normalise();
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] < 0)
            {
                Assert.That(output[i] == 0);
            }
            else
            {
                Assert.That(MathTools.Approx(expected[i], output[i]));
            }
        }
    }


    private void ErroneousTest()
    {
        ParticleVector a = new(new float[] { 1, 2, 3, 4, 5 });
        ParticleVector b = new(new float[] { 6, 7 });

        Assert.Throws(typeof(WarnException), () => ParticleVector.MultAndAdd(a, b));
        Assert.Throws(typeof(WarnException), () => ParticleVector.MultAndAdd(a, a, float.PositiveInfinity));
        Assert.Throws(typeof(WarnException), () => ParticleVector.MultAndAdd(a, a, float.NegativeInfinity));
    }


    private ParticleVector MultAndAddTest(ParticleVector a, ParticleVector b, float scalar, float[] expected)
    {
        ParticleVector output = ParticleVector.MultAndAdd(a, b, scalar);

        for (int i = 0; i < 5; i++)
        {
            Assert.That(MathTools.Approx(output[i], expected[i]));
        }

        return output;
    }
}