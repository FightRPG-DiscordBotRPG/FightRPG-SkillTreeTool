﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;

namespace Assets.Game.Code
{
    public class NodeSPTreeManager : MonoBehaviour
    {
        [Header("Nodes Related")]
        public Dictionary<int, GameObject> Nodes = new Dictionary<int, GameObject>();
        public GameObject EmptyNodePrefab, NodesGroup, NodeJoinPrefab;

        [HideInInspector]
        public List<GameObject> SelectedNodesList = new List<GameObject>(), SelectedLinksList = new List<GameObject>();

        /**
         * UI Related
         */
        [Header("UI Nodes")]
        public Toggle LockToggle, IsInitialToggle;
        public GameObject AddButton = null;
        public TMP_InputField xPosUI, yPosUI, CostUI, IdUI;

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
        public TMP_InputField EditVisualsUISearch, EditVisualsUILinkImage, UINewVisualsName, UINewVisualsUrl;
        public TMP_Text SelectedVisualUIName;
        public RecyclingListView VisualsToAddRecyclingListView;
        public Button EditVisualsUIRemoveSelectedButton;

        private readonly Dictionary<int, bool> SelectedVisualsToAdd = new Dictionary<int, bool>();
        private List<NodeVisuals> PossibleNodeVisualsToAddFiltered = new List<NodeVisuals>();

        private int currentIdToGenerate = 1;

        public bool IsGridActive { get; private set; } = false;

        private List<NodeSPTree> clipBoardNodeCopyList = new List<NodeSPTree>();

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

            UpdateSelectedNodePositionInUI();

        }

        private GameObject GetSelectedNode()
        {
            return SelectedNodesList.FirstOrDefault();
        }

        private GameObject GetSelectedLink()
        {
            return SelectedLinksList.FirstOrDefault();
        }

        private void UpdateSelectedNodePositionInUI()
        {
            if (GetSelectedNode() && !IsUiFocused())
            {
                xPosUI.text = GetSelectedNode().transform.position.x.ToString();
                yPosUI.text = GetSelectedNode().transform.position.y.ToString();
            }
        }

        public void UpdateFromUIIdentifier()
        {
            if (GetSelectedNode() && int.TryParse(IdUI.text, out int id))
            {

                NodeSPTree selectedNodeScript = GetSelectedNodeScript();
                if (Nodes.ContainsKey(id))
                {
                    if (Nodes[id] != GetSelectedNode())
                    {
                        GameObject toSwapNode = Nodes[id];
                        NodeSPTree toSwapScript = toSwapNode.GetComponent<NodeSPTree>();

                        Nodes[selectedNodeScript.data.id] = toSwapNode;
                        toSwapScript.data.id = selectedNodeScript.data.id;

                        Nodes[id] = GetSelectedNode();
                        selectedNodeScript.data.id = id;
                    }


                }
                else
                {
                    // Move from index x to index y and change id
                    Nodes.Remove(selectedNodeScript.data.id);
                    selectedNodeScript.data.id = id;
                    Nodes.Add(id, GetSelectedNode());
                }
            }
        }

        public void UpdateFromUIPositionX()
        {
            if (GetSelectedNode() && float.TryParse(xPosUI.text, out float x))
            {

                GetSelectedNode().transform.position = new Vector3(x, GetSelectedNode().transform.position.y, GetSelectedNode().transform.position.z);
                GetSelectedNodeScript().UpdateLinksPositions();
            }
        }

        public void UpdateFromUIPositionY()
        {
            if (GetSelectedNode() && float.TryParse(yPosUI.text, out float y))
            {
                GetSelectedNode().transform.position = new Vector3(GetSelectedNode().transform.position.x, y, GetSelectedNode().transform.position.z);
                GetSelectedNodeScript().UpdateLinksPositions();
            }
        }

        public void Clear()
        {
            foreach (KeyValuePair<int, GameObject> kvp in Nodes.ToArray())
            {
                RemoveNode(kvp.Value.GetComponent<NodeSPTree>());
            }


            SelectedNodesList.Clear();
            SelectedLinksList.Clear();
            UpdateIds();
        }

        private void DoShortcutsOnNodes()
        {
            if (Input.GetKeyDown(KeyCode.Delete) && !IsUiFocused())
            {
                TryToRemoveSelection();
            }
            else if (Input.GetKeyDown(KeyCode.L) && SelectedNodesList.Count > 0)
            {
                bool lockValue = !GetSelectedNodeScript().IsLocked;
                foreach (GameObject node in SelectedNodesList)
                {
                    NodeSPTree nodeScript = node.GetComponent<NodeSPTree>();
                    nodeScript.IsLocked = lockValue;
                    LockToggle.isOn = nodeScript.IsLocked;
                }
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                AddNewNode();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && SelectedNodesList.Count > 0)
            {
                UnSelectAllNodes();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && SelectedLinksList.Count > 0)
            {
                UnSelectAllLinks();
            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                IsGridActive ^= true;
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                EditNode();
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C) && GetSelectedNode())
            {
                foreach(GameObject node in SelectedNodesList)
                {
                    clipBoardNodeCopyList.Add(node.GetComponent<NodeSPTree>());
                }
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V) && clipBoardNodeCopyList.Count > 0)
            {
                PasteClipboardNodes();
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
            {
                PSTreeApiManager.Instance.SaveFromButtonSync();
            }
            else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                PSTreeApiManager.Instance.Reload();
            }
            else if (Input.GetKey(KeyCode.Escape) && Input.GetKeyDown(KeyCode.LeftControl))
            {
                PSTreeApiManager.Instance.confirmReloadDialogBox.Show("Exit", "Are you sure to quit? (Unsaved work will be lost)", null, Application.Quit, new string[] { "Quit", "Cancel" });
            }

        }

        private void UnSelectAllLinks()
        {
            foreach (GameObject link in SelectedLinksList)
            {
                link.GetComponent<NodeLink>().UnSelect();
            }
            SelectedNodesList.Clear();
        }

        private void UnSelectAllNodes()
        {

            foreach (GameObject node in SelectedNodesList)
            {
                node.GetComponent<NodeSPTree>().UnSelect();
            }
            SelectedNodesList.Clear();

            Button editNodeButton = EditNodeButton.GetComponent<Button>();

            CostUI.readOnly = false;
            LockToggle.interactable = false;
            IsInitialToggle.interactable = false;
            editNodeButton.interactable = true;
            xPosUI.readOnly = false;
            yPosUI.readOnly = false;
            IdUI.readOnly = false;
        }

        private bool IsUiFocused()
        {
            return xPosUI.isFocused || yPosUI.isFocused || CostUI.isFocused || IdUI.isFocused;
        }

        private void TryToRemoveSelection()
        {
            //if (GetSelectedNode())
            //{
            //    RemoveNode(GetSelectedNodeScript());
            //    SelectedNode = null;
            //}
            //else if (SelectedLink)
            //{
            //    NodeLink link = SelectedLink.GetComponent<NodeLink>();
            //    link.Remove();
            //    SelectedLink = null;
            //}

            foreach(GameObject node in SelectedNodesList)
            {
                RemoveNode(node.GetComponent<NodeSPTree>());
            }
            SelectedNodesList.Clear();

            // TODO: Verify if links exist or something ?
            foreach(GameObject linkObject in SelectedLinksList)
            {
                NodeLink link = linkObject.GetComponent<NodeLink>();
                link.Remove();
            }
            SelectedLinksList.Clear();
        }

        void RemoveNode(NodeSPTree node)
        {
            node.Remove();
            Nodes.Remove(node.data.id);
        }

        private void PasteClipboardNodes()
        {
            UnSelectAllNodes();
            UnSelectAllLinks();

            NodeSPTree selectedNodeScript = clipBoardNodeCopyList.First();

            Vector3 spawnPos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));
            Vector3 firstNodePosition = new Vector3(selectedNodeScript.data.x, selectedNodeScript.data.y);

            Dictionary<int, NodeSPTree> oldNodesIdsForNewNodes = new Dictionary<int, NodeSPTree>();

            foreach (NodeSPTree originalNode in clipBoardNodeCopyList)
            {
                NodeData nData = JsonUtility.FromJson<NodeData>(JsonUtility.ToJson(originalNode.data));
               
                nData.x = RoundSnapGrid(originalNode.data.x - firstNodePosition.x + spawnPos.x);
                nData.y = RoundSnapGrid(originalNode.data.y - firstNodePosition.y + spawnPos.y);

                nData.linkedNodesIds.Clear();
                nData.id = currentIdToGenerate;
                NodeSPTree node = LoadNodeFromData(nData);
                for (int i = 0; i < node.data.skillsUnlocked.Count; i++)
                {
                    nData.skillsUnlocked[i] = PSTreeApiManager.Instance.PossibleSkills[nData.skillsUnlocked[i].id];
                }
                node.IsLocked = false;
                _ = node.UpdateImage();
                Select(node.gameObject, true);

                oldNodesIdsForNewNodes[originalNode.data.id] = node;
            }

            foreach(NodeSPTree originalNode in clipBoardNodeCopyList)
            {
                foreach(int idLink in originalNode.data.linkedNodesIds)
                {
                    if(oldNodesIdsForNewNodes.ContainsKey(idLink) && oldNodesIdsForNewNodes.ContainsKey(originalNode.data.id))
                    {
                        oldNodesIdsForNewNodes[originalNode.data.id].AddJoin(oldNodesIdsForNewNodes[idLink]);
                    }
                }
            }


        }

        public void AddNewNode()
        {
            GameObject Node = Instantiate(EmptyNodePrefab, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2)), Quaternion.identity, NodesGroup.transform);
            Node.transform.position = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);

            if (IsGridActive)
            {
                Node.transform.position = new Vector3(RoundSnapGrid(Node.transform.position.x), RoundSnapGrid(Node.transform.position.y), 0);
            }
            //Node.transform.localPosition = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);

            NodeSPTree script = Node.GetComponent<NodeSPTree>();
            AddNode(Node);
            script.OnSelectionEvent += Select;
            script.OnDragging += SetDraggingForSelectedNodes;
            script.JoinPrefab = NodeJoinPrefab;
            UpdateIds();
            Select(Node);
            script.Manager = this;
        }

        public void EditNode()
        {
            if (GetSelectedNode() == null)
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
            if (GetSelectedNode() == null)
            {
                return;
            }

            NodeSPTree script = GetSelectedNodeScript();
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
            ClearNewVisualsForm();
            SelectedVisualsToAdd.Clear();
            VisualsToAddRecyclingListView.Clear();
            VisualsToAddRecyclingListView.ItemCallback = PopulateVisualsToAdd;
            VisualsToAddRecyclingListView.RowCount = PossibleNodeVisualsToAddFiltered.Count;
            SelectedVisualUIName.text = GetSelectedNodeScript().data.visuals.name;
        }

        void ClearNewVisualsForm()
        {
            EditVisualsUIRemoveSelectedButton.interactable = false;
            UINewVisualsName.text = "";
            UINewVisualsUrl.text = "";
        }

        private void PopulateSkills(RecyclingListViewItem item, int rowIndex)
        {
            NodeData data = GetSelectedNode().GetComponent<NodeSPTree>().data;
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
                    if (otherItem != child)
                    {
                        otherItem.UnSelect();
                    }
                }
                SelectedVisualsToAdd[id] = true;

                UINewVisualsName.text = PSTreeApiManager.Instance.PossibleNodesVisuals[id].name;
                UINewVisualsUrl.text = PSTreeApiManager.Instance.PossibleNodesVisuals[id].icon;
                EditVisualsUIRemoveSelectedButton.interactable = true;
            };

            child.onUnSelectCallback = (int id) =>
            {
                SelectedVisualsToAdd[id] = false;
                ClearNewVisualsForm();
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
            if (GetSelectedNode() != null)
            {
                NodeSPTree node = GetSelectedNodeScript();
                node.data.stats[sender.transform.parent.name] = int.Parse(sender.text);
            }
        }
        public void UpdateSecondaryStatValue(TMP_InputField sender)
        {
            if (GetSelectedNode() != null)
            {
                NodeSPTree node = GetSelectedNodeScript();
                node.data.secondaryStats[sender.transform.parent.name] = int.Parse(sender.text);
            }
        }

        public NodeSPTree LoadNodeFromData(NodeData nData)
        {
            GameObject Node = Instantiate(EmptyNodePrefab, new Vector3(nData.x, nData.y), Quaternion.identity, NodesGroup.transform);
            Node.transform.position = new Vector3(Node.transform.position.x, Node.transform.position.y, 0);
            AddNode(Node);


            NodeSPTree script = Node.GetComponent<NodeSPTree>();
            script.OnSelectionEvent += Select;
            script.OnDragging += SetDraggingForSelectedNodes;
            script.JoinPrefab = NodeJoinPrefab;
            script.data = nData;
            script.IsLocked = true;
            script.Manager = this;
            return script;
        }

        private void AddNode(GameObject node, int id = -1)
        {
            int toTake = currentIdToGenerate;
            if (id > -1)
            {
                toTake = id;
            }
            Nodes[toTake] = node;
            currentIdToGenerate++;
        }

        public async Task ReloadAllNodes()
        {
            foreach (KeyValuePair<int, GameObject> kvpNode in Nodes)
            {
                NodeSPTree script = kvpNode.Value.GetComponent<NodeSPTree>();
                await script.UpdateImage();
                foreach (int id in script.data.linkedNodesIds)
                {
                    if (Nodes.ContainsKey(id))
                    {
                        script.AddJoin(Nodes[id].GetComponent<NodeSPTree>());
                    }

                }
            }
        }

        public void ReloadAllNodesImages()
        {
            foreach (KeyValuePair<int, GameObject> kvpNode in Nodes)
            {
                NodeSPTree script = kvpNode.Value.GetComponent<NodeSPTree>();
                _ = script.UpdateImage();
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
            node.data.visuals = GetSelectedVisual();

            UpdateVisualsToAdd();
            _ = node.UpdateImage();
        }

        NodeVisuals GetSelectedVisual()
        {
            foreach (KeyValuePair<int, bool> kvp in SelectedVisualsToAdd)
            {
                if (kvp.Value == true)
                {
                    NodeVisuals visual = PSTreeApiManager.Instance.PossibleNodesVisuals[kvp.Key];
                    return visual;
                }
            }

            return null;
        }

        public void AddNewVisual()
        {
            string name = UINewVisualsName.text;
            string imageUrl = UINewVisualsUrl.text;

            NodeVisuals n = GetSelectedVisual();
            if (n != null)
            {
                n.name = name;
                n.icon = imageUrl;
            }
            else
            {
                PSTreeApiManager.Instance.AddNewVisual(name, imageUrl);
            }

            UpdateVisualsToAdd();
            ReloadAllNodesImages();
        }

        public void RemoveSelectedVisual()
        {
            PSTreeApiManager.Instance.confirmReloadDialogBox.Show("Remove", "Are you sure to remove this visual?", null, RealRemoveVisual, new string[] { "Delete", "Cancel" });
        }

        private void RealRemoveVisual(int index)
        {
            if (index != 0)
            {
                return;
            }
            NodeVisuals n = GetSelectedVisual();
            if (n != null)
            {
                PSTreeApiManager.Instance.RemoveVisual(n);
                UpdateVisualsToAdd();
                ReloadAllNodesImages();
            }
        }

        private NodeSPTree GetSelectedNodeScript()
        {
            return GetSelectedNode()?.GetComponent<NodeSPTree>();
        }

        public void UpdateIds()
        {
            SortedDictionary<int, GameObject> updatedDicitonnary = new SortedDictionary<int, GameObject>();
            currentIdToGenerate = 1;
            foreach (KeyValuePair<int, GameObject> nodes in Nodes)
            {
                updatedDicitonnary[currentIdToGenerate] = nodes.Value;
                nodes.Value.GetComponent<NodeSPTree>().data.id = currentIdToGenerate;
                currentIdToGenerate++;
            }
        }

        private void SetDraggingForSelectedNodes(bool isDragging)
        {
            foreach(GameObject node in SelectedNodesList)
            {
                NodeSPTree nodeScript = node.GetComponent<NodeSPTree>();
                if (isDragging)
                {
                    nodeScript.distanceFromCamera = Vector3.Distance(nodeScript.transform.position, Camera.main.transform.position);

                    Vector3 ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    nodeScript.dragOffset = nodeScript.transform.position - ray;
                    nodeScript.dragging = isDragging;
                } else
                {
                    nodeScript.dragging = isDragging;
                }

            }
        }

        private void Select(GameObject toSelectGameObject, bool isMultipleSelect = false)
        {
            if (SelectedNodesList.Contains(toSelectGameObject) || SelectedLinksList.Contains(toSelectGameObject))
            {
                return;
            }

            NodeSPTree scriptNode = toSelectGameObject.GetComponent<NodeSPTree>();
            NodeLink scriptLink = toSelectGameObject.GetComponent<NodeLink>();

            Button editNodeButton = EditNodeButton.GetComponent<Button>();

            if (!isMultipleSelect)
            {
                UnSelectAllNodes();
                UnSelectAllLinks();
            }






            if (scriptNode)
            {
                SelectedNodesList.Add(toSelectGameObject);
                CostUI.readOnly = false;
                xPosUI.readOnly = false;
                yPosUI.readOnly = false;
                IdUI.readOnly = false;
                CostUI.text = scriptNode.data.cost.ToString();
                IdUI.text = scriptNode.data.id.ToString();

                LockToggle.isOn = scriptNode.IsLocked;
                LockToggle.interactable = true;

                IsInitialToggle.isOn = scriptNode.data.isInitial;
                IsInitialToggle.interactable = true;

                editNodeButton.interactable = true;

                scriptNode.Select();
            }
            else if (scriptLink)
            {
                CostUI.readOnly = true;
                xPosUI.readOnly = true;
                yPosUI.readOnly = true;
                IdUI.readOnly = true;
                LockToggle.interactable = false;
                IsInitialToggle.interactable = true;

                SelectedLinksList.Add(toSelectGameObject);

                editNodeButton.interactable = false;

                scriptLink.Select();
            }


        }

        public void ChangeSelectedCost()
        {
            if (GetSelectedNode())
            {
                string text = CostUI.text;
                GetSelectedNodeScript().SetCost(int.Parse(text));
            }
        }

        public void ChangeLockState()
        {
            if (GetSelectedNode())
            {
                GetSelectedNodeScript().IsLocked = LockToggle.isOn;
            }
        }

        public void ChangeIsInitial()
        {
            if (GetSelectedNode())
            {
                GetSelectedNodeScript().data.isInitial = IsInitialToggle.isOn;
            }
        }

        public static float RoundSnapGrid(float number)
        {
            float snapValue = 0.5f;
            return snapValue * Mathf.Round(number / snapValue);
        }

    }
}