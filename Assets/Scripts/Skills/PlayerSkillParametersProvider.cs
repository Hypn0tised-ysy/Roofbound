using UnityEngine;

public class PlayerSkillParametersProvider : MonoBehaviour
{
    [SerializeField] private PlayerSkillParameters parameters;

    public PlayerSkillParameters Parameters => parameters;
}
