using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace enp_unity_extensions.Runtime.Scripts.UI.Windows
{
    public class AnimatedWindow : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private void OnValidate()
        {
            _animator = GetComponent<Animator>();
        }
    
        public void Open(string animName)
        {
            gameObject.SetActive(true);
            _animator.Play(animName);
        }
    
        public void Close(string animName, UnityAction onComplete)
        {
            DoAnimCloseCoroutine(animName, onComplete).Start();
        }

        IEnumerator DoAnimCloseCoroutine(string animName, UnityAction onComplete)
        {
            _animator.Play(animName);
        
            var animState = _animator.runtimeAnimatorController.animationClips;
            float delay = 1;
            foreach (var clips in animState)
            {
                if (clips.name == animName)
                {
                    delay = clips.length;
                    break;
                }
            }

            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }
}