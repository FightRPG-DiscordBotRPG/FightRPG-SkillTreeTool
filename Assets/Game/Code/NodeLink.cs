using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeLink : MonoBehaviour
{
    public List<NodeSPTree> AllAttachedNodes = new List<NodeSPTree>();
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
        GetComponent<LineRenderer>().startColor = Color.yellow;
        GetComponent<LineRenderer>().endColor = Color.yellow;
    }

}
