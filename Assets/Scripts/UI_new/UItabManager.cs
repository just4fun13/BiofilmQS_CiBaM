using Assets.Scripts.MVVM_CA.Models.ModelParams;
using Assets.Scripts.UI_new;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class UItabManager : MonoBehaviour
    {

        [SerializeField] private Image[] tabImages;

        [SerializeField] private GameObject[] tabContents;

        [SerializeField] private UIcontent[] uiData;

        Color activeColor = new Color(67f / 255f, 125f / 255f, 224f / 255f);
        Color inactiveColor = new Color(26f / 255f, 30f / 255f, 40f / 255f);

        public void ClickTab(int tabIndex)
        {
            Debug.Log($"Clicked on tab {tabIndex}");
            for (int i = 0; i < tabImages.Length; i++)
            {
                bool show = (i == tabIndex);
                tabImages[i].color = show ? activeColor : inactiveColor;
                tabContents[i].SetActive(show);
            }
        }

        public void ReadAllTabs()
        {
            foreach (UIcontent ui in uiData)
                ui.ReadAll();
        }
    }
}
