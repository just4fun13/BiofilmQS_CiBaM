using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.UI_new
{
    public class UIcontent : MonoBehaviour
    {
        public virtual void ReadAll()
        {
            Debug.Log($"ReadAll for {gameObject.name}");
        }

    }
}
