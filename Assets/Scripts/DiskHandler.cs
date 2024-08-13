using System.Collections;
using UnityEngine;

public class DiskHandler : MonoBehaviour
{
    public bool isSelected = false;
    public Color originalColor;
    public Color selectedColor = Color.white;
    public Color winColor = Color.yellow;
    private Renderer renderer;
    private GameObject[] sticks;
    private bool isGameWon = false;
    public int diskOrder;
    public float diskSize;

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        originalColor = renderer.material.color;
        sticks = GameObject.FindGameObjectsWithTag("Stick");

        diskOrder = transform.GetSiblingIndex();
    }

    private void OnMouseDown()
    {
        if (isGameWon)
        {
            ResetGame();
            return;
        }

        if (IsTopmostDisk())
        {
            isSelected = true;
            renderer.material.color = selectedColor;
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
                    GameObject stick = hit.collider.gameObject;
                    if (CanPlaceDiskOnStick(stick))
                    {
                        // snapping behavior
                        float snapDistance = 0.1f;
                        if (Vector3.Distance(transform.position, stick.transform.position) < snapDistance)
                        {
                            transform.position = stick.transform.position;
                        }

                        transform.parent = stick.transform;
                        transform.localPosition = new Vector3(0, transform.localPosition.y, 0);

                        CheckWinCondition();
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
            return true; // empty stick, can place disk
        }

        Transform topDisk = stick.transform.GetChild(stick.transform.childCount - 1);
        DiskHandler topDiskScript = topDisk.GetComponent<DiskHandler>();

        return diskSize < topDiskScript.diskSize;
    }

    private bool IsTopmostDisk()
    {
        if (transform.parent == null)
        {
            return true; // no parent -> it's the topmost disk
        }

        Debug.Log("Parent: " + transform.parent.name);
        Debug.Log("Child Count: " + transform.parent.childCount);

        // go through siblings, but not itself
        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform sibling = transform.parent.GetChild(i);
            if (sibling != this)
            {
                DiskHandler siblingScript = sibling.GetComponent<DiskHandler>();
                if (siblingScript && siblingScript.diskSize < diskSize)
                {
                    return false; // found smaller disk above -> not topmost
                }
            }
        }

        return true; // no smaller disks -> found topmost disk
    }




    private Coroutine winCoroutine;

    private void CheckWinCondition()
    {
        GameObject[] disks = GameObject.FindGameObjectsWithTag("Disk");

        if (sticks[1].transform.childCount == disks.Length)
        {
            isGameWon = true;
            winCoroutine = StartCoroutine(WinDelay());
        }
    }

    private IEnumerator WinDelay()
    {
        yield return new WaitForSeconds(0.1f);

        foreach (GameObject disk in GameObject.FindGameObjectsWithTag("Disk"))
        {
            disk.GetComponent<Renderer>().material.color = winColor;
        }
    }


    private void ResetGame()
    {
        isGameWon = false;

        GameObject[] disks = GameObject.FindGameObjectsWithTag("Disk");
        foreach (GameObject disk in disks)
        {
            DiskHandler diskScript = disk.GetComponent<DiskHandler>();
            disk.transform.parent = GameObject.Find("Left").transform;
            diskScript.transform.localPosition = new Vector3(-1f, diskScript.transform.localPosition.y, diskScript.transform.localPosition.z);
            diskScript.renderer.material.color = diskScript.originalColor;
            diskScript.isSelected = false;
        }
    }
}