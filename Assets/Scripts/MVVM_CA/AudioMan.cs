using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.MVVM_CA
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioMan : MonoBehaviour
    {
        [SerializeField] private AudioClip ExplosionClip;
        [SerializeField] private AudioClip PhotoShotClip;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }
        public void PlayExplosion()
        {
            audioSource.volume = 0.12f;
            audioSource.PlayOneShot(ExplosionClip);
        }
        public void PlayPhotShot()
        {
            audioSource.volume = 0.12f;
            audioSource.PlayOneShot(PhotoShotClip);
        }
        public void MakeEndless()
        {
            audioSource.loop = true;
        }
    }
}
