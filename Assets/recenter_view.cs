using UnityEngine;
using UnityEngine.InputSystem;

public class RecenterOnSpace : MonoBehaviour
{
    public GameObject environment;
    public GameObject headset;

    public Vector3 pos_offset;
    public Vector3 rot_offset;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("BOOM - Environment Recentered!");

            // 1. POSITION: Match the headset's X and Z, but keep the environment's current Y (floor height)
            Vector3 flatPosition = new Vector3(headset.transform.position.x, environment.transform.position.y, headset.transform.position.z);
            environment.transform.position = flatPosition + pos_offset;

            // 2. ROTATION: Grab ONLY the left/right rotation (Yaw) from the headset using eulerAngles
            float headsetYaw = headset.transform.eulerAngles.y;

            // Apply that Yaw to the environment, keeping Pitch and Roll at 0. 
            // We also add your rot_offset in case you need to fine-tune the final facing direction!
            environment.transform.eulerAngles = new Vector3(0, headsetYaw, 0) + rot_offset;
        }
    }
}