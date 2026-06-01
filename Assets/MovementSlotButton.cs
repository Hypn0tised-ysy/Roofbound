using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MovementSlotButton : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private GameObject border;
    [SerializeField] private TextAsset skillsCatalog;
    [SerializeField] private Transform descriptionRoot;

    private const string NoneValue = "None";

    private TMP_InputField titleInputField;
    private TMP_InputField descriptionInputField;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private Text titleLegacyText;
    private Text descriptionLegacyText;

    private void Awake()
    {
        if (border == null)
        {
            Transform child = transform.Find("Border");
            if (child != null)
            {
                border = child.gameObject;
            }
        }
    }

    private void OnEnable()
    {
        RegisterSkill();
        RefreshFromSavedSelection();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ApplySelectionToggle();
        UpdateDescriptionUI();
    }

    private void RegisterSkill()
    {
        string skillName = gameObject.name;
        SkillSelectionStore.RegisterSkill(skillName, true);
    }

    private void UpdateDescriptionUI()
    {
        EnsureDescriptionTargets();
        string skillName = gameObject.name;
        string description = ResolveDescription(skillName);

        SetTextValue(titleInputField, titleText, titleLegacyText, skillName);
        SetTextValue(descriptionInputField, descriptionText, descriptionLegacyText, description);
    }

    private void ApplySelectionToggle()
    {
        SkillSelectionData data = SkillSelectionStore.Load();
        string skillName = gameObject.name;

        bool isCurrentlySelected = SkillSelectionStore.IsMovementSelected(data.movementSkillId, skillName);
        data.movementSkillId = isCurrentlySelected ? NoneValue : skillName;

        SkillSelectionStore.Save(data);
        RefreshAllSlots();
    }

    public void RefreshFromSavedSelection()
    {
        SkillSelectionData data = SkillSelectionStore.Load();
        string skillName = gameObject.name;

        bool isSelected = SkillSelectionStore.IsMovementSelected(data.movementSkillId, skillName);

        SetBorderActive(isSelected);
    }

    private void RefreshAllSlots()
    {
        MovementSlotButton[] slots = FindObjectsOfType<MovementSlotButton>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].RefreshFromSavedSelection();
        }
    }

    private void SetBorderActive(bool isActive)
    {
        if (border != null)
        {
            border.SetActive(isActive);
        }
    }

    private void EnsureDescriptionTargets()
    {
        if (descriptionRoot == null)
        {
            GameObject root = GameObject.Find("Description");
            if (root != null)
            {
                descriptionRoot = root.transform;
            }
        }

        if (descriptionRoot == null)
        {
            return;
        }

        if (titleInputField == null && titleText == null && titleLegacyText == null)
        {
            Transform titleTransform = descriptionRoot.Find("Title");
            CacheTextTargets(titleTransform, ref titleInputField, ref titleText, ref titleLegacyText);
        }

        if (descriptionInputField == null && descriptionText == null && descriptionLegacyText == null)
        {
            Transform descTransform = descriptionRoot.Find("DescriptionText");
            CacheTextTargets(descTransform, ref descriptionInputField, ref descriptionText, ref descriptionLegacyText);
        }
    }

    private void CacheTextTargets(Transform target,
        ref TMP_InputField inputField,
        ref TMP_Text tmpText,
        ref Text legacyText)
    {
        if (target == null)
        {
            return;
        }

        inputField = target.GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            return;
        }

        tmpText = target.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            return;
        }

        legacyText = target.GetComponent<Text>();
    }

    private void SetTextValue(TMP_InputField inputField, TMP_Text tmpText, Text legacyText, string value)
    {
        if (inputField != null)
        {
            inputField.text = value;
            return;
        }

        if (tmpText != null)
        {
            tmpText.text = value;
            return;
        }

        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }

    private string ResolveDescription(string skillName)
    {
        return SkillSelectionStore.TryGetDescription(skillsCatalog, skillName, true, out string description)
            ? description
            : string.Empty;
    }
}
