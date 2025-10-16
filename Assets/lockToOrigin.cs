using UnityEngine;

// This script provides a simple way to recenter the VR view
// using a controller button or a keyboard key.
public class ViewRecenter : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        // Check for the 'A' button on the right Oculus Touch controller
        // OVRInput.Button.One is typically the 'A' button on the right controller
        // and 'X' on the left.
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            RecenterView();
        }
    }

    /// <summary>
    /// Calls the OVRManager to recenter the tracking pose.
    /// </summary>
    public void RecenterView()
    {
        Debug.Log("Recentering VR View...");

        // Check if an OVRManager instance exists
        if (OVRManager.instance != null)
        {
            // NEW METHOD for recent SDK versions:
            OVRManager.display.RecenterPose();
        }
        else
        {
            Debug.LogWarning("OVRManager instance not found. Cannot recenter view.");
        }

        // OLD, DEPRECATED METHOD:
        // OVRManager.instance.RecenterTrackingOrigin();
    }
}