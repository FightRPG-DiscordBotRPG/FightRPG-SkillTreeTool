using Assets.Game.Code;
using DevionGames.UIWidgets;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PSTreeApiManager : MonoBehaviour
{

    public Image UIFill;
    public TMP_Text LoadingText;
    public NodeSPTreeManager NodeManager;
    public GameObject RetryButton;

    public GameObject[] GameRelatedToActivate;
    public Texture DefaultTextureIfFail;
    public DialogBox confirmReloadDialogBox;


    static PSTreeApiManager _Instance;

    public readonly Dictionary<int, NodeVisuals> PossibleNodesVisuals = new Dictionary<int, NodeVisuals>();
    public readonly Dictionary<int, Skill> PossibleSkills = new Dictionary<int, Skill>();
    public readonly Dictionary<int, State> PossibleStates = new Dictionary<int, State>();

    public readonly List<Skill> PossibleSkillsAsList = new List<Skill>();
    public readonly List<NodeVisuals> PossibleNodesVisualsAsList = new List<NodeVisuals>();

    public string BackendHost
    {
        get; private set;
    }

    private readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

    public static PSTreeApiManager Instance
    {
        get
        {
            return _Instance;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _Instance = this;
        try
        {
            string configPath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.json";

            if (!File.Exists(configPath))
            {
                var jsonConfig = new JSONObject();
                jsonConfig.Add("host", "http://127.0.0.1:25012");

                File.WriteAllText(configPath, jsonConfig.ToString());
            }


            JSONNode config = JSON.Parse(File.ReadAllText(configPath));
           
            BackendHost = config["host"];
        } catch
        {
            BackendHost = "http://127.0.0.1:25012";
        }

        _ = Load();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Reload()
    {
        confirmReloadDialogBox.Show("Reloading", "Are you sure to reload? (Unsaved work will be removed)", null, OnDialogResult, new string[] { "Reload", "Cancel" });
    }

    private async void OnDialogResult(int index)
    {
        if (index == 0)
        {
            await Load();
        }
    }

    private void SetActiveAllRelatedGameObjects(bool value)
    {
        foreach (GameObject go in GameRelatedToActivate)
        {
            go.SetActive(value);
        }
    }

    public void LoadFromButtonSync()
    {
        _ = Load();
    }

    public void SaveFromButtonSync()
    {
        _ = Save();
    }

    public async Task Save()
    {
        GameObject LoadingScreen = UIFill.gameObject.transform.parent.gameObject;
        LoadingScreen.SetActive(true);
        SetActiveAllRelatedGameObjects(false);
        RetryButton.SetActive(false);

        try
        {
            await SaveNodesVisuals(0f, 0.5f);
            await SaveNodes(0.5f, 1f);
        }
        catch (Exception ex)
        {
            LoadingText.text = "Error: " + ex.Message;
            UIFill.fillAmount = 1;

            // Display button restart
            RetryButton.SetActive(true);
        }

    }

    async Task SaveNodes(float loadingStart, float loadingMax)
    {
        float segment = (loadingMax - loadingStart) / 2;

        //Step One
        LoadingText.text = "Parsing Nodes to Json...";
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        List<string> allNodesData = new List<string>();
        JSONNode nodesJson = new JSONObject();

        foreach (GameObject node in NodeManager.Nodes.Values)
        {
            NodeSPTree nodeScript = node.GetComponent<NodeSPTree>();
            nodeScript.UpdateLinksData();
            allNodesData.Add(JsonUtility.ToJson(nodeScript.data));
        }

        nodesJson["nodes"] = allNodesData;
        // Step 2

        LoadingText.text = "Sending to server... (Reload is automatic after save)";
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;
        WWWForm data = new WWWForm();
        data.AddField("dataNodes", nodesJson.ToString());


        await PostRequestAsync(BackendHost + "/nodes_update", data);
        await Load();

    }

    async Task SaveNodesVisuals(float loadingStart, float loadingMax)
    {
        float segment = (loadingMax - loadingStart) / 2;

        //Step One
        LoadingText.text = "Parsing Visuals to Json...";
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        List<string> allVisualsData = new List<string>();
        JSONNode visualsJson = new JSONObject();

        foreach (NodeVisuals visual in PossibleNodesVisualsAsList)
        {
            JSONNode n = JSON.Parse(JsonUtility.ToJson(visual));
            n["localizedNames"] = new JSONObject();
            foreach (KeyValuePair<string, string> kvp in visual.localizedNames)
            {
                n["localizedNames"].Add(kvp.Key, kvp.Value);
            }
            allVisualsData.Add(n.ToString());
        }

        visualsJson["visuals"] = allVisualsData;
        // Step 2

        LoadingText.text = "Sending visuals to server...";
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;
        WWWForm data = new WWWForm();
        data.AddField("visuals", visualsJson.ToString());

        await PostRequestAsync(BackendHost +"/visuals_update", data);

    }

    public async Task Load()
    {
        GameObject LoadingScreen = UIFill.gameObject.transform.parent.gameObject;
        LoadingScreen.SetActive(true);
        SetActiveAllRelatedGameObjects(false);
        RetryButton.SetActive(false);
        NodeManager.Clear();
        UIFill.fillAmount = 0f;
        try
        {
            await LoadNodesVisuals(0.0f, 0.25f);
            UIFill.fillAmount = 0.25f;

            await LoadSkills(0.25f, 0.5f);
            UIFill.fillAmount = 0.5f;

            await LoadStates(0.5f, 0.75f);
            UIFill.fillAmount = 0.75f;

            await LoadAllNodes(0.75f, 1f);
            UIFill.fillAmount = 1;

            LoadingScreen.SetActive(false);

            // Activate every needed components
            SetActiveAllRelatedGameObjects(true);
        }
        catch (Exception ex)
        {
            LoadingText.text = "Error: " + ex.ToString();
            UIFill.fillAmount = 1;

            // Display button restart
            RetryButton.SetActive(true);
        }
    }

    async Task LoadAllNodes(float loadingStart, float loadingMax)
    {
        float segment = (loadingMax - loadingStart) / 3;

        LoadingText.text = "Loading Nodes...";
        UnityWebRequest request = await GetRequestAsync(BackendHost + "/nodes");


        LoadingText.text = "Parsing Data...";
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        JSONArray array = JSON.Parse(request.downloadHandler.text)["nodes"].AsArray;

        float loadingNodeSegment = (loadingStart + segment) - loadingStart / array.Count;

        foreach (JSONNode json in array)
        {
            loadingStart += loadingNodeSegment;
            UIFill.fillAmount = loadingStart;
            NodeData nData = JsonUtility.FromJson<NodeData>(json.ToString());
            LoadingText.text = "Loading Node: " + nData.visuals.name;

            foreach (string skillId in json["skillsUnlockedIds"].AsStringArray)
            {
                nData.skillsUnlocked.Add(PossibleSkills[int.Parse(skillId)]);
            }

            NodeManager.LoadNodeFromData(nData);
        }

        LoadingText.text = "Linking and updating nodes data...";
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        NodeManager.UpdateIds();
        await NodeManager.ReloadAllNodes();

    }

    async Task LoadNodesVisuals(float loadingStart, float loadingMax)
    {
        float segment = (loadingMax - loadingStart) / 3;
        LoadingText.text = "Loading Nodes Visuals...";

        PossibleNodesVisuals.Clear();
        PossibleNodesVisualsAsList.Clear();
        UnityWebRequest request = await GetRequestAsync(BackendHost +"/nodes/visuals");
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        LoadingText.text = "Parsing Data...";

        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        JSONNode data = JSON.Parse(request.downloadHandler.text);
        foreach (JSONNode visual in data["visuals"].Values)
        {
            PossibleNodesVisuals[visual["id"]] = JsonUtility.FromJson<NodeVisuals>(visual.ToString());
            var localizedNames = visual["localizedNames"];

            foreach (KeyValuePair<string, JSONNode> keyValuePair in localizedNames)
            {
                PossibleNodesVisuals[visual["id"]].localizedNames.Add(keyValuePair.Key, keyValuePair.Value);
            }

            PossibleNodesVisualsAsList.Add(PossibleNodesVisuals[visual["id"]]);
        }

    }

    async Task LoadSkills(float loadingStart, float loadingMax)
    {
        float segment = (loadingMax - loadingStart) / 3;
        LoadingText.text = "Loading Skills...";

        PossibleSkills.Clear();
        PossibleSkillsAsList.Clear();
        UnityWebRequest request = await GetRequestAsync(BackendHost +"/skills");
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        LoadingText.text = "Parsing Data...";

        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        JSONNode data = JSON.Parse(request.downloadHandler.text);
        foreach (JSONNode skill in data["skills"].Values)
        {
            PossibleSkills[skill["id"]] = JsonUtility.FromJson<Skill>(skill.ToString());
            PossibleSkillsAsList.Add(PossibleSkills[skill["id"]]);
        }
    }

    async Task LoadStates(float loadingStart, float loadingMax)
    {
        float segment = (loadingMax - loadingStart) / 3;
        LoadingText.text = "Loading States...";

        PossibleStates.Clear();
        UnityWebRequest request = await GetRequestAsync(BackendHost + "/states");
        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        LoadingText.text = "Parsing Data...";

        loadingStart += segment;
        UIFill.fillAmount = loadingStart;

        JSONNode data = JSON.Parse(request.downloadHandler.text);
        foreach (JSONNode state in data["states"].Values)
        {
            PossibleStates[state["id"]] = JsonUtility.FromJson<State>(state.ToString());
        }

    }

    public async Task<UnityWebRequest> GetRequestAsync(string url, bool throwException = true)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        if (request.isNetworkError && throwException)
        {
            throw new Exception(request.error);
        }
        else
        {
            return request;
        }
    }

    public async Task<UnityWebRequest> PostRequestAsync(string url, WWWForm data, bool throwException = true)
    {
        UnityWebRequest request = UnityWebRequest.Post(url, data);
        await request.SendWebRequest();
        if (request.isNetworkError && throwException)
        {
            throw new Exception(request.error);
        }
        else
        {
            return request;
        }
    }

    public async Task<Texture> GetTextureAsync(string url)
    {
        if (url.StartsWith("data:image") || url.StartsWith("base64,") || !url.StartsWith("http"))
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(url.Substring(url.IndexOf("base64,") + 7));
                Texture2D tex = new Texture2D(0, 0);
                tex.LoadImage(imageBytes);

                return tex;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return DefaultTextureIfFail;
            }


        }
        else
        {
            if (TextureCache.ContainsKey(url))
            {
                return TextureCache[url];
            }
            else
            {
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
                await request.SendWebRequest();

                if (request.isNetworkError)
                {
                    return DefaultTextureIfFail;
                }
                else
                {
                    Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    TextureCache.Add(url, texture);
                    return texture;
                }
            }

        }




    }

    public async Task<Texture2D> GetTextureNode(int idVisual)
    {
        Texture2D tex;
        NodeVisuals value;
        if (PossibleNodesVisuals.TryGetValue(idVisual, out value))
        {
            tex = (Texture2D)await GetTextureAsync(value.icon);
        }
        else
        {
            tex = (Texture2D)DefaultTextureIfFail;
        }

        return PrepareTextureForNode(tex);
    }

    public async Task<Texture2D> GetTextureNode(string url)
    {
        Texture2D tex;
        try
        {
            tex = (Texture2D)await GetTextureAsync(url);
        }
        catch
        {
            tex = (Texture2D)DefaultTextureIfFail;
        }

        return PrepareTextureForNode(tex);
    }

    public Sprite GetSpriteForNode(Texture2D tx)
    {
        return Sprite.Create(tx, new Rect(0f, 0f, tx.width, tx.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Texture2D PrepareTextureForNode(Texture2D tex)
    {
        RenderTexture rt = new RenderTexture(64, 64, 24);
        RenderTexture.active = rt;
        Graphics.Blit(tex, rt);
        Texture2D result = new Texture2D(64, 64);
        result.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        result.Apply();
        return result;
    }

    public void AddNewVisual(string name, string imageUrl)
    {
        NodeVisuals n = new NodeVisuals
        {
            name = name,
            icon = imageUrl,
            id = PossibleNodesVisuals.Count + 1
        };

        PossibleNodesVisuals.Add(n.id, n);
        PossibleNodesVisualsAsList.Add(n);
    }

    public void RemoveVisual(NodeVisuals n)
    {
        PossibleNodesVisuals.Remove(n.id);
        PossibleNodesVisualsAsList.Remove(n);
    }
}


public static class ExtensionMethods
{
    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<object>();
        asyncOp.completed += obj => { tcs.SetResult(null); };
        return ((Task)tcs.Task).GetAwaiter();
    }
}