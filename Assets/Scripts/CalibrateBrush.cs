using System.Linq;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
// Removed UnityEditor.ShaderGraph and System.IO imports as they are unnecessary for this logic
using System.IO; // Kept File access functions

public class CalibrateBrush : MonoBehaviour
{
    //########## TIMING WINDOW VARIABLES ####################################
    private const float COMBO_BUFFER_TIME = 0.1f; // Time in seconds to wait for the second button
    private bool comboTriggered = false; // Flag to ensure combo only fires once

    //########## OBJECT REFERENCES ####################################
    [SerializeField]
    private GameObject hand;
    [SerializeField]
    private GameObject hand_anchor;

    [SerializeField]
    private GameObject brush;

    [SerializeField]
    private GameObject _tracking_dot_brush;
    [SerializeField]
    private GameObject _tracking_dot_finger;

    [SerializeField]
    private OVRSkeleton _skeleton;

    public int bone_id = 10;

    // Renamed for clarity: These track the *current physical state* of the buttons
    private bool isCalibrateHeld = false;
    private bool isRecordHeld = false;

    //########### EXPERIMENT SETTINGS ###################################

    [Space]
    [Header("Experimental Controlls")]
    [Space]

    [SerializeField]
    bool hand_visible = true;
    [SerializeField]
    bool brush_visible = true;

    private OVRBone _indexTipBone;

    private VRControls _controls;

    [Space]
    [Tooltip("The shift of the virtual hand from the users actual hand. (The brush will need to be recalibrated)")]
    public Vector3 hand_offset;

    private Vector3 brush_offset;

    private string dataFilePath;


    //########### HELPER FUNCTIONS ###################################

    public void PerformCalibration()
    {
        if (_tracking_dot_brush == null || _tracking_dot_finger == null)
        {
            Debug.LogWarning("Tracking dots are not assigned. Cannot calibrate.");
            return;
        }
        // UnityEditor.Undo.RecordObject(this, "Calibrate Offset"); // Only usable in the Editor
        Vector3 dotOffsetWorld = _tracking_dot_brush.transform.position - brush.transform.position;

        // (This is the finger's position MINUS that offset).
        Vector3 targetBrushWorldPos = _tracking_dot_finger.transform.position - dotOffsetWorld;

        // We use InverseTransformPoint because we are converting a "point" in world space.
        brush_offset = brush.transform.parent.InverseTransformPoint(targetBrushWorldPos);
        Debug.Log("Calibration Performed (Single Action)");
    }

    public void PerformRecording()
    {
        if (_tracking_dot_brush == null)
        {
            Debug.LogWarning("Tracking dot is not found.");
            return;
        }

        // --- Existing Recording Logic ---
        float timestamp = Time.time;
        Vector3 position = _tracking_dot_brush.transform.position;
        string dataLine = $"{timestamp},{position.x},{position.y},{position.z}\n";
        try
        {
            File.AppendAllText(dataFilePath, dataLine);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }
        Debug.Log("Recording Performed (Single Action)");
    }

    public void PerformComboAction()
    {
        if (hand_visible && brush_visible)
        {
            // State 1 (Both) -> State 2 (Brush Only)
            hand_visible = false;
            brush_visible = true;
            Debug.Log("Combo Action: State 2 (Brush Only)");
        }
        else if (!hand_visible && brush_visible)
        {
            // State 2 (Brush Only) -> State 3 (Hand Only)
            hand_visible = true;
            brush_visible = false;
            Debug.Log("Combo Action: State 3 (Hand Only)");
        }
        else if (hand_visible && !brush_visible)
        {
            // State 3 (Hand Only) -> State 4 (Neither)
            hand_visible = false;
            brush_visible = false;
            Debug.Log("Combo Action: State 4 (Neither Visible)");
        }
        else
        {
            // State 4 (Neither) -> State 1 (Both)
            hand_visible = true;
            brush_visible = true;
            Debug.Log("Combo Action: State 1 (Both Visible)");
        }
    }


    //########### INPUT EVENT HANDLERS ###################################

    // --- Calibration Button Logic ---

    private void OnCalibratePressed(InputAction.CallbackContext context)
    {
        isCalibrateHeld = true;
        comboTriggered = false;

        if (isRecordHeld)
        {
            FireCombo();
        }
        else
        {
            // Calibrate pressed first, start the buffer
            Invoke(nameof(ExecuteCalibrateAction), COMBO_BUFFER_TIME);
        }
    }

    private void OnCalibrateReleased(InputAction.CallbackContext context)
    {
        isCalibrateHeld = false;
        // When released, we can always cancel any pending single action
        CancelInvoke(nameof(ExecuteCalibrateAction));
    }

    // --- Record Button Logic ---

    private void OnRecordPressed(InputAction.CallbackContext context)
    {
        isRecordHeld = true;
        comboTriggered = false;

        if (isCalibrateHeld)
        {
            // If the other button is ALREADY held, fire combo immediately
            FireCombo();
        }
        else
        {
            // Record pressed first, start the buffer
            Invoke(nameof(ExecuteRecordAction), COMBO_BUFFER_TIME);
        }
    }

    private void OnRecordReleased(InputAction.CallbackContext context)
    {
        isRecordHeld = false;
        CancelInvoke(nameof(ExecuteRecordAction));
    }

    // --- Delayed Action Execution ---

    private void ExecuteCalibrateAction()
    {
        if (isRecordHeld)
        {
            // Safety check: if Record was pressed at the very end of the buffer, fire combo
            FireCombo();
        }
        else
        {
            _indexTipBone = _skeleton.Bones.FirstOrDefault(b => b.Id == (OVRSkeleton.BoneId)bone_id);
            PerformCalibration();
        }
    }

    private void ExecuteRecordAction()
    {
        if (isCalibrateHeld)
        {
            // Safety check: if Calibrate was pressed at the very end of the buffer, fire combo
            FireCombo();
        }
        else
        {
            // The buffer expired, and the other button wasn't pressed
            PerformRecording();
        }
    }

    private void FireCombo()
    {
        if (comboTriggered) return;

        comboTriggered = true;

        // Immediately cancel any pending single actions
        CancelInvoke(nameof(ExecuteCalibrateAction));
        CancelInvoke(nameof(ExecuteRecordAction));

        PerformComboAction();
    }


    //########### UNITY LIFECYCLE ###################################

    private void Awake()
    {
        _controls = new VRControls();

        string fileName = "brush_stroke_data.csv";
        dataFilePath = Path.Combine("C:\\Users\\jonathan.h.1505\\Documents\\Pilot_Data_Collection", fileName);

        if (!File.Exists(dataFilePath))
        {
            string header = "Timestamp,PositionX,PositionY,PositionZ\n";
            File.WriteAllText(dataFilePath, header);
        }

        Debug.Log($"Data will be saved to: {dataFilePath}");
    }

    private void OnEnable()
    {
        // Subscribing to started and canceled events
        _controls.VRController.Calibrate.started += OnCalibratePressed;
        _controls.VRController.Calibrate.canceled += OnCalibrateReleased;

        _controls.VRController.Record.started += OnRecordPressed;
        _controls.VRController.Record.canceled += OnRecordReleased;

        _controls.VRController.Enable();
    }

    private void OnDisable()
    {
        _controls.VRController.Disable();

        // IMPORTANT: Use the correct event (started/canceled) for unsubscribing
        _controls.VRController.Calibrate.started -= OnCalibratePressed;
        _controls.VRController.Calibrate.canceled -= OnCalibrateReleased;

        _controls.VRController.Record.started -= OnRecordPressed;
        _controls.VRController.Record.canceled -= OnRecordReleased;
    }

    private void Initialize()
    {
        _indexTipBone = _skeleton.Bones.FirstOrDefault(b => b.Id == (OVRSkeleton.BoneId)bone_id);
    }
    void Start()
    {
        Invoke(nameof(Initialize), 0.5f);
    }

    void Update()
    {
        if (hand == null || brush == null || _tracking_dot_finger == null || _indexTipBone == null)
        {
            return;
        }

        hand.transform.position = hand_anchor.transform.position + hand_offset;
        hand.transform.rotation = hand_anchor.transform.rotation;
        brush.transform.localPosition = brush_offset;

        _tracking_dot_finger.transform.position = _indexTipBone.Transform.position;

        hand.SetActive(hand_visible);
        brush.SetActive(brush_visible);
    }
}