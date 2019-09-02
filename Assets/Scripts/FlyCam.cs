using UnityEngine;

public class FlyCam : MonoBehaviour
{
    public float VerticalSensitivity = 2.0f;
    public float HorizontalSensitivity = 2.0f;

    float pitch = 0.0f;
    float yaw = 0.0f;

    void GrabCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        GrabCursor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReleaseCursor();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            GrabCursor();
        }

        if (Cursor.lockState != CursorLockMode.None)
        {
            pitch -= Input.GetAxis("Mouse Y") * VerticalSensitivity;
            pitch = Mathf.Clamp(pitch, -89.0f, 89.0f);

            yaw += Input.GetAxis("Mouse X") * HorizontalSensitivity;
            if (yaw < -180.0f) yaw += 360.0f;
            if (yaw > 180.0f) yaw -= 360.0f;

            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

        transform.Translate(
            transform.right * Input.GetAxis("XMove") +
            Vector3.up * Input.GetAxis("YMove") +
            transform.forward * Input.GetAxis("ZMove"),
            Space.World
        );
    }
}
