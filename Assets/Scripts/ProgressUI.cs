
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class ProgressUI : MonoBehaviour 
    {
        [SerializeField] private GameObject parentPanel;
        [SerializeField] private TMP_Text progressPercentage;
        [SerializeField] private TMP_Text timeAmount;
        [SerializeField] private Image fillImage;

        [SerializeField] private TMP_Text timeText;

        public void Progress(float progress)
        {
            fillImage.fillAmount= progress;
            progressPercentage.text = $"{(progress*100).ToString("###")}%";
        }
        public void Hide()
        {
            parentPanel.SetActive(false);
        }
        public void Show()
        {
            parentPanel.SetActive(true);
        }

        public void RefreshTime(float time)
        {
            timeAmount.text = $"{time} sec.";
        }

        public void ShowTime(Vector2Int v)
        {
            timeText.text = "Real time: " + IntToStrTime(v.x) + Environment.NewLine + "Simul time: "+ IntToStrTime(v.y);    
        }

        public void ShowTimeWithProgress(Vector3 v)
        {
            timeText.text = "Real time: " + IntToStrTime((int)v.x) + Environment.NewLine
                + "Simul time: " + IntToStrTime((int)v.y) + Environment.NewLine
                + "Total progress: " + (v.z);
        }


        private string IntToStrTime(int t)
        {
            if (t < 3600)
                return  (t / 60).ToString("00m") + ":" + (t % 60).ToString("00s");
            else
                return (t / 3600).ToString("00h") + ":" + ((t / 60)%60).ToString("00m");
        }
    }
}
