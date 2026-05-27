using Assets.Scripts.MVVM_CA.Models.ModelParams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI_new
{
    public class UImainSettings : UIcontent
    {

        [SerializeField] private TMP_InputField timeStepText;
        [SerializeField] private TMP_InputField maxTimeText;
        [SerializeField] private TMP_InputField doScreenPeriod;

        public void ChangeShowLog(bool showLog)
        {
            ModelParameters.mainParameters.WriteLog = showLog;
            ModelParameters.ShowInDebug();
            DoneChange();
        }

        public void ChangeDynamicUpdate(bool dynUp)
        {
            ModelParameters.mainParameters.DynamicUpdate = dynUp;
            ModelParameters.ShowInDebug();
            DoneChange();
        }

        public void DoneChange()
        {
            ModelParameters.mainParameters.TimeStep = double.Parse(timeStepText.text);
            ModelParameters.mainParameters.MaxTimeInHours = double.Parse(maxTimeText.text);
            ModelParameters.mainParameters.DoScreens = int.Parse(doScreenPeriod.text);
            ModelParameters.ShowInDebug();
        }

        public override void ReadAll()
        {
            DoneChange();
        }
    }
}
