using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class ScanPanel : UIPanel
{
    [SerializeField] Image[] bars;
    float[] percents = new float[4];
    float[] refs = new float[4];
    WaitForSeconds fifth;
    protected override void Awake()
    {
        base.Awake();
        for (int i = 0; i < 4; i++) percents[i] = bars[i].fillAmount;
        fifth = Player.fifth;
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(FillBar());
    }
    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        if (canvas.enabled) StartCoroutine(FillBar());
    }
    IEnumerator FillBar()
    {
        for (int i = 0; i < 4; i++) bars[i].fillAmount = 0;
        yield return fifth;
        while (true)
        {
            bool exit = true;
            for (int i = 0; i < 4; i++)
            {
                float f = bars[i].fillAmount = Mathf.SmoothDamp(bars[i].fillAmount, percents[i], ref refs[i], 0.4f);
                if (Mathf.Abs(percents[i] - f) > 0.01f) exit = false;
            }
            if (exit) break;
            yield return null;
        }
        for (int i = 0; i < 4; i++) bars[i].fillAmount = percents[i];
    }
}
