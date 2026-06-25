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
    [Tooltip("The shift of the virtual hand from the users actual hand.")]
    public Vector3 hand_offset;

    [Tooltip("The fixed rotation of the brush relative to the controller (in degrees).")]
    public Vector3 brush_rotation_offset;

    private Vector3 brush_offset;

    //########### LIVE SAMPLE VALUES (read by ExperimentDataManager) ###########

    // Calibrated brush-point X (tracking dot minus the calibration offset).
    public float CurrentBrushX =>
        _tracking_dot_brush != null ? _tracking_dot_brush.transform.position.x - brush_offset.x : 0f;

    // Button X position.
    public float CurrentButtonX =>
        button != null ? button.transform.position.x : 0f;

    //########### HELPER FUNCTIONS ###################################

    //Brings the brush to the virtual hands index finger
    public void PerformCalibration()
    {
        if (_tracking_dot_brush == null || _tracking_dot_finger == null)
        {
            Debug.LogWarning("Tracking dots are not assigned. Cannot calibrate.");
            return;
        }

        Vector3 dotOffsetWorld = _tracking_dot_brush.transform.position - brush.transform.position;
        Vector3 targetBrushWorldPos = _tracking_dot_finger.transform.position - dotOffsetWorld;

        brush_offset = brush.transform.parent.InverseTransformPoint(targetBrushWorldPos);
        Debug.Log("Calibration Performed (Position Synced)");
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
    }

    private void OnEnable()
    {
        _controls.VRController.Calibrate.started += OnCalibratePressed;
        _controls.VRController.Calibrate.canceled += OnCalibrateReleased;
        _controls.VRController.Record.started += OnRecordPressed;
        _controls.VRController.Record.canceled += OnRecordReleased;
        _controls.VRController.Enable();
    }

    private void OnDisable()
    {
        _controls.VRController.Disable();
        _controls.VRController.Calibrate.started -= OnCalibratePressed;
        _controls.VRController.Calibrate.canceled -= OnCalibrateReleased;
        _controls.VRController.Record.started -= OnRecordPressed;
        _controls.VRController.Record.canceled -= OnRecordReleased;
    }

    void Update()
    {
        if (hand == null || brush == null || _tracking_dot_finger == null)
        {
            return;
        }

        // Hand Offset
        hand.transform.position = hand_offset;
        _tracking_dot_finger.transform.position = index_finger.transform.position;

        // Brush tracks the calibrated offset relative to its parent.
        brush.transform.localPosition = brush_offset;
        brush.transform.localRotation = Quaternion.Euler(brush_rotation_offset);

        // Visibility
        hand.SetActive(hand_visible);
        brush.SetActive(brush_visible);
    }
}
