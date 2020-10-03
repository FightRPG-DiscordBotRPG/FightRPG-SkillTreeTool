using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

[Serializable]
public class NodeData
{
    public int id = 0;
    public NodeVisuals visuals = new NodeVisuals();
    public Stats stats = new Stats();
    public SecondaryStats secondaryStats = new SecondaryStats();
    public List<Skill> skillsUnlocked = new List<Skill>();
    public List<State> statesProtectedFrom = new List<State>();
    public List<State> statesAdded = new List<State>();
    public List<int> linkedNodes = new List<int>();
    public float x = 0f, y = 0f;
    public int cost = 0;
    public bool isInitial = false;
}
