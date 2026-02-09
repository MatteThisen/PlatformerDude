using UnityEngine;
using UnityEngine.Splines;

public class SplineBoxBehaviour : MonoBehaviour
{

    [SerializeField] private SplineInstantiate endPointInstantiator;
    [SerializeField] private SplineContainer splineContainer;

    private void Awake()
    {
        float splineLength = splineContainer.CalculateLength();
        endPointInstantiator.MaxSpacing = splineLength;
        endPointInstantiator.MinSpacing = splineLength;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
