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
        [Min(0)]
        [SerializeField] private int magazineAmmo;

        public PrototypeWeaponDefinition WeaponDefinition => weaponDefinition;
        public int MagazineAmmo => weaponDefinition != null && !weaponDefinition.IsMeleeWeapon
            ? Mathf.Clamp(magazineAmmo, 0, weaponDefinition.MagazineSize)
            : 0;

        public void Configure(PrototypeWeaponDefinition definition, int loadedAmmo)
        {
            weaponDefinition = definition;
            magazineAmmo = definition != null && !definition.IsMeleeWeapon
                ? Mathf.Clamp(loadedAmmo, 0, definition.MagazineSize)
                : 0;
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

    public void AddWeapon(PrototypeWeaponDefinition weaponDefinition, int loadedAmmo)
    {
        if (weaponDefinition == null)
        {
            return;
        }

        weapons ??= new List<WeaponEntry>();

        var entry = new WeaponEntry();
        entry.Configure(weaponDefinition, loadedAmmo);
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

        if (!controller.TryEquipLootedWeapon(entry.WeaponDefinition, entry.MagazineAmmo, out PrototypeWeaponDefinition replacedWeapon, out int replacedAmmo))
        {
            return false;
        }

        weapons.RemoveAt(index);
        if (replacedWeapon != null)
        {
            AddWeapon(replacedWeapon, replacedAmmo);
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
