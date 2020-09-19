using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeLink : MonoBehaviour
{
    public NodeSPTree FirstItem, SecondItem;
    public delegate void OnSelectionEventHandler(GameObject sender);
    public event OnSelectionEventHandler OnSelectionEvent;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateMesh()
    {
        try
        {
            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            MeshCollider meshCollider = GetComponent<MeshCollider>();

            Mesh mesh = new Mesh();
            lineRenderer.BakeMesh(mesh, true);
            meshCollider.sharedMesh = mesh;

            Vector3 pos = lineRenderer.GetPosition(0);
            transform.localPosition = new Vector3(-pos.x, -pos.y);

        } catch
        {
            Debug.LogError("Balec Sérieux");
        }

    }

    void OnMouseDown()
    {
        Debug.Log("On Mouse Down Node Link");

        RaycastHit hit;
        Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100);
        if(hit.collider && hit.collider.gameObject == gameObject)
        {
            OnSelectionEvent?.Invoke(gameObject);
        }

    }

    public void Select()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
    }

    public void UnSelect()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.startColor = Color.white;
        lr.endColor = Color.white;
    }

    public void Remove()
    {
        FirstItem?.ClearLink(gameObject);
        SecondItem?.ClearLink(gameObject);
        Destroy(gameObject);
    }

    public NodeSPTree GetOtherNode(NodeSPTree node)
    {
        if(FirstItem == node)
        {
            return SecondItem;
        } else if(SecondItem == node)
        {
            return FirstItem;
        }else
        {
            return null;
        }

    }

}
