using TMPro;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System;

public class ShowKeyboard : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(OpenKeyboard);
    }
    public void OpenKeyboard(String x)
    {
        NonNativeKeyboard.Instance.InputField = inputField;
        NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);
        SetCaretAlpha(1);
        NonNativeKeyboard.Instance.OnClosed += Instance_OnClosed;
    }

    void Instance_OnClosed(object sender, System.EventArgs e)
    {
        SetCaretAlpha(0);
        NonNativeKeyboard.Instance.OnClosed -= Instance_OnClosed;
    }

    public void SetCaretAlpha(float a)
    {
        inputField.customCaretColor = true;
        Color caretColor = inputField.caretColor;
        caretColor.a = a;
        inputField.caretColor = caretColor;
    }
}
