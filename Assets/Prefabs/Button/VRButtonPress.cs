using System;
using UnityEngine;

public class VRButtonPress : MonoBehaviour
{


    [SerializeField]
    private GameObject buttonFace;

    [SerializeField]
    private AudioSource a_source;


    [SerializeField]
    private AudioClip button_in; 

    [SerializeField]
    private AudioClip button_out;


    private bool pressed = false; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        //*start event*

        if (!pressed){
            buttonFace.transform.localPosition += new Vector3(0, -0.01f, 0);
            pressed = true;
            a_source.clip = button_in;
            a_source.Play();
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (pressed)
        {
            pressed = false;
            buttonFace.transform.localPosition += new Vector3(0, 0.01f, 0);
            a_source.clip = button_out;
            a_source.Play();
        }
    }
}
