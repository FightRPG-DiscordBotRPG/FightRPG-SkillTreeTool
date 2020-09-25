using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class SBrackets
{
    public int this[string key]
    {
        get
        {
            return (int)GetType().GetField(FormatKey(key)).GetValue(this);
        }
        set
        {
            GetType().GetField(FormatKey(key)).SetValue(this, value);
        }
    }

    private string FormatKey(string key)
    {
        return key[0].ToString().ToLower() + key.Substring(1).Replace(" ", "");
    }
}
