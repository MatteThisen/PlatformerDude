using UnityEngine;

public class HookRope : MonoBehaviour
{
    
    private GameObject player;
    private GameObject hookObject;
    private LineRenderer lineRenderer;

    private Vector2 hookAttachPointOffset = new Vector2(0f, -0.25f);
    private Vector2 playerAttachPointOffset = new Vector2(0f, 0.35f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player");
        hookObject = GameObject.Find("PlayerHook");
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 hookAttachPoint = (Vector2)hookObject.transform.position + hookAttachPointOffset;
        Vector2 playerAttachPoint = (Vector2)player.transform.position + playerAttachPointOffset;
        lineRenderer.SetPosition(0, hookAttachPoint);
        lineRenderer.SetPosition(1, playerAttachPoint);
    }
}
