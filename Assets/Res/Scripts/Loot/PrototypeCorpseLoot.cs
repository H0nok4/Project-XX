using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PrototypeCorpseLoot : MonoBehaviour
{
    [Serializable]
    public sealed class WeaponEntry
    {
        [SerializeField] private PrototypeWeaponDefinition weaponDefinition;
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;
        [Min(0)]
        [SerializeField] private int magazineAmmo;
        [Min(0f)]
        [SerializeField] private float durability = 1f;
        [SerializeField] private List<ItemAffix> affixes = new List<ItemAffix>();
        [SerializeField] private List<ItemSkill> skills = new List<ItemSkill>();

        public PrototypeWeaponDefinition WeaponDefinition => weaponDefinition;
        public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
        public float Durability => Mathf.Max(0f, durability);
        public int MagazineAmmo => weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
            ? Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize)
            : 0;

        public IReadOnlyList<ItemAffix> Affixes => affixes;
        public IReadOnlyList<ItemSkill> Skills => skills;

        public void Configure(
            PrototypeWeaponDefinition definition,
            int loadedAmmo,
            float startingDurability = 1f,
            ItemRarity itemRarity = ItemRarity.Common,
            IReadOnlyList<ItemAffix> affixesOverride = null,
            IReadOnlyList<ItemSkill> skillsOverride = null)
        {
            weaponDefinition = definition;
            rarity = ItemRarityUtility.Sanitize(itemRarity);
            magazineAmmo = definition != null && !definition.IsMeleeWeapon
                ? Mathf.Clamp(loadedAmmo, 0, definition.MagazineSize)
                : 0;
            durability = Mathf.Max(0f, startingDurability);
            affixes = ItemAffixUtility.CloneList(affixesOverride);
            ItemAffixUtility.SanitizeAffixes(affixes);
            skills = ItemSkillUtility.CloneList(skillsOverride);
            ItemSkillUtility.SanitizeSkills(skills);
        }

        public void Configure(ItemInstance instance)
        {
            Configure(
                instance != null ? instance.WeaponDefinition : null,
                instance != null ? instance.MagazineAmmo : 0,
                instance != null ? instance.CurrentDurability : 1f,
                instance != null ? instance.Rarity : ItemRarity.Common,
                instance != null ? instance.Affixes : null,
                instance != null ? instance.Skills : null);
        }

        public void Configure(WeaponInstance instance)
        {
            Configure(ItemInstance.Create(instance));
        }

        public void Sanitize()
        {
            if (affixes == null)
            {
                affixes = new List<ItemAffix>();
            }

            if (skills == null)
            {
                skills = new List<ItemSkill>();
            }

            ItemAffixUtility.SanitizeAffixes(affixes);
            ItemSkillUtility.SanitizeSkills(skills);
            rarity = ItemRarityUtility.Sanitize(rarity);
            if (weaponDefinition != null && !weaponDefinition.IsMeleeWeapon)
            {
                magazineAmmo = Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize);
            }
            else
            {
                magazineAmmo = 0;
            }

            durability = Mathf.Max(0f, durability);
        }

        public ItemInstance CreateInstance()
        {
            return weaponDefinition != null
                ? ItemInstance.Create(weaponDefinition, MagazineAmmo, Durability, null, Rarity, affixes, false, skills, false)
                : null;
        }
    }

    [SerializeField] private string corpseLabel = "Corpse";
    [SerializeField] private List<WeaponEntry> weapons = new List<WeaponEntry>();

    public string CorpseLabel => string.IsNullOrWhiteSpace(corpseLabel) ? gameObject.name : corpseLabel.Trim();
    public IReadOnlyList<WeaponEntry> Weapons => weapons;
    public bool HasWeapons => weapons != null && weapons.Count > 0;
    public bool HasAnyLoot
    {
        get
        {
            InventoryContainer inventory = GetComponent<InventoryContainer>();
            return HasWeapons || (inventory != null && !inventory.IsEmpty);
        }
    }

    private void OnValidate()
    {
        corpseLabel = string.IsNullOrWhiteSpace(corpseLabel) ? gameObject.name : corpseLabel.Trim();
        SanitizeWeapons();
    }

    public void Configure(string label)
    {
        corpseLabel = string.IsNullOrWhiteSpace(label) ? gameObject.name : label.Trim();
        SanitizeWeapons();
    }

    public void AddWeapon(PrototypeWeaponDefinition weaponDefinition, int loadedAmmo, float durability = 1f, ItemRarity rarity = ItemRarity.Common)
    {
        if (weaponDefinition == null)
        {
            return;
        }

        ItemInstance instance = ItemInstance.Create(weaponDefinition, loadedAmmo, durability, null, rarity);
        AddWeapon(instance);
    }

    public void AddWeapon(ItemInstance instance)
    {
        if (instance == null || !instance.IsWeapon || instance.WeaponDefinition == null)
        {
            return;
        }

        weapons ??= new List<WeaponEntry>();
        var entry = new WeaponEntry();
        entry.Configure(instance);
        entry.Sanitize();
        weapons.Add(entry);
    }

    public void AddWeapon(WeaponInstance instance)
    {
        AddWeapon(ItemInstance.Create(instance));
    }

    public WeaponEntry GetWeaponEntry(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Count)
        {
            return null;
        }

        return weapons[index];
    }

    public bool TryTakeWeapon(PlayerInteractor interactor, int index)
    {
        if (interactor == null || weapons == null || index < 0 || index >= weapons.Count)
        {
            return false;
        }

        PrototypeFpsController controller = interactor.GetComponent<PrototypeFpsController>();
        WeaponEntry entry = weapons[index];
        if (controller == null || entry == null || entry.WeaponDefinition == null)
        {
            return false;
        }

        ItemInstance incomingWeapon = entry.CreateInstance();
        if (!controller.TryEquipLootedWeapon(incomingWeapon, out ItemInstance replacedWeapon))
        {
            return false;
        }

        weapons.RemoveAt(index);
        if (replacedWeapon != null)
        {
            AddWeapon(replacedWeapon);
        }

        PrototypeEquippedWeaponVisual equippedWeaponVisual = GetComponent<PrototypeEquippedWeaponVisual>();
        if (equippedWeaponVisual != null)
        {
            PrototypeWeaponDefinition visibleWeapon = weapons != null && weapons.Count > 0
                ? weapons[0].WeaponDefinition
                : null;
            equippedWeaponVisual.SetEquippedWeapon(visibleWeapon);
        }

        return true;
    }

    public bool TryExtractWeapon(int index, out ItemInstance extractedWeapon)
    {
        extractedWeapon = null;
        if (weapons == null || index < 0 || index >= weapons.Count)
        {
            return false;
        }

        WeaponEntry entry = weapons[index];
        extractedWeapon = entry != null ? entry.CreateInstance() : null;
        if (extractedWeapon == null)
        {
            return false;
        }

        weapons.RemoveAt(index);
        RefreshEquippedWeaponVisual();
        return true;
    }

    private void SanitizeWeapons()
    {
        if (weapons == null)
        {
            weapons = new List<WeaponEntry>();
            return;
        }

        for (int index = weapons.Count - 1; index >= 0; index--)
        {
            WeaponEntry entry = weapons[index];
            if (entry == null || entry.WeaponDefinition == null)
            {
                weapons.RemoveAt(index);
                continue;
            }

            entry.Sanitize();
        }
    }

    private void RefreshEquippedWeaponVisual()
    {
        PrototypeEquippedWeaponVisual equippedWeaponVisual = GetComponent<PrototypeEquippedWeaponVisual>();
        if (equippedWeaponVisual != null)
        {
            PrototypeWeaponDefinition visibleWeapon = weapons != null && weapons.Count > 0
                ? weapons[0].WeaponDefinition
                : null;
            equippedWeaponVisual.SetEquippedWeapon(visibleWeapon);
        }
    }
}
