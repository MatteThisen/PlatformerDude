using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform playerObject; // The target the camera will follow
    [SerializeField] private float cameraOffsetZ = 0f;
    [SerializeField] private float cameraSize;
    private Vector3 offset; // The offset from the target position

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        offset = new Vector3(3, 3, cameraOffsetZ);
        GetComponent<Camera>().orthographicSize = cameraSize;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, playerObject.position + offset, Time.deltaTime * 5f);


    }
}
