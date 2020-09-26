using Assets.Game.Code;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    Camera cam;
    public float zoomSpeed, orthographicSizeMin, orthographicSizeMax;


    private Vector3 ResetCamera; // original camera position
    private Vector3 Origin; // place where mouse is first pressed
    private Vector3 Diference; // change in position of mouse relative to origin

    public GameObject EditNodeUI = null;





    void Start()
    {
        cam = Camera.main;
        ResetCamera = cam.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(!EditNodeUI.activeInHierarchy)
        {
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                cam.orthographicSize += zoomSpeed;
            }
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                cam.orthographicSize -= zoomSpeed;
            }
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, orthographicSizeMin, orthographicSizeMax);
        }


    }

    void LateUpdate()
    {
        if(!EditNodeUI.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(2))
            {
                Origin = MousePos();
            }
            if (Input.GetMouseButton(2))
            {
                Diference = MousePos() - transform.position;
                transform.position = Origin - Diference;
            }
            if (Input.GetKeyDown(KeyCode.R)) // reset camera to original position
            {
                transform.position = ResetCamera;
            }
        }

    }
    // return the position of the mouse in world coordinates (helper method)
    Vector3 MousePos()
    {
        return cam.ScreenToWorldPoint(Input.mousePosition);
    }
}
