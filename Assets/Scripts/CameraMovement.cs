using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform playerObject; // The target the camera will follow
    private Vector3 offset = new Vector3(3, 3, -10); // The offset from the target position

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, playerObject.position + offset, Time.deltaTime * 5f);


    }
}
