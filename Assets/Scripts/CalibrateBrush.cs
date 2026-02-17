using System.Linq;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using Oculus.VoiceSDK.UX;

public class CalibrateBrush : MonoBehaviour
{
    //########## TIMING WINDOW VARIABLES ####################################
    private const float COMBO_BUFFER_TIME = 0.1f;
    private bool comboTriggered = false;
    private bool measured = false;

    //########## OBJECT REFERENCES ####################################
    [SerializeField]
    private GameObject hand;
    [SerializeField]
    private GameObject index_finger;

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

    public string dataFilePath;

    [SerializeField]
    bool hand_visible = true;
    [SerializeField]
    bool brush_visible = true;

    private VRControls _controls;

    [Space]
    [Tooltip("The shift of the virtual hand from the users actual hand.")]
    public Vector3 hand_offset;

    [Tooltip("The fixed rotation of the brush relative to the controller (in degrees).")]
    public Vector3 brush_rotation_offset;

    private Vector3 brush_offset;

    //########### SCANNING SETTINGS ###################################
    [Space]
    [Header("Scanning Automation")]
    [Tooltip("World space position where the scan starts.")]
    public Vector3 scanStart;

    [Tooltip("World space position where the scan ends.")]
    public Vector3 scanEnd;

    [Tooltip("The rotation of the brush during the automated sweep.")]
    public Vector3 sweepRotation;

    [Tooltip("How long (in seconds) the sweep takes to complete.")]
    public float sweepDuration = 5.0f;

    // State tracking for the sweep
    public bool IsSweeping { get; private set; } = false;
    private float _sweepStartTime;

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

    //Logs the position of the brush point to the system.
    public void PerformRecording()
    {
        if (_tracking_dot_brush == null)
        {
            Debug.LogWarning("Tracking dot is not found.");
            return;
        }

        float timestamp = Time.time;
        Vector3 position = _tracking_dot_brush.transform.position;
        string dataLine = "";
        if (IsSweeping)
        {
            dataLine = $"{timestamp},sweep,{position.x},{position.y},{position.z}\n";
        }
        else
        {
            dataLine = $"{timestamp},manual,{position.x},{position.y},{position.z}\n";
        }

        try
        {
            File.AppendAllText(dataFilePath, dataLine);
            measured = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }
        Debug.Log("Recording Performed");
    }

    public void NextMeasurement()
    {
        if (measured == true)
        {
            string dataLine = "-,-,-,-,-\n";
            File.AppendAllText(dataFilePath, dataLine);
            measured = false;
        }
    }

    public void StartScan()
    {
        if (hand == null || brush == null) return;

        IsSweeping = true;
        _sweepStartTime = Time.time;
        Debug.Log("Scan Started");
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
        if (isCalibrateHeld) FireCombo();
        else PerformRecording();
    }

    private void FireCombo()
    {
        if (comboTriggered) return;
        comboTriggered = true;
        CancelInvoke(nameof(ExecuteCalibrateAction));
        CancelInvoke(nameof(ExecuteRecordAction));
        PerformComboAction();
    }

    public void ButtonPressed()
    {
        if (_tracking_dot_finger == null)
        {
            Debug.LogWarning("Tracking dot for finger is not found.");
            return;
        }

        float timestamp = Time.time;
        Vector3 position = _tracking_dot_finger.transform.position;
        string dataLine = $"{timestamp},button,{position.x},{position.y},{position.z}\n";

        try
        {
            File.AppendAllText(dataFilePath, dataLine);
            measured = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }
        Debug.Log("Recording Performed");
    }

    //########### UNITY LIFECYCLE ###################################

    private void Awake()
    {
        _controls = new VRControls();

        if (!File.Exists(dataFilePath))
        {
            Debug.LogError("There is no file path for data collection");
        }
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

    private void Initialize()
    {

    }
    void Start()
    {
        Invoke(nameof(Initialize), 0.5f);
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

        // Brush Logic (Tracked vs Sweeping)
        if (IsSweeping)
        {
            float timeSinceStart = Time.time - _sweepStartTime;
            float percentageComplete = timeSinceStart / sweepDuration;

            if (percentageComplete >= 1.0f)
            {
                // Sweep complete, reset to tracking
                IsSweeping = false;
                Debug.Log("Scan Complete");
            }
            else
            {
                // Perform Sweep Interpolation
                brush.transform.position = Vector3.Lerp(scanStart, scanEnd, percentageComplete);
                brush.transform.rotation = Quaternion.Euler(sweepRotation);
            }
        }
        else
        {
            brush.transform.localPosition = brush_offset;
            brush.transform.localRotation = Quaternion.Euler(brush_rotation_offset);
        }

        //  Visibility
        hand.SetActive(hand_visible);
        brush.SetActive(brush_visible);
    }

    // --- VISUALIZATION: Gizmos for Scan Path & Offset ---
    private void OnDrawGizmos()
    {
        // Scan Path Gizmos
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(scanStart, 0.02f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(scanEnd, 0.02f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(scanStart, scanEnd);

    }
}