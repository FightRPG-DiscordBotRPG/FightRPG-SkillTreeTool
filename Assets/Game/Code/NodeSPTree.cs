using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeSPTree : MonoBehaviour
{
    public string Image = "";
    public double X = 0d;
    public double Y = 0d;
    public uint Cost = 1;
    public List<NodeSPTree> Parents = new List<NodeSPTree>(), Children = new List<NodeSPTree>();
    public List<GameObject> AllJoinsParents = new List<GameObject>();
    public List<GameObject> AllJoinsChildren = new List<GameObject>();
    public bool IsLocked = false;
    public uint id = 0;
    public GameObject JoinPrefab;

    private float distance;
    private bool dragging, linking;
    private GameObject spawnedJoin;

    public delegate void OnSelectionEventHandler(GameObject sender);
    public event OnSelectionEventHandler OnSelectionEvent;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnMouseDown()
    {
        
        OnSelectionEvent?.Invoke(gameObject);

        if(!IsLocked)
        {
            distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            dragging = true;
        } else
        {
            // 
            linking = true;
            spawnedJoin = Instantiate(JoinPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    void OnMouseUp()
    {
        dragging = false;
        if(spawnedJoin)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit.collider != null)
            {
                NodeSPTree toLink = hit.collider.gameObject.GetComponent<NodeSPTree>();
                if(toLink && toLink.TryAddParent(this))
                {
                    Children.Add(toLink);
                    spawnedJoin.GetComponent<LineRenderer>().SetPositions(new[]
                    { 
                        new Vector3(transform.position.x, transform.position.y, 0f),
                        new Vector3(hit.collider.gameObject.transform.position.x, hit.collider.gameObject.transform.position.y)
                    });
                    toLink.AllJoinsParents.Add(spawnedJoin);
                    AllJoinsChildren.Add(spawnedJoin);

                    // All link to me and the other
                    NodeLink nodeLinkScript = spawnedJoin.GetComponent<NodeLink>();

                    nodeLinkScript.AllAttachedNodes.Add(this);
                    nodeLinkScript.AllAttachedNodes.Add(toLink);

                } else
                {
                    Destroy(spawnedJoin);
                }
            } else
            {
                Destroy(spawnedJoin);
            }
            spawnedJoin = null;
            linking = false;

        }

        AllJoinsChildren.ForEach((GameObject o) => o.GetComponent<NodeLink>().UpdateMesh());
        AllJoinsParents.ForEach((GameObject o) => o.GetComponent<NodeLink>().UpdateMesh());
    }

    bool TryAddParent(NodeSPTree newParent)
    {
        if (!Parents.Contains(newParent))
        {
            Parents.Add(newParent);
            return true;
        }

        return false;
    }

    void Update()
    {
        if (dragging && !IsLocked)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 rayPoint = ray.GetPoint(distance);
            transform.position = rayPoint;

            // Parent links
            foreach(GameObject join in AllJoinsParents)
            {
                LineRenderer lr = join.GetComponent<LineRenderer>();
                lr.SetPositions(new[]
                    {
                        lr.GetPosition(0),
                        new Vector3(transform.position.x, transform.position.y)
                    });
            }

            // Children links
            foreach (GameObject join in AllJoinsChildren)
            {
                LineRenderer lr = join.GetComponent<LineRenderer>();
                lr.SetPositions(new[]
                    {
                        new Vector3(transform.position.x, transform.position.y),
                        lr.GetPosition(1),
                    });
            }
        }

        if (linking)
        {
            Camera c = Camera.main;
            spawnedJoin.GetComponent<LineRenderer>().SetPositions(new []{ new Vector3(transform.position.x, transform.position.y, 0f), c.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0)) });
        }
    }

}
