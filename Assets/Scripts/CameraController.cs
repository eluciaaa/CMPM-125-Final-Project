using UnityEngine;

public class CameraController : MonoBehaviour

{
    private Camera followCamera;
    public MeshRenderer followObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        followCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (followObj != null)
        {
            followCamera.transform.position = followObj.transform.position + new Vector3(0f, 12f, -15f);
        }
    }
}
