using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Controls the steps in the in coaching card.
    /// </summary>
    public class StepManager : MonoBehaviour
    {
        [Serializable]
        class Step
        {
            [SerializeField]
            public GameObject stepObject;

            [SerializeField]
            public string buttonText;
            public Step(Transform t, String s)
            {
                stepObject = t.gameObject;
                buttonText = s;
            }
        }

        [SerializeField]
        public TextMeshProUGUI m_StepButtonTextField;

        [SerializeField]
        List<Step> m_StepList = new List<Step>();

        int m_CurrentStepIndex = 0;

        [SerializeField] string endText = "Restart";
        [SerializeField] bool closeOnEnd;

        void Awake()
        {
            if (m_StepList.Count != 0) return;
            Transform cards = transform;
            int n = cards.childCount - 1;
            for (int i = 2; i < n; i++)
            {
                Transform card = cards.GetChild(i);
                m_StepList.Add(new Step(card, i == n-1 ? endText : "Continue"));
                Transform dots = card.GetChild(1);
                for (int j = 0; j < n-2; j++)
                {
                    if (j == i - 2) dots.GetChild(j).GetChild(0).gameObject.SetActive(true);
                    else dots.GetChild(j).GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        public void Next()
        {
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);
            m_CurrentStepIndex = (m_CurrentStepIndex + 1) % m_StepList.Count;
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
            m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;
            if (m_CurrentStepIndex == 0 && closeOnEnd) transform.parent.gameObject.SetActive(false);
        }
    }
}
