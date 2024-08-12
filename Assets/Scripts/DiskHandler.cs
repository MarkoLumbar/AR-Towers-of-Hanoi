using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskHandler : MonoBehaviour
{
    public bool isSelected = false;
    public Color originalColor;
    public Color selectedColor = Color.white;
    private Renderer renderer;

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        originalColor = renderer.material.color;
    }

    private void OnMouseDown()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            if (hit.collider.gameObject.CompareTag("Disk"))
            {
                // Check for the topmost disk
                GameObject topDisk = hit.collider.gameObject;
                while (topDisk.transform.parent != null && topDisk.transform.parent.gameObject.CompareTag("Disk"))
                {
                    topDisk = topDisk.transform.parent.gameObject;
                }

                // Select the topmost disk
                isSelected = true;
                renderer.material.color = selectedColor;
            }
        }
    }

    private void OnMouseUp()
    {
        if (isSelected)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                if (hit.collider.gameObject.CompareTag("Stick"))
                {
                    if (CanPlaceDiskOnStick(hit.collider.gameObject))
                    {
                        GameObject stick = hit.collider.gameObject;
                        transform.position = new Vector3(stick.transform.position.x, transform.position.y, stick.transform.position.z);
                        transform.parent = stick.transform;
                        transform.localPosition = new Vector3(0, transform.localPosition.y, 0);
                    }
                }
            }
            isSelected = false;
            renderer.material.color = originalColor;
        }
    }


    private bool CanPlaceDiskOnStick(GameObject stick)
    {
        if (stick.transform.childCount == 0)
        {
            return true; // Empty stick, can place disk
        }

        Transform topDisk = stick.transform.GetChild(0);
        return transform.position.y < topDisk.position.y;
    }
}