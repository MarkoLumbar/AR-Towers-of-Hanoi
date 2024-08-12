using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskHandler : MonoBehaviour
{
    public float moveSpeed = 5f;

    private bool isSelected = false;

    private void OnMouseDown()
    {
        if (!isSelected)
        {
            isSelected = true;
        }
    }

    private void OnMouseUp()
    {
        if (isSelected)
        {
            isSelected = false;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Stick"))
                {
                    StartCoroutine(MoveToStick(hit.transform.position.x));
                }
            }
        }
    }

    private IEnumerator MoveToStick(float targetX)
    {
        while (transform.position.x != targetX)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position = new Vector3(Mathf.MoveTowards(transform.position.x, targetX, step), transform.position.y, transform.position.z);
            yield return null;
        }
    }
}