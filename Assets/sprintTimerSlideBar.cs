using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class sprintTimerSlideBar : MonoBehaviour
{
    [Tooltip("Optional reference to PlayerControl. If null, will FindObjectOfType on Start.")]
    public playerControl player;

    [Tooltip("Speed to smooth the visual change (units/sec). Set 0 for immediate.")]
    public float smoothSpeed = 8f;

    [SerializeField]
    float visualFill = 0f;

    [SerializeField]
    float sprintCoolDown = 0f;

    [SerializeField]
    float remainingSprintCoolDown = 0f;

    Slider slider;

    player_config player_config;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<playerControl>();

        player_config = player.configAsset;
        if (player_config == null)
        {
            Debug.LogError("sprintTimerSlideBar: player_config is not assigned.");
        }
        else
        {
            sprintCoolDown = player_config.sprintCooldown;
            remainingSprintCoolDown = 0f;
        }

        slider = GetComponent<Slider>();
        if (slider == null)
        {
            Debug.LogError("sprintTimerSlideBar: slider is not assigned.");
        }

    }

    void Update()
    {
        if (player == null)
            return;

        remainingSprintCoolDown = player.SprintCooldownRemaining;

        float target = 1f;
        target = 1f - Mathf.Clamp01(remainingSprintCoolDown / sprintCoolDown);

        if (smoothSpeed > 0f)
            visualFill = Mathf.MoveTowards(visualFill, target, smoothSpeed * Time.deltaTime);
        else
            visualFill = target;


        slider.value = visualFill;
    }
}
