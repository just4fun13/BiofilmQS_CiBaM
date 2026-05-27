using Assets.Scripts.MVVM_CA.Models.ModelParams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MVVM_CA.Utils
{
    public class SimStart : MonoBehaviour
    {
        public void StartSimul()
        {
            Debug.Log($"Starting simul with params ");
            ModelParameters.ShowInDebug();
            if (ModelParameters.Is2d)
                SceneManager.LoadScene(1);
            else
                SceneManager.LoadScene(2);

        }




    }
}
