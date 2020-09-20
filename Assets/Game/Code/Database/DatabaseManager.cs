using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance = null;
    public TMP_InputField UsernameInput, PasswordInput, DatabaseNameInput;
    public TMP_Text ErrorText;
    public Button ConnectButton;
    private SqlConnection Connection;

    string Username = "", Password = "", DatabaseName = "";
    bool isLoading = false;

    private string ConnectionString
    {
        get
        {
            string passwordString = Password.Length > 0 ? "PASSWORD=" + Password : "";
            return $"SERVER=127.0.0.1;DATABASE={DatabaseName};UID={Username};{passwordString}";
        }
    }


    public DatabaseManager()
    {
        Instance = this;
    }

    void Start()
    {
        // Load from player pref
        LoadConnectionData();
    }

    //// Update is called once per frame
    //void Update()
    //{

    //}

    void LoadConnectionData()
    {
        Username = PlayerPrefs.GetString("dbUsername", "");
        Password = PlayerPrefs.GetString("dbPassword", "");
        DatabaseName = PlayerPrefs.GetString("dbName", "");
        UpdateForms();
    }

    void UpdateForms()
    {
        UsernameInput.text = Username;
        PasswordInput.text = Password;
        DatabaseNameInput.text = DatabaseName;
    }

    public async void Connect()
    {
        Connection = new SqlConnection(ConnectionString);
        try
        {
            Debug.Log("Try Connecting");
            isLoading = true;
            LoadingUIUpdate();
            await Connection.OpenAsync();
        }
        catch (Exception ex)
        {
            Debug.Log("Catch Connecting");
            ErrorText.text = ex.Message;
            LoadingUIUpdate();
        }


        isLoading = false;
        LoadingUIUpdate(); 
    }

    void LoadingUIUpdate()
    {
        if (isLoading)
        {
            ErrorText.text = "Loading...";
            UsernameInput.readOnly = true;
            PasswordInput.readOnly = true;
            DatabaseNameInput.readOnly = true;
            ConnectButton.interactable = false;
        }
        else
        {
            UsernameInput.readOnly = false;
            PasswordInput.readOnly = false;
            DatabaseNameInput.readOnly = false;
            ConnectButton.interactable = true;
            if(ErrorText.text == "Loading...")
            {
                ErrorText.text = "";
            }
        }

    }
}
