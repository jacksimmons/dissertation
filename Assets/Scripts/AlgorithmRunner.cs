using UnityEngine;

public class AlgorithmRunner : MonoBehaviour
{
    private Algorithm m_algorithm;


    private void Start()
    {
        m_algorithm = new GeneticAlgorithm();
        m_algorithm.Run();
    }
}