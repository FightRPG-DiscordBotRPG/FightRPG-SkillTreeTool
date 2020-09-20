using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PSTreeApiManager : MonoBehaviour
{

    public Image UIFill;
    public TMP_Text LoadingText;

    public GameObject[] GameRelatedToActivate;

    // Start is called before the first frame update
    void Start()
    {
        Load();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    async void Load()
    {
        try
        {
            await LoadAllNodes();
            UIFill.fillAmount = 1;
            GameObject LoadingScreen = UIFill.gameObject.transform.parent.gameObject;
            LoadingScreen.SetActive(false);

            // Activate every needed components
            foreach(GameObject go in GameRelatedToActivate) {
                go.SetActive(true);
            }
        } catch (Exception ex)
        {
            LoadingText.text = "Error: " + ex.Message;
            UIFill.fillAmount = 1;

            // Display button restart
        }

    }

    async Task LoadAllNodes()
    {
        LoadingText.text = "Loading Nodes...";
        UnityWebRequest request = await GetRequestAsync("http://localhost:25012/nodes");

        LoadingText.text = request.downloadHandler.text;

        // TODO: Real loading

    }

    async Task<UnityWebRequest> GetRequestAsync(string uri)
    {
        UnityWebRequest request = UnityWebRequest.Get(uri);
        await request.SendWebRequest();
        if(request.isNetworkError)
        {
            throw new Exception(request.error);
        } else
        {
            return request;
        }
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