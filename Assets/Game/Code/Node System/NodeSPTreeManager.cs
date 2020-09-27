using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Assets.Game.Code
{
    public class NodeSPTreeManager : MonoBehaviour
    {
        [Header("Nodes Related")]
        public Dictionary<int, GameObject> Nodes = new Dictionary<int, GameObject>();
        public GameObject EmptyNodePrefab, NodesGroup, NodeJoinPrefab;

        [HideInInspector]
        public GameObject SelectedNode = null, SelectedLink = null;

        /**
         * UI Related
         */
        [Header("UI Nodes")]
        public GameObject TextCostObject = null;
        public GameObject LockObject = null;
        public GameObject AddButton = null;

        [Header("UI Edit Node")]
        public GameObject EditNodeUI;
        public GameObject EditNodeButton = null, CloseEditButton = null;
        public GameObject EditNodeUIAddSkillMenu = null;
        public TMP_InputField SearchAddSkill = null;
        public RecyclingListView SkillsRecyclingListView = null;
        public RecyclingListView SkillsToAddRecyclingListView = null;

        private readonly Dictionary<int, bool> SelectedSkills = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> SelectedSkillsToAdd = new Dictionary<int, bool>();

        private List<Skill> PossibleSkillsToAddFiltered = new List<Skill>();

        [Header("UI Edit Node - Visuals")]
        public GameObject EditVisualsUI;
        public TMP_InputField EditVisualsUISearch, EditVisualsUILinkImage;
        public RecyclingListView VisualsToAddRecyclingListView;

        private readonly Dictionary<int, bool> SelectedVisualsToAdd = new Dictionary<int, bool>();
        private List<NodeVisuals> PossibleNodeVisualsToAddFiltered = new List<NodeVisuals>();



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
            if (!EditNodeUI.activeInHierarchy)
            {
                DoShortcutsOnNodes();
            }

        }

        public void Clear()
        {
            foreach (KeyValuePair<int, GameObject> kvp in Nodes.ToArray())
            {
                RemoveNode(kvp.Value.GetComponent<NodeSPTree>());
            }

            SelectedNode = null;
            SelectedLink = null;
            UpdateIds();
        }

        private void DoShortcutsOnNodes()
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
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && SelectedNode)
            {
                SelectedNode.GetComponent<NodeSPTree>().UnSelect();
                SelectedNode = null;



                TMP_InputField fieldCost = TextCostObject.GetComponent<TMP_InputField>();
                Toggle fieldIsLocked = LockObject.GetComponent<Toggle>();
                Button editNodeButton = EditNodeButton.GetComponent<Button>();

                fieldCost.readOnly = false;
                fieldIsLocked.interactable = false;
                editNodeButton.interactable = true;
            }

        }

        private void TryToRemoveSelection()
        {
            if (SelectedNode)
            {
                RemoveNode(GetSelectedNodeScript());
                SelectedNode = null;
            }
            else if (SelectedLink)
            {
                NodeLink link = SelectedLink.GetComponent<NodeLink>();
                link.Remove();
                SelectedLink = null;
            }
        }

        void RemoveNode(NodeSPTree node)
        {
            node.Remove();
            Nodes.Remove(node.data.id);
        }

        public void AddNewNode()
        {
            GameObject Node = Instantiate(EmptyNodePrefab, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2)), Quaternion.identity, NodesGroup.transform);
            Node.transform.position = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);
            //Node.transform.localPosition = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);

            NodeSPTree script = Node.GetComponent<NodeSPTree>();
            AddNode(Node);
            script.OnSelectionEvent += Select;
            script.JoinPrefab = NodeJoinPrefab;
            UpdateIds();
            Select(Node);
            script.Manager = this;
        }

        public void EditNode()
        {
            if (SelectedNode == null)
            {
                return;
            }

            EditNodeUI.SetActive(true);
            EditNodeButton.SetActive(false);
            AddButton.SetActive(false);
            EditNodeUIAddSkillMenu.SetActive(false);
            UpdateEditNodeUI();
        }

        public void CloseEditNode()
        {
            EditNodeUI.SetActive(false);
            EditNodeButton.SetActive(true);
            AddButton.SetActive(true);
            EditNodeUIAddSkillMenu.SetActive(false);
        }

        private void UpdateEditNodeUI()
        {
            if (SelectedNode == null)
            {
                return;
            }

            NodeSPTree script = SelectedNode.GetComponent<NodeSPTree>();
            PossibleSkillsToAddFiltered = PSTreeApiManager.Instance.PossibleSkillsAsList;
            PossibleNodeVisualsToAddFiltered = PSTreeApiManager.Instance.PossibleNodesVisualsAsList;

            EditVisualsUILinkImage.text = "";

            SearchAddSkill.text = "";
            EditVisualsUISearch.text = "";

            UpdateEditNodeStatsUI(script);
            UpdateEditNodeSecondaryStatsUI(script);
            UpdateEditNodeSkillsUI(script);
        }

        private void UpdateEditNodeSkillsUI(NodeSPTree script)
        {

            SelectedSkills.Clear();
            SkillsRecyclingListView.Clear();
            SkillsRecyclingListView.ItemCallback = PopulateSkills;
            SkillsRecyclingListView.RowCount = script.data.skillsUnlocked.Count;
        }

        public void UpdateSearchSkillsToAdd()
        {
            PossibleSkillsToAddFiltered = PSTreeApiManager.Instance.PossibleSkillsAsList.Where(x => x.name.ToLower().Contains(SearchAddSkill.text.ToLower())).ToList();
            UpdateSkillsToAdd();
        }

        public void UpdateSearchVisualsToAdd()
        {
            PossibleNodeVisualsToAddFiltered = PSTreeApiManager.Instance.PossibleNodesVisualsAsList.Where(x => x.name.ToLower().Contains(EditVisualsUISearch.text.ToLower())).ToList();
            UpdateVisualsToAdd();
        }

        public void UpdateSkillsToAdd()
        {
            SelectedSkillsToAdd.Clear();
            SkillsToAddRecyclingListView.Clear();
            SkillsToAddRecyclingListView.ItemCallback = PopulateSkillsToAdd;
            SkillsToAddRecyclingListView.RowCount = PossibleSkillsToAddFiltered.Count;
        }

        public void UpdateVisualsToAdd()
        {
            SelectedVisualsToAdd.Clear();
            VisualsToAddRecyclingListView.Clear();
            VisualsToAddRecyclingListView.ItemCallback = PopulateVisualsToAdd;
            VisualsToAddRecyclingListView.RowCount = PossibleNodeVisualsToAddFiltered.Count;
        }

        private void PopulateSkills(RecyclingListViewItem item, int rowIndex)
        {
            NodeData data = SelectedNode.GetComponent<NodeSPTree>().data;
            var child = item as ListViewItemSimpleText;
            child.SetText(data.skillsUnlocked[rowIndex].name);
            child.id = data.skillsUnlocked[rowIndex].id;
            child.onSelectCallback = (int id) =>
            {
                SelectedSkills[id] = true;
            };

            child.onUnSelectCallback = (int id) =>
            {
                SelectedSkills[id] = false;
            };

            if (SelectedSkills.ContainsKey(child.id) && SelectedSkills[child.id] == true)
            {
                child.Select();
            }
            else
            {
                child.UnSelect();
            }
        }

        private void PopulateSkillsToAdd(RecyclingListViewItem item, int rowIndex)
        {
            var child = item as ListViewItemSimpleText;

            child.SetText(PossibleSkillsToAddFiltered[rowIndex].name);
            child.id = PossibleSkillsToAddFiltered[rowIndex].id;

            child.onSelectCallback = (int id) =>
            {
                SelectedSkillsToAdd[id] = true;
            };

            child.onUnSelectCallback = (int id) =>
            {
                SelectedSkillsToAdd[id] = false;
            };

            if (SelectedSkillsToAdd.ContainsKey(child.id) && SelectedSkillsToAdd[child.id] == true)
            {
                child.Select();
            }
            else
            {
                child.UnSelect();
            }
        }

        private void PopulateVisualsToAdd(RecyclingListViewItem item, int rowIndex)
        {
            var child = item as ListViewItemSimpleTextImage;

            child.SetText(PossibleNodeVisualsToAddFiltered[rowIndex].name == "" ? "Unknwon" : PossibleNodeVisualsToAddFiltered[rowIndex].name);
            child.SetImage(PossibleNodeVisualsToAddFiltered[rowIndex].icon);
            child.id = PossibleNodeVisualsToAddFiltered[rowIndex].id;

            child.onSelectCallback = (int id) =>
            {
                // We only want one to be selected
                SelectedVisualsToAdd.Clear();
                for (int i = 0; i < VisualsToAddRecyclingListView.Items.Length; i++)
                {
                    var otherItem = VisualsToAddRecyclingListView.Items[i] as ListViewItemSimpleTextImage;
                    if(otherItem != child)
                    {
                        otherItem.UnSelect();
                    }
                }
                SelectedVisualsToAdd[id] = true;
            };

            child.onUnSelectCallback = (int id) =>
            {
                SelectedVisualsToAdd[id] = false;
            };

            if (SelectedVisualsToAdd.ContainsKey(child.id) && SelectedVisualsToAdd[child.id] == true)
            {
                child.Select();
            }
            else
            {
                child.UnSelect();
            }
        }
        private void UpdateEditNodeStatsUI(NodeSPTree node)
        {
            GameObject statsPanel = EditNodeUI.transform.GetChild(0).GetChild(1).gameObject;
            for (int i = 0; i < statsPanel.transform.childCount; i++)
            {
                var child = statsPanel.transform.GetChild(i);
                child.GetChild(0).GetComponent<TMP_InputField>().text = node.data.stats[child.name].ToString();
            }
        }

        private void UpdateEditNodeSecondaryStatsUI(NodeSPTree node)
        {
            GameObject secondaryStatsPanel = EditNodeUI.transform.GetChild(1).GetChild(1).gameObject;
            GameObject resistsPanel = EditNodeUI.transform.GetChild(1).GetChild(3).gameObject;
            for (int i = 0; i < secondaryStatsPanel.transform.childCount; i++)
            {
                var child = secondaryStatsPanel.transform.GetChild(i);
                child.GetChild(0).GetComponent<TMP_InputField>().text = node.data.secondaryStats[child.name].ToString();
            }

            for (int i = 0; i < resistsPanel.transform.childCount; i++)
            {
                var child = resistsPanel.transform.GetChild(i);
                child.GetChild(0).GetComponent<TMP_InputField>().text = node.data.secondaryStats[child.name].ToString();
            }
        }

        public void UpdateStatValue(TMP_InputField sender)
        {
            if (SelectedNode != null)
            {
                NodeSPTree node = SelectedNode.GetComponent<NodeSPTree>();
                node.data.stats[sender.transform.parent.name] = int.Parse(sender.text);
            }
        }
        public void UpdateSecondaryStatValue(TMP_InputField sender)
        {
            if (SelectedNode != null)
            {
                NodeSPTree node = SelectedNode.GetComponent<NodeSPTree>();
                node.data.secondaryStats[sender.transform.parent.name] = int.Parse(sender.text);
            }
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
            script.IsLocked = true;
            script.Manager = this;
        }

        private void AddNode(GameObject node)
        {
            Nodes[currentIdToGenerate] = node;
            currentIdToGenerate++;
        }

        public void ReloadAllNodes()
        {
            foreach (KeyValuePair<int, GameObject> kvpNode in Nodes)
            {
                NodeSPTree script = kvpNode.Value.GetComponent<NodeSPTree>();
                _ = script.UpdateImage();
                foreach (int id in script.data.linkedNodes)
                {
                    if (Nodes.ContainsKey(id))
                    {
                        script.AddJoin(Nodes[id].GetComponent<NodeSPTree>());
                    }

                }
            }
        }

        public void OpenAddSkillAddingMenu()
        {
            EditNodeUIAddSkillMenu.SetActive(true);
            UpdateSkillsToAdd();
        }

        public void CloseAddSkillAddingMenu()
        {
            EditNodeUIAddSkillMenu.SetActive(false);
        }

        public void OpenVisualsAddingMenu()
        {
            EditVisualsUI.SetActive(true);
            UpdateVisualsToAdd();
        }

        public void CloseVisualsAddingMenu()
        {
            EditVisualsUI.SetActive(false);
        }

        public void AddSelectedSkills()
        {
            NodeSPTree node = GetSelectedNodeScript();
            foreach (KeyValuePair<int, bool> kvp in SelectedSkillsToAdd)
            {
                Skill skillToAdd = PSTreeApiManager.Instance.PossibleSkills[kvp.Key];
                if (kvp.Value && !node.data.skillsUnlocked.Contains(skillToAdd))
                {
                    node.data.skillsUnlocked.Add(skillToAdd);
                }
            }

            UpdateEditNodeSkillsUI(node);
        }

        public void RemoveSelectedSkills()
        {
            NodeSPTree node = GetSelectedNodeScript();
            foreach (KeyValuePair<int, bool> kvp in SelectedSkills)
            {
                if (kvp.Value)
                {
                    node.data.skillsUnlocked.Remove(PSTreeApiManager.Instance.PossibleSkills[kvp.Key]);
                }
            }

            UpdateEditNodeSkillsUI(node);
        }

        public void SetSelectedVisual()
        {
            NodeSPTree node = GetSelectedNodeScript();
            foreach (KeyValuePair<int, bool> kvp in SelectedVisualsToAdd)
            {
                NodeVisuals visual = PSTreeApiManager.Instance.PossibleNodesVisuals[kvp.Key];
                node.data.visuals = visual;
                break;
            }

            UpdateVisualsToAdd();
            ReloadAllNodes();
        }

        private NodeSPTree GetSelectedNodeScript()
        {
            return SelectedNode?.GetComponent<NodeSPTree>();
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
            string text = TextCostObject.GetComponent<TMP_InputField>().text;
            SelectedNode.GetComponent<NodeSPTree>().data.cost = int.Parse(text);
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