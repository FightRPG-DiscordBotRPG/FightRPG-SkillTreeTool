using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

namespace Assets.Game.Code
{
    public class NodeSPTreeManager : MonoBehaviour
    {
        public List<GameObject> Nodes = new List<GameObject>();
        public GameObject EmptyNodePrefab, NodesGroup, NodeJoinPrefab;
        public GameObject SelectedNode = null;

        /**
         * UI Related
         */
        public GameObject TextCostObject = null;
        public GameObject LockObject = null;
        private string CostBeforeChange = "";

        // Use this for initialization
        void Start()
        {
            // Load From DB
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AddNewNode()
        {
            GameObject Node = Instantiate(EmptyNodePrefab, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane)), Quaternion.identity, NodesGroup.transform);
            Nodes.Add(Node);

            NodeSPTree script = Node.GetComponent<NodeSPTree>();
            script.OnSelectionEvent += Select;
            script.JoinPrefab = NodeJoinPrefab;
            UpdateIds();
            Select(Node);
        }

        private void UpdateIds()
        {
            for(int i=0;i<Nodes.Count;i++)
            {
                Nodes[i].GetComponent<NodeSPTree>().id = (uint) i + 1;
            }
        }

        private void Select(GameObject node)
        {
            SelectedNode = node;

            NodeSPTree scriptNode = node.GetComponent<NodeSPTree>();

            TMP_InputField fieldCost = TextCostObject.GetComponent<TMP_InputField>();
            fieldCost.readOnly = false;
            fieldCost.text = scriptNode.Cost.ToString();

            LockObject.GetComponent<Toggle>().isOn = scriptNode.IsLocked;
        }

        public void ChangeSelectedCost()
        {
            try
            {
                string text = TextCostObject.GetComponent<TMP_InputField>().text;
                SelectedNode.GetComponent<NodeSPTree>().Cost = (uint) int.Parse(text);
                CostBeforeChange = text;
            }
            catch
            {
                TextCostObject.GetComponent<TMP_InputField>().text = CostBeforeChange;
            }
        }

        public void ChangeLockState()
        {
            if(SelectedNode)
            {
                SelectedNode.GetComponent<NodeSPTree>().IsLocked = LockObject.GetComponent<Toggle>().isOn;
            }
        }
        
    }
}