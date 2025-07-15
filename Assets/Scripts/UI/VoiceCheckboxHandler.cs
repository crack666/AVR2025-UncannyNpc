using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Handler for voice selection checkboxes
    /// Handles the connection between checkbox toggles and voice changes
    /// </summary>
    public class VoiceCheckboxHandler : MonoBehaviour
    {
        [SerializeField] private int voiceIndex;
        [SerializeField] private Toggle checkbox;
        
        private void Awake()
        {
            // Automatically find the checkbox on the same GameObject
            if (checkbox == null)
                checkbox = GetComponent<Toggle>();
        }
        
        private void Reset()
        {
            // This is called when the component is first added or reset in the editor
            // Automatically assign the Toggle component
            if (checkbox == null)
                checkbox = GetComponent<Toggle>();
        }
        
        private void OnValidate()
        {
            // This is called when the component is changed in the editor
            // Ensure we have a checkbox reference
            if (checkbox == null)
                checkbox = GetComponent<Toggle>();
        }
        
        private void Start()
        {
            // Add the event listener
            if (checkbox != null)
            {
                checkbox.onValueChanged.AddListener(OnCheckboxChanged);
                Debug.Log($"[UI] Voice checkbox handler {voiceIndex} initialized with checkbox: {checkbox.name}");
            }
            else
            {
                Debug.LogError($"[UI] No checkbox found for VoiceCheckboxHandler on {gameObject.name}");
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event listener
            if (checkbox != null)
            {
                checkbox.onValueChanged.RemoveListener(OnCheckboxChanged);
            }
        }
        
        public void SetVoiceIndex(int index)
        {
            voiceIndex = index;
            Debug.Log($"[UI] Voice checkbox handler voice index set to: {index}");
        }
        
        private void OnCheckboxChanged(bool isOn)
        {
            Debug.Log($"[UI] Voice checkbox {voiceIndex} changed to: {isOn}");
            
            if (isOn) // Only trigger when checkbox is turned ON
            {
                Debug.Log($"[UI] Attempting to change voice to index: {voiceIndex}");
                
                // Find the UI Manager and call the voice change method
                var uiManager = FindFirstObjectByType<Managers.NpcUiManager>();
                if (uiManager != null)
                {
                    Debug.Log($"[UI] Found NpcUiManager, calling OnVoiceCheckboxChanged");
                    uiManager.OnVoiceCheckboxChanged(voiceIndex);
                }
                else
                {
                    Debug.LogError("[UI] NpcUiManager not found in scene");
                }
            }
        }
    }
}
