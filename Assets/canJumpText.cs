using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class canJumpText : MonoBehaviour
{
    [Tooltip("Optional reference to playerControl; if null, will FindObjectOfType on Start.")]
    public playerControl player;

    [Tooltip("UI Text (legacy) to update. If set, will be used.")]
    public Text uiText;

    [Tooltip("TextMeshProUGUI to update. If set, will be used.")]
    public TextMeshProUGUI tmpText;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<playerControl>();

        if (uiText == null && tmpText == null)
            Debug.LogWarning("canJumpText: no text component assigned (uiText or tmpText).");
    }

    void Update()
    {
        if (player == null)
            return;

        string line1 = player.CanJump ? "can jump" : "can't jump";
        string line2 = player != null ? player.debugLocomotionState.ToString() : "-";

        string combined = line1 + "\n" + line2;

        if (tmpText != null)
        {
            tmpText.text = combined;
        }


        if (uiText != null)
        {
            uiText.text = combined;
        }

        if (!tmpText && !uiText)
        {
            Debug.LogWarning("canJumpText: no text component assigned (uiText or tmpText).");
        }
    }
}
