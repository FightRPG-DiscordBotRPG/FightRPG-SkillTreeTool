using Assets.Game.Code;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeSPTree : MonoBehaviour
{

    public List<NodeSPTree> Parents = new List<NodeSPTree>();
    public List<NodeSPTree> Children = new List<NodeSPTree>();
    public List<GameObject> AllJoinsParents = new List<GameObject>();
    public List<GameObject> AllJoinsChildren = new List<GameObject>();
    public bool IsLocked = false;
    public GameObject JoinPrefab;

    private float distanceFromCamera;
    private Vector3 dragOffset;
    private bool dragging, linking;
    private GameObject spawnedJoin;

    public delegate void OnSelectionEventHandler(GameObject sender);
    public event OnSelectionEventHandler OnSelectionEvent;

    public Material NonSelectedMaterial, SelectedMaterial;

    public NodeData data = new NodeData();

    private SpriteRenderer sprite;
    public NodeSPTreeManager Manager;


    // Start is called before the first frame update
    void Start()
    {
        sprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    void OnMouseDown()
    {
        if(Manager.EditNodeUI.activeInHierarchy)
        {
            return;
        }

        OnSelectionEvent?.Invoke(gameObject);


        if (!IsLocked)
        {
            distanceFromCamera = Vector3.Distance(transform.position, Camera.main.transform.position);

            Vector3 ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dragOffset = transform.position - ray;

            dragging = true;
        }
        else
        {
            // 
            linking = true;
            spawnedJoin = Instantiate(JoinPrefab, new Vector3(transform.position.x, transform.position.y, 1f), Quaternion.identity, transform);
        }
    }

    void OnMouseUp()
    {
        if (Manager.EditNodeUI.activeInHierarchy)
        {
            return;
        }

        dragging = false;
        if (spawnedJoin)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, 100);
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                NodeSPTree toLink = hit.collider.gameObject.GetComponent<NodeSPTree>();
                AddJoin(toLink);
            }
            else
            {
                Destroy(spawnedJoin);
            }

            linking = false;

        }

        UpdateAllJoinsMeshes();
    }

    void Update()
    {
        if (dragging && !IsLocked)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 rayPoint = ray.GetPoint(distanceFromCamera);
            transform.position = new Vector3(rayPoint.x, rayPoint.y, transform.position.z) + new Vector3(dragOffset.x, dragOffset.y);

            UpdateLinksPositions();
        }

        if (linking)
        {
            Camera c = Camera.main;
            Vector3 pointInWorld = c.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y));
            pointInWorld.z = 1f;
            spawnedJoin.GetComponent<LineRenderer>().SetPositions(new[]{
                new Vector3(transform.position.x, transform.position.y, 1f),
                pointInWorld
            });
        }

        UpdateDataPosition();

    }

    void UpdateAllJoinsMeshes()
    {
        AllJoinsChildren.ForEach((GameObject o) => o.GetComponent<NodeLink>().UpdateMesh());
        AllJoinsParents.ForEach((GameObject o) => o.GetComponent<NodeLink>().UpdateMesh());
    }

    public void UpdateLinksData()
    {
        data.linkedNodes.Clear();
        foreach(NodeSPTree node in Children)
        {
            data.linkedNodes.Add(node.data.id);
        }
    }

    public void AddJoin(NodeSPTree otherNode)
    {
        if(!spawnedJoin)
        {
            spawnedJoin = Instantiate(JoinPrefab, new Vector3(transform.position.x, transform.position.y, 1f), Quaternion.identity, transform);
        }


        if (otherNode && otherNode.TryAddParent(this))
        {
            Children.Add(otherNode);
            spawnedJoin.GetComponent<LineRenderer>().SetPositions(new[]
            {
                        new Vector3(transform.position.x, transform.position.y, 1f),
                        new Vector3(otherNode.gameObject.transform.position.x, otherNode.gameObject.transform.position.y, 1f)
                    });
            otherNode.AllJoinsParents.Add(spawnedJoin);
            AllJoinsChildren.Add(spawnedJoin);


            // All link to me and the other
            NodeLink nodeLinkScript = spawnedJoin.GetComponent<NodeLink>();
            nodeLinkScript.OnSelectionEvent += ReactNodeLinkSelected;
            nodeLinkScript.FirstItem = this;
            nodeLinkScript.SecondItem = otherNode;
            nodeLinkScript.Manager = Manager;

        }
        else
        {
            Destroy(spawnedJoin);
        }

        spawnedJoin = null;
    }

    internal void Remove()
    {
        for (int i = AllJoinsChildren.Count - 1; i >= 0; i--)
        {
            AllJoinsChildren[i].GetComponent<NodeLink>().Remove();
        }

        for (int i = AllJoinsParents.Count - 1; i >= 0; i--)
        {
            AllJoinsParents[i].GetComponent<NodeLink>().Remove();
        }

        Destroy(gameObject);
    }

    public async Task UpdateImage()
    {
        Texture2D tx = await PSTreeApiManager.Instance.GetTextureNode(data.visuals.id);
        // It's only called on start but this func may be called before
        if (!sprite)
        {
            sprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
        }

        sprite.sprite = PSTreeApiManager.Instance.GetSpriteForNode(tx);
    }

    bool TryAddParent(NodeSPTree newParent)
    {
        if (!Parents.Contains(newParent) && !Children.Contains(newParent) && this != newParent)
        {
            Parents.Add(newParent);
            return true;
        }

        return false;
    }

    private void UpdateDataPosition()
    {
        data.x = transform.position.x;
        data.y = transform.position.y;
    }

    public void UpdateLinksPositions()
    {
        // Parent links
        foreach (GameObject join in AllJoinsParents)
        {
            LineRenderer lr = join.GetComponent<LineRenderer>();
            lr.SetPositions(new[]
                {
                        lr.GetPosition(0),
                        new Vector3(transform.position.x, transform.position.y, 1f)
                    });
        }

        // Children links
        foreach (GameObject join in AllJoinsChildren)
        {
            LineRenderer lr = join.GetComponent<LineRenderer>();
            lr.SetPositions(new[]
                {
                        new Vector3(transform.position.x, transform.position.y, 1f),
                        lr.GetPosition(1),
                    });
        }
    }

    void ReactNodeLinkSelected(GameObject nodeLink)
    {
        OnSelectionEvent?.Invoke(nodeLink);
    }

    public void ClearLink(GameObject link, bool clearOther=true)
    {
        // To Optimize One Day
        NodeSPTree other = link.GetComponent<NodeLink>().GetOtherNode(this);

        if (AllJoinsChildren.Remove(link))
        {
            Children.Remove(other);
        }
        if (AllJoinsParents.Remove(link))
        {
            Parents.Remove(other);
        }

        if(clearOther)
        {
            // clear other side
            other.ClearLink(link, false);
        }

        //Debug.Log(id + " => " + AllJoinsChildren.Count + " - " + AllJoinsParents.Count);
    }

    public void Select()
    {
        GetComponent<SpriteRenderer>().material = SelectedMaterial;
    }

    public void UnSelect()
    {
        GetComponent<SpriteRenderer>().material = NonSelectedMaterial;
    }



}
