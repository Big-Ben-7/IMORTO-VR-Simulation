using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] Slider audioLevel;
    [SerializeField] TMP_Text valueText;
    [SerializeField] Toggle hints, vignette;
    [SerializeField] TMP_Dropdown rotation;
    [SerializeField] GameObject[] hintTips;
    int tipCount;
    List<int> hintIndices = new List<int>();
    [SerializeField] GameObject tvc;
    [SerializeField] GameObject turnProvider;
    //public static SettingsMenu instance;

    void Awake()
    {
        //instance = this;
        tipCount = hintTips.Length;
        for (int i = 0; i < tipCount; i++)
        {
            hintTips[i] = hintTips[i].transform.parent.gameObject;
        }
    }

    public void UpdateValue()
    {
        float value = audioLevel.value;
        valueText.SetText(value.ToString("0") + "%");
        AudioListener.volume = value / 100f;
    }

    public void ToggleVignette()
    {
        tvc.gameObject.SetActive(vignette.isOn);
    }

    public void ToggleHints()
    {
        if (!hints.isOn)
        {
            for (int i = 0; i < tipCount; i++)
            {
                GameObject tip = hintTips[i];
                if (tip.activeSelf)
                {
                    tip.SetActive(false);
                    hintIndices.Add(i);
                }
            }
            return;
        }
        foreach (int i in hintIndices) hintTips[i].SetActive(true);
        hintIndices.Clear();
    }

    public void UpdateRotation()
    {
        switch (rotation.value)
        {
            case 0:
                Player.snapTurn = 0;
                turnProvider.SetActive(true);
                break;
            case 1:
                Player.snapTurn = 30;
                turnProvider.SetActive(false);
                break;
            case 2:
                Player.snapTurn = 45;
                turnProvider.SetActive(false);
                break;
            case 3:
                Player.snapTurn = 90;
                turnProvider.SetActive(false);
                break;
        }
    }

    public void RemoveIndex(int i)
    {
        hintIndices.RemoveAll(item => item == i);
    }
    public void AddIndex(int i)
    {
        if (!hintIndices.Contains(i)) hintIndices.Add(i);
    }
}
