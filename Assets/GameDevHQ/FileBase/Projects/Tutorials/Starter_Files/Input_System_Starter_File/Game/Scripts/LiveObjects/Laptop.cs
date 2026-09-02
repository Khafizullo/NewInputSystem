using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Cinemachine;

namespace Game.Scripts.LiveObjects
{
    public class Laptop : MonoBehaviour
    {
        [SerializeField] private Slider _progressBar;
        [SerializeField] private int _hackTime = 5;
        private bool _hacked = false;
        [SerializeField] private CinemachineVirtualCamera[] _cameras;
        private int _activeCamera = 0;
        [SerializeField] private InteractableZone _interactableZone;

        public static event Action onHackComplete;
        public static event Action onHackEnded;

        private void OnEnable()
        {
            InteractableZone.onHoldStarted += InteractableZone_onHoldStarted;
            InteractableZone.onHoldEnded += InteractableZone_onHoldEnded;
        }

        private void Update()
        {
            if (_hacked)
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return;

                if (keyboard.eKey.wasPressedThisFrame)
                {
                    var previous = _activeCamera;
                    _activeCamera++;

                    if (_activeCamera >= _cameras.Length)
                        _activeCamera = 0;

                    _cameras[_activeCamera].Priority = 11;
                    _cameras[previous].Priority = 9;
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    _hacked = false;
                    onHackEnded?.Invoke();
                    ResetCameras();
                }
            }
        }

        void ResetCameras()
        {
            foreach (var cam in _cameras)
            {
                cam.Priority = 9;
            }
        }

        private void InteractableZone_onHoldStarted(int zoneID)
        {
            if (zoneID == 3 && !_hacked)
            {
                _progressBar.gameObject.SetActive(true);
                StartCoroutine(HackingRoutine());
                onHackComplete?.Invoke();
            }
        }

        private void InteractableZone_onHoldEnded(int zoneID)
        {
            if (zoneID == 3)
            {
                if (_hacked) return;

                StopAllCoroutines();
                _progressBar.gameObject.SetActive(false);
                _progressBar.value = 0;
                onHackEnded?.Invoke();
            }
        }

        IEnumerator HackingRoutine()
        {
            while (_progressBar.value < 1)
            {
                _progressBar.value += Time.deltaTime / _hackTime;
                yield return new WaitForEndOfFrame();
            }

            _hacked = true;
            _interactableZone.CompleteTask(3);
            _progressBar.gameObject.SetActive(false);
            _cameras[0].Priority = 11;
        }

        private void OnDisable()
        {
            InteractableZone.onHoldStarted -= InteractableZone_onHoldStarted;
            InteractableZone.onHoldEnded -= InteractableZone_onHoldEnded;
        }
    }
}