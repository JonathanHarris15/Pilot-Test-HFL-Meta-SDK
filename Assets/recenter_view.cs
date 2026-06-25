using UnityEngine;
using UnityEngine.InputSystem;

// Recenters the user by moving the XR Camera Rig (NOT the environment).
//
// The world stays fixed - the Environment, the calibration scan zone, and the
// frame your brush_stroke_data.csv is recorded in all keep their coordinates.
// Instead we teleport/rotate the rig so the headset lands on `target`, facing
// the target's yaw. Because the hands and brush are children of the rig, they
// move WITH the user and stay aligned with the world (no desync).
public class RecenterOnSpace : MonoBehaviour
{
    [Tooltip("The XR Camera Rig root to move. Leave empty to auto-use headset.root.")]
    public Transform cameraRig;

    [Tooltip("The headset (CenterEyeAnchor). Must be a child of the rig.")]
    public Transform headset;

    [Tooltip("Where the user should end up. The rig moves so the headset lands here (X/Z) facing this transform's yaw. Drag this anchor to the desired standing spot and rotate it to set facing.")]
    public Transform target;

    [Tooltip("Optional world-space position nudge applied after recentering.")]
    public Vector3 pos_offset;

    [Tooltip("Optional extra yaw (uses Y only), in degrees, added to the final facing.")]
    public Vector3 rot_offset;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Recenter();
        }
    }

    // Public so it can also be driven from a button / UnityEvent if needed.
    public void Recenter()
    {
        if (headset == null || target == null)
        {
            Debug.LogWarning("RecenterOnSpace: 'headset' and 'target' must both be assigned.", this);
            return;
        }

        // Fall back to the rig root if the rig wasn't wired explicitly.
        Transform rig = cameraRig != null ? cameraRig : headset.root;
        if (rig == null || rig == headset)
        {
            Debug.LogWarning("RecenterOnSpace: could not resolve a Camera Rig above the headset to move.", this);
            return;
        }

        Debug.Log("BOOM - Recentered (moved Camera Rig)!");

        // 1. YAW: rotate the rig about the user's HEAD so they end up facing the
        //    target's yaw. Pivoting about the head means the user spins in place -
        //    no nausea and no being flung across the room (fixes the pivot issue).
        //    Yaw only, so we never introduce pitch/roll tilt on the world.
        float targetYaw = target.eulerAngles.y + rot_offset.y;
        float deltaYaw = Mathf.DeltaAngle(headset.eulerAngles.y, targetYaw);
        rig.RotateAround(headset.position, Vector3.up, deltaYaw);

        // 2. POSITION: slide the rig horizontally so the head lands on the target.
        //    Read headset.position AFTER the rotation (its X/Z are unchanged by a
        //    yaw about itself). Keep the user's real physical height - correct X/Z only.
        Vector3 flatDelta = target.position - headset.position;
        flatDelta.y = 0f;
        rig.position += flatDelta + pos_offset;
    }
}
