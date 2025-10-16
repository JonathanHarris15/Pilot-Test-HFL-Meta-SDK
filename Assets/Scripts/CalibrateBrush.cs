using System.Linq;
using System;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.InputSystem;

public class CalibrateBrush : MonoBehaviour
{

    [SerializeField]
    private GameObject LeftHandAnchor;

    [SerializeField]
    private GameObject BrushOffset;

    [SerializeField]
    private GameObject _tracking_dot_brush;
    [SerializeField]
    private GameObject _tracking_dot_finger;

    [SerializeField]
    private OVRSkeleton _skeleton;

    private OVRBone _indexTipBone;

    private VRControls _controls;

    public Vector3 additional_offset;

    [SerializeField]
    private int bone_id;

    private void Awake()
    {
        _controls = new VRControls();
    }
    private void OnEnable()
    {
        _controls.VRController.Calibrate.performed += OnCalibratePerformed;
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

        additional_offset -= _tracking_dot_brush.transform.position - _tracking_dot_finger.transform.position;

    }



    void Start()
    {
        Invoke(nameof(Initialize), 0.5f);
    }
    private void Initialize()
    { 
        _indexTipBone = _skeleton.Bones.FirstOrDefault(b => b.Id == (OVRSkeleton.BoneId)bone_id);
    }
  

    void Update()
    {
        BrushOffset.transform.position = LeftHandAnchor.transform.position + additional_offset;
        _tracking_dot_finger.transform.position = _indexTipBone.Transform.position;
    }

}
