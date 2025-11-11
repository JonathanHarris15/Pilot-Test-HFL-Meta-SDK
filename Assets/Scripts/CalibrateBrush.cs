using System.Linq;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor.ShaderGraph;
using System.IO;

public class CalibrateBrush : MonoBehaviour
{

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

    private int bone_id = 10;

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

        Debug.Log("Calibrate button pressed!");
        UnityEditor.Undo.RecordObject(this, "Calibrate Offset");
        // 1. Get the world-space offset from the brush's origin to its tracking dot.
        Vector3 dotOffsetWorld = _tracking_dot_brush.transform.position - brush.transform.position;

        // 2. Determine the target world position for the brush's origin.
        // (This is the finger's position MINUS that offset).
        Vector3 targetBrushWorldPos = _tracking_dot_finger.transform.position - dotOffsetWorld;

        // 3. Convert this target world position into the parent's local space and store it.
        // We use InverseTransformPoint because we are converting a "point" in world space.
        brush_offset = brush.transform.parent.InverseTransformPoint(targetBrushWorldPos);
    }

    public void PerformRecording()
    {
        if (_tracking_dot_brush == null)
        {
            Debug.LogWarning("Tracking dot is not found.");
            return;
        }

        float timestamp = Time.time;
        Vector3 position = _tracking_dot_brush.transform.position;

        // 2. Format the data as a CSV line
        string dataLine = $"{timestamp},{position.x},{position.y},{position.z}\n";

        // 3. Append this line to the file
        try
        {
            File.AppendAllText(dataFilePath, dataLine);
            // Optional: Log to confirm, might be spammy
            // Debug.Log($"Saved data: {dataLine}");
        }
        catch (Exception e)
        {
            // Log an error if saving fails (e.g., disk full)
            Debug.LogError($"Failed to save data: {e.Message}");
        }

    }

    private void Awake()
    {
        _controls = new VRControls();

        string fileName = "brush_stroke_data.csv";
        dataFilePath = Path.Combine("C:\\Users\\jonathan.h.1505\\Documents\\Pilot_Data_Collection", fileName);//TODO: THIS SHOULD BE CHANGED

        // Write a header row if the file doesn't exist yet
        if (!File.Exists(dataFilePath))
        {
            string header = "Timestamp,PositionX,PositionY,PositionZ\n";
            File.WriteAllText(dataFilePath, header);
        }

        Debug.Log($"Data will be saved to: {dataFilePath}");
    }
    private void OnEnable()
    {
        _controls.VRController.Calibrate.performed += OnCalibratePerformed;
        _controls.VRController.Record.performed += OnRecordPerformed;
        _controls.VRController.Enable();
    }

    private void OnDisable()
    {
        _controls.VRController.Disable();
        _controls.VRController.Calibrate.performed -= OnCalibratePerformed;
    }

    private void OnCalibratePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Calibrate button pressed!");

        PerformCalibration();

    }

    private void OnRecordPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Record button pressed!");

        PerformRecording();
    }


//########### START & UPDATE ###################################
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
        if (hand == null || brush == null || _tracking_dot_finger == null || _indexTipBone == null || hand == null)
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