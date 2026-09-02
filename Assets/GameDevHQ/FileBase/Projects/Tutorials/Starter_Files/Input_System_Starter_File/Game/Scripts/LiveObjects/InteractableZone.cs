using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Scripts.UI;

namespace Game.Scripts.LiveObjects
{
    public class InteractableZone : MonoBehaviour
    {
        private enum ZoneType
        {
            Collectable,
            Action,
            HoldAction
        }

        private enum KeyState
        {
            Press,
            PressHold
        }

        [SerializeField] private ZoneType _zoneType;
        [SerializeField] private int _zoneID;
        [SerializeField] private int _requiredID;
        [SerializeField] [Tooltip("Press the (---) Key to .....")] private string _displayMessage;
        [SerializeField] private GameObject[] _zoneItems;
        private bool _inZone = false;
        private bool _itemsCollected = false;
        private bool _actionPerformed = false;
        [SerializeField] private Sprite _inventoryIcon;
        [SerializeField] private KeyCode _zoneKeyInput;
        [SerializeField] private KeyState _keyState;
        [SerializeField] private GameObject _marker;

        private bool _inHoldState = false;

        private static int _currentZoneID = 0;
        public static int CurrentZoneID
        {
            get => _currentZoneID;
            set => _currentZoneID = value;
        }

        public static event Action<InteractableZone> onZoneInteractionComplete;
        public static event Action<int> onHoldStarted;
        public static event Action<int> onHoldEnded;

        private void OnEnable()
        {
            onZoneInteractionComplete += SetMarker;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && _currentZoneID > _requiredID)
            {
                switch (_zoneType)
                {
                    case ZoneType.Collectable:
                        if (!_itemsCollected)
                        {
                            _inZone = true;
                            string msg = !string.IsNullOrEmpty(_displayMessage)
                                ? $"Press the {_zoneKeyInput} key to {_displayMessage}."
                                : $"Press the {_zoneKeyInput} key to collect";
                            UIManager.Instance.DisplayInteractableZoneMessage(true, msg);
                        }
                        break;

                    case ZoneType.Action:
                        if (!_actionPerformed)
                        {
                            _inZone = true;
                            string msg = !string.IsNullOrEmpty(_displayMessage)
                                ? $"Press the {_zoneKeyInput} key to {_displayMessage}."
                                : $"Press the {_zoneKeyInput} key to perform action";
                            UIManager.Instance.DisplayInteractableZoneMessage(true, msg);
                        }
                        break;

                    case ZoneType.HoldAction:
                        _inZone = true;
                        string holdMsg = !string.IsNullOrEmpty(_displayMessage)
                            ? $"Press the {_zoneKeyInput} key to {_displayMessage}."
                            : $"Hold the {_zoneKeyInput} key to perform action";
                        UIManager.Instance.DisplayInteractableZoneMessage(true, holdMsg);
                        break;
                }
            }
        }

        private void Update()
        {
            if (_inZone)
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return;

                // Check KeyControl based on serialized KeyCode
                Key targetKey = MapKeyCodeToKey(_zoneKeyInput);
                var keyControl = keyboard[targetKey];

                if (keyControl == null) return;

                if (keyControl.wasPressedThisFrame && _keyState != KeyState.PressHold)
                {
                    switch (_zoneType)
                    {
                        case ZoneType.Collectable:
                            if (!_itemsCollected)
                            {
                                CollectItems();
                                _itemsCollected = true;
                                UIManager.Instance.DisplayInteractableZoneMessage(false);
                            }
                            break;

                        case ZoneType.Action:
                            if (!_actionPerformed)
                            {
                                PerformAction();
                                _actionPerformed = true;
                                UIManager.Instance.DisplayInteractableZoneMessage(false);
                            }
                            break;
                    }
                }
                else if (keyControl.isPressed && _keyState == KeyState.PressHold && !_inHoldState)
                {
                    _inHoldState = true;

                    switch (_zoneType)
                    {
                        case ZoneType.HoldAction:
                            PerformHoldAction();
                            break;
                    }
                }

                if (keyControl.wasReleasedThisFrame && _keyState == KeyState.PressHold)
                {
                    _inHoldState = false;
                    onHoldEnded?.Invoke(_zoneID);
                }
            }
        }

        private Key MapKeyCodeToKey(KeyCode keyCode)
        {
            if (Enum.TryParse(keyCode.ToString(), out Key key))
                return key;

            return Key.E; // Fallback
        }

        private void CollectItems()
        {
            foreach (var item in _zoneItems)
            {
                item.SetActive(false);
            }

            UIManager.Instance.UpdateInventoryDisplay(_inventoryIcon);
            CompleteTask(_zoneID);
            onZoneInteractionComplete?.Invoke(this);
        }

        private void PerformAction()
        {
            foreach (var item in _zoneItems)
            {
                item.SetActive(true);
            }

            if (_inventoryIcon != null)
                UIManager.Instance.UpdateInventoryDisplay(_inventoryIcon);

            onZoneInteractionComplete?.Invoke(this);
        }

        private void PerformHoldAction()
        {
            UIManager.Instance.DisplayInteractableZoneMessage(false);
            onHoldStarted?.Invoke(_zoneID);
        }

        public GameObject[] GetItems() => _zoneItems;
        public int GetZoneID() => _zoneID;

        public void CompleteTask(int zoneID)
        {
            if (zoneID == _zoneID)
            {
                _currentZoneID++;
                onZoneInteractionComplete?.Invoke(this);
            }
        }

        public void ResetAction(int zoneID)
        {
            if (zoneID == _zoneID)
                _actionPerformed = false;
        }

        public void SetMarker(InteractableZone zone)
        {
            if (_marker != null)
                _marker.SetActive(_zoneID == _currentZoneID);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _inZone = false;
                UIManager.Instance.DisplayInteractableZoneMessage(false);
            }
        }

        private void OnDisable()
        {
            onZoneInteractionComplete -= SetMarker;
        }
    }
}