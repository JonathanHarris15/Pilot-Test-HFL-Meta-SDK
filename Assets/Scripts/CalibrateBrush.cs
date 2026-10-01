using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Handles brush-to-finger calibration, hand/brush visibility, and the controller
// input. Recording of experiment data now lives in ExperimentDataManager - the
// controller 'Record' button routes into it (see ExecuteRecordAction).
public class CalibrateBrush : MonoBehaviour
{
    //########## TIMING WINDOW VARIABLES ####################################
    private const float COMBO_BUFFER_TIME = 0.1f;
    private bool comboTriggered = false;

    //########## OBJECT REFERENCES ####################################
    [SerializeField]
    private GameObject hand;
    [SerializeField]
    private GameObject index_finger;
    [SerializeField]
    private GameObject button;

    [SerializeField]
    private GameObject brush;

    [SerializeField]
    private GameObject _tracking_dot_brush;
    [SerializeField]
    private GameObject _tracking_dot_finger;

    [SerializeField]
    private OVRSkeleton _skeleton;

    public int bone_id = 10;

    private bool isCalibrateHeld = false;
    private bool isRecordHeld = false;

    //########### EXPERIMENT SETTINGS ###################################

    [Space]
    [Header("Experimental Controls")]
    [Space]

    [SerializeField]
    bool hand_visible = true;
    [SerializeField]
    bool brush_visible = true;

    private VRControls _controls;
    private ExperimentDataManager _experiment;

    [Space]
    [Tooltip("The shift of the virtual hand from the users actual hand (world space). " +
             "The virtual brush receives the exact same shift, so calibration never has to be redone when this changes.")]
    public Vector3 hand_offset;

    [Tooltip("The fixed rotation of the brush relative to the controller (in degrees).")]
    public Vector3 brush_rotation_offset;

    [Tooltip("TrackingSpace of the camera rig. The hand is placed in this frame so it stays aligned " +
             "with the controller after the rig is recentered. Auto-found from OVRCameraRig if left empty.")]
    [SerializeField]
    private Transform tracking_space;

    [Tooltip("Frames averaged per calibration to smooth out hand-tracking jitter on the fingertip.")]
    [SerializeField]
    private int calibration_frames = 30;

    [Tooltip("Warn if the fingertip moved more than this (metres, relative to the controller) during calibration.")]
    [SerializeField]
    private float calibration_max_spread = 0.005f;

    // The REAL brush contact point, expressed in the controller's local space.
    // Never contains the hand shift - the shift is added in world space every frame.
    private Vector3 _tipLocal;
    private bool _isCalibrating;

    private Transform Controller => brush != null ? brush.transform.parent : null;

    //########### LIVE SAMPLE VALUES (read by ExperimentDataManager) ###########

    // Physical (unshifted) brush contact point X in world space.
    public float CurrentBrushX =>
        Controller != null ? Controller.TransformPoint(_tipLocal).x : 0f;

    // Button X position.
    public float CurrentButtonX =>
        button != null ? button.transform.position.x : 0f;

    //########### HELPER FUNCTIONS ###################################

    // Records where the real brush touches the real index finger. Hold the real
    // brush on the fingertip and keep both still for ~calibration_frames frames.
    // Works with or without hand_offset applied: the shift is removed from the
    // virtual fingertip before it is stored, so the result is always "real on real".
    public void PerformCalibration()
    {
        if (index_finger == null || Controller == null || _tracking_dot_brush == null)
        {
            Debug.LogWarning("Index finger, brush or brush tracking dot is not assigned. Cannot calibrate.");
            return;
        }
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Calibration only works in Play mode.");
            return;
        }
        if (!hand.activeInHierarchy)
        {
            Debug.LogWarning("Hand is hidden, so finger tracking is paused. Make the hand visible before calibrating.");
            return;
        }
        if (_isCalibrating) return;

        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        _isCalibrating = true;
        int frames = Mathf.Max(1, calibration_frames);
        var samples = new Vector3[frames];
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < frames; i++)
        {
            yield return null;
            // Undo the hand shift to get the real fingertip, then express it
            // relative to the controller so head/body motion doesn't matter.
            Vector3 realFinger = index_finger.transform.position - hand_offset;
            samples[i] = Controller.InverseTransformPoint(realFinger);
            sum += samples[i];
        }

        Vector3 mean = sum / frames;
        float spread = 0f;
        foreach (var s in samples) spread = Mathf.Max(spread, Vector3.Distance(s, mean));

        _tipLocal = mean;
        _isCalibrating = false;

        string msg = $"Calibration Performed ({frames} frames, max deviation {spread * 1000f:F1} mm)";
        if (spread > calibration_max_spread)
            Debug.LogWarning(msg + " - hand or brush moved during calibration, consider repeating.");
        else
            Debug.Log(msg);
    }

    // Places the hand and brush for this frame. Both get the identical world-space
    // shift, so the virtual contact point always matches the real one.
    private void ApplyPoses()
    {
        if (hand == null || brush == null || Controller == null || _tracking_dot_brush == null)
            return;

        // Hand: the XR Hands prefab reports joints in tracking space, so its root
        // must follow the rig's TrackingSpace (otherwise a recenter desyncs it from
        // the controller), plus the experimental shift.
        // OVRCameraRig only creates trackingSpace in its own Awake, so resolve lazily.
        if (tracking_space == null)
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null) tracking_space = rig.trackingSpace;
        }

        if (tracking_space != null)
            hand.transform.SetPositionAndRotation(tracking_space.position + hand_offset, tracking_space.rotation);
        else
            hand.transform.position = hand_offset;

        if (_tracking_dot_finger != null && index_finger != null)
            _tracking_dot_finger.transform.position = index_finger.transform.position;

        // Brush: fixed rotation, then translate so the tip lands on the real
        // contact point plus the same shift the hand received.
        brush.transform.localRotation = Quaternion.Euler(brush_rotation_offset);
        Vector3 targetTip = Controller.TransformPoint(_tipLocal) + hand_offset;
        brush.transform.position += targetTip - _tracking_dot_brush.transform.position;
    }

    public void PerformComboAction()
    {
        if (hand_visible && brush_visible)
        {
            hand_visible = false;
            brush_visible = true;
            Debug.Log("Combo Action: State 2 (Brush Only)");
        }
        else if (!hand_visible && brush_visible)
        {
            hand_visible = true;
            brush_visible = false;
            Debug.Log("Combo Action: State 3 (Hand Only)");
        }
        else if (hand_visible && !brush_visible)
        {
            hand_visible = false;
            brush_visible = false;
            Debug.Log("Combo Action: State 4 (Neither Visible)");
        }
        else
        {
            hand_visible = true;
            brush_visible = true;
            Debug.Log("Combo Action: State 1 (Both Visible)");
        }
    }


    //########### INPUT EVENT HANDLERS ###################################

    private void OnCalibratePressed(InputAction.CallbackContext context)
    {
        isCalibrateHeld = true;
        comboTriggered = false;

        if (isRecordHeld) FireCombo();
        else Invoke(nameof(ExecuteCalibrateAction), COMBO_BUFFER_TIME);
    }

    private void OnCalibrateReleased(InputAction.CallbackContext context)
    {
        isCalibrateHeld = false;
        CancelInvoke(nameof(ExecuteCalibrateAction));
    }

    private void OnRecordPressed(InputAction.CallbackContext context)
    {
        isRecordHeld = true;
        comboTriggered = false;

        if (isCalibrateHeld) FireCombo();
        else Invoke(nameof(ExecuteRecordAction), COMBO_BUFFER_TIME);
    }

    private void OnRecordReleased(InputAction.CallbackContext context)
    {
        isRecordHeld = false;
        CancelInvoke(nameof(ExecuteRecordAction));
    }

    private void ExecuteCalibrateAction()
    {
        if (isRecordHeld) FireCombo();
        else
        {
            PerformCalibration();
        }
    }

    private void ExecuteRecordAction()
    {
        if (isCalibrateHeld)
        {
            FireCombo();
            return;
        }

        if (_experiment == null)
        {
            Debug.LogWarning("No ExperimentDataManager on this GameObject - cannot record.");
            return;
        }

        if (!_experiment.RecordCurrent(out string err) && !string.IsNullOrEmpty(err))
            Debug.LogWarning(err);
    }

    private void FireCombo()
    {
        if (comboTriggered) return;
        comboTriggered = true;
        CancelInvoke(nameof(ExecuteCalibrateAction));
        CancelInvoke(nameof(ExecuteRecordAction));
        PerformComboAction();
    }

    //########### UNITY LIFECYCLE ###################################

    private void Awake()
    {
        _controls = new VRControls();
        _experiment = GetComponent<ExperimentDataManager>();

        // Until calibrated, keep the brush tip where it was authored on the controller.
        if (brush != null && Controller != null && _tracking_dot_brush != null)
        {
            brush.transform.localRotation = Quaternion.Euler(brush_rotation_offset);
            _tipLocal = Controller.InverseTransformPoint(_tracking_dot_brush.transform.position);
        }
    }

    private void OnEnable()
    {
        Application.onBeforeRender += ApplyPoses;
        _controls.VRController.Calibrate.started += OnCalibratePressed;
        _controls.VRController.Calibrate.canceled += OnCalibrateReleased;
        _controls.VRController.Record.started += OnRecordPressed;
        _controls.VRController.Record.canceled += OnRecordReleased;
        _controls.VRController.Enable();
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= ApplyPoses;
        _isCalibrating = false; // disabling stops the coroutine
        _controls.VRController.Disable();
        _controls.VRController.Calibrate.started -= OnCalibratePressed;
        _controls.VRController.Calibrate.canceled -= OnCalibrateReleased;
        _controls.VRController.Record.started -= OnRecordPressed;
        _controls.VRController.Record.canceled -= OnRecordReleased;
    }

    void Update()
    {
        if (hand == null || brush == null)
        {
            return;
        }

        // Visibility
        hand.SetActive(hand_visible);
        brush.SetActive(brush_visible);
    }

    // Poses are applied after all tracking updates (and again right before
    // rendering, in case the rig late-latches controller/hand poses).
    void LateUpdate()
    {
        ApplyPoses();
    }
}
