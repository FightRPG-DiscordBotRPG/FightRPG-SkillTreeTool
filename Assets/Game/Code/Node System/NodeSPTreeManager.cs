using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System;

namespace Assets.Game.Code
{
    public class NodeSPTreeManager : MonoBehaviour
    {
        public Dictionary<int, GameObject> Nodes = new Dictionary<int, GameObject>();
        public GameObject EmptyNodePrefab, NodesGroup, NodeJoinPrefab;
        public GameObject SelectedNode = null, SelectedLink = null;

        /**
         * UI Related
         */
        public GameObject TextCostObject = null;
        public GameObject LockObject = null;
        public GameObject EditNodeButton = null;
        private string CostBeforeChange = "";
        private int currentIdToGenerate = 1;

        // Use this for initialization
        void Start()
        {
            // Load From DB
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                TryToRemoveSelection();
            }
            else if (Input.GetKeyDown(KeyCode.L) && SelectedNode)
            {
                NodeSPTree nodeScript = SelectedNode.GetComponent<NodeSPTree>();
                nodeScript.IsLocked = !nodeScript.IsLocked;
                LockObject.GetComponent<Toggle>().isOn = nodeScript.IsLocked;
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                AddNewNode();
            } else if (Input.GetKeyDown(KeyCode.Escape) && SelectedNode)
            {
                SelectedNode.GetComponent<NodeSPTree>().UnSelect();
                SelectedNode = null;



                TMP_InputField fieldCost = TextCostObject.GetComponent<TMP_InputField>();
                Toggle fieldIsLocked = LockObject.GetComponent<Toggle>();
                Button editNodeButton = EditNodeButton.GetComponent<Button>();

                fieldCost.readOnly = false;;
                fieldIsLocked.interactable = false;
                editNodeButton.interactable = true;
            }



        }

        private void TryToRemoveSelection()
        {
            if (SelectedNode)
            {
                NodeSPTree node = SelectedNode.GetComponent<NodeSPTree>();
                node.Remove();
                Nodes.Remove(node.data.id);
                SelectedNode = null;
            }
            else if (SelectedLink)
            {
                NodeLink link = SelectedLink.GetComponent<NodeLink>();
                link.Remove();
                SelectedLink = null;
            }
        }

        public void AddNewNode()
        {
            GameObject Node = Instantiate(EmptyNodePrefab, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2)), Quaternion.identity, NodesGroup.transform);
            Node.transform.position = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);
            //Node.transform.localPosition = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);

            NodeSPTree script = Node.GetComponent<NodeSPTree>();
            AddNode( Node);
            script.OnSelectionEvent += Select;
            script.JoinPrefab = NodeJoinPrefab;
            UpdateIds();
            Select(Node);
        }

        public void LoadNodeFromData(NodeData nData)
        {
            GameObject Node = Instantiate(EmptyNodePrefab, new Vector3(nData.x, nData.y), Quaternion.identity, NodesGroup.transform);
            Node.transform.position = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);

            AddNode(Node);


            NodeSPTree script = Node.GetComponent<NodeSPTree>();
            script.OnSelectionEvent += Select;
            script.JoinPrefab = NodeJoinPrefab;
            script.data = nData;
        }

        private void AddNode(GameObject node)
        {
            Nodes[currentIdToGenerate] = node;
            currentIdToGenerate++;
        }

        public void ReloadAllNodes()
        {
            foreach(KeyValuePair<int, GameObject> kvpNode in Nodes)
            {
                NodeSPTree script = kvpNode.Value.GetComponent<NodeSPTree>();
                _ = script.UpdateImage();
                foreach(int id in script.data.linkedNodes)
                {
                    if (Nodes.ContainsKey(id))
                    {
                        script.AddJoin(Nodes[id].GetComponent<NodeSPTree>());
                    }

                }
            }
        }

        public void UpdateIds()
        {
            Dictionary<int, GameObject> updatedDicitonnary = new Dictionary<int, GameObject>();
            currentIdToGenerate = 1;
            foreach (KeyValuePair<int, GameObject> nodes in Nodes)
            {
                updatedDicitonnary[currentIdToGenerate] = nodes.Value;
                nodes.Value.GetComponent<NodeSPTree>().data.id = currentIdToGenerate;
                currentIdToGenerate++;
            }
        }

        private void Select(GameObject toSelectGameObject)
        {


            if (SelectedNode == toSelectGameObject || SelectedLink == toSelectGameObject)
            {
                return;
            }

            NodeSPTree scriptNode = toSelectGameObject.GetComponent<NodeSPTree>();
            NodeLink scriptLink = toSelectGameObject.GetComponent<NodeLink>();

            TMP_InputField fieldCost = TextCostObject.GetComponent<TMP_InputField>();
            Toggle fieldIsLocked = LockObject.GetComponent<Toggle>();
            Button editNodeButton = EditNodeButton.GetComponent<Button>();

            if (SelectedNode)
            {
                SelectedNode.GetComponent<NodeSPTree>().UnSelect();
            }

            SelectedNode = null;

            if (SelectedLink)
            {
                SelectedLink.GetComponent<NodeLink>().UnSelect();
            }

            SelectedLink = null;



            if (scriptNode)
            {
                SelectedNode = toSelectGameObject;
                fieldCost.readOnly = false;
                fieldCost.text = scriptNode.data.cost.ToString();

                fieldIsLocked.isOn = scriptNode.IsLocked;
                fieldIsLocked.interactable = true;

                editNodeButton.interactable = true;

                scriptNode.Select();
            }
            else if (scriptLink)
            {
                fieldCost.readOnly = true;
                fieldIsLocked.interactable = false;

                SelectedLink = toSelectGameObject;

                editNodeButton.interactable = false;

                scriptLink.Select();
            }


        }

        public void ChangeSelectedCost()
        {
            try
            {
                string text = TextCostObject.GetComponent<TMP_InputField>().text;
                SelectedNode.GetComponent<NodeSPTree>().data.cost = int.Parse(text);
                CostBeforeChange = text;
            }
            catch
            {
                TextCostObject.GetComponent<TMP_InputField>().text = CostBeforeChange;
            }
        }

        public void ChangeLockState()
        {
            if (SelectedNode)
            {
                SelectedNode.GetComponent<NodeSPTree>().IsLocked = LockObject.GetComponent<Toggle>().isOn;
            }
        }

    }
}