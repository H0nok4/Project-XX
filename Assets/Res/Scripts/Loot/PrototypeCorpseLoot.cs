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

        public PrototypeWeaponDefinition WeaponDefinition => weaponDefinition;
        public ItemRarity Rarity => ItemRarityUtility.Sanitize(rarity);
        public float Durability => Mathf.Max(0f, durability);
        public int MagazineAmmo => weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
            ? Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize)
            : 0;

        public void Configure(PrototypeWeaponDefinition definition, int loadedAmmo, float startingDurability = 1f, ItemRarity itemRarity = ItemRarity.Common)
        {
            weaponDefinition = definition;
            rarity = ItemRarityUtility.Sanitize(itemRarity);
            magazineAmmo = definition != null && !definition.IsMeleeWeapon
                ? Mathf.Clamp(loadedAmmo, 0, definition.MagazineSize)
                : 0;
            durability = Mathf.Max(0f, startingDurability);
        }

        public void Configure(WeaponInstance instance)
        {
            Configure(
                instance != null ? instance.Definition : null,
                instance != null ? instance.MagazineAmmo : 0,
                instance != null ? instance.Durability : 1f,
                instance != null ? instance.Rarity : ItemRarity.Common);
        }

        public WeaponInstance CreateInstance()
        {
            return weaponDefinition != null
                ? WeaponInstance.Create(weaponDefinition, MagazineAmmo, Durability, null, Rarity)
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

        weapons ??= new List<WeaponEntry>();

        var entry = new WeaponEntry();
        entry.Configure(weaponDefinition, loadedAmmo, durability, rarity);
        weapons.Add(entry);
    }

    public void AddWeapon(WeaponInstance instance)
    {
        if (instance == null || instance.Definition == null)
        {
            return;
        }

        weapons ??= new List<WeaponEntry>();
        var entry = new WeaponEntry();
        entry.Configure(instance);
        weapons.Add(entry);
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

        WeaponInstance incomingWeapon = entry.CreateInstance();
        if (!controller.TryEquipLootedWeapon(incomingWeapon, out WeaponInstance replacedWeapon))
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
            }
        }
    }
}
