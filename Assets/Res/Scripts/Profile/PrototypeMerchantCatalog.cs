using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Profile/Merchant Catalog", fileName = "PrototypeMerchantCatalog")]
public class PrototypeMerchantCatalog : ScriptableObject
{
    [Serializable]
    public sealed class ItemOffer
    {
        public ItemDefinition definition;
        [Min(1)]
        public int quantity = 1;
        [Min(1)]
        public int price = 1;
    }

    [Serializable]
    public sealed class WeaponOffer
    {
        public PrototypeWeaponDefinition definition;
        [Min(1)]
        public int price = 1;
    }

    public readonly struct MerchantOfferView
    {
        public MerchantOfferView(ItemOffer itemOffer)
        {
            ItemOffer = itemOffer;
            WeaponOffer = null;
        }

        public MerchantOfferView(WeaponOffer weaponOffer)
        {
            ItemOffer = null;
            WeaponOffer = weaponOffer;
        }

        public ItemOffer ItemOffer { get; }
        public WeaponOffer WeaponOffer { get; }
        public ItemDefinitionBase DefinitionBase => WeaponOffer != null
            ? (ItemDefinitionBase)WeaponOffer.definition
            : ItemOffer != null ? ItemOffer.definition : null;
        public bool IsWeapon => DefinitionBase is PrototypeWeaponDefinition;
        public ItemDefinition ItemDefinition => ItemOffer != null ? ItemOffer.definition : null;
        public PrototypeWeaponDefinition WeaponDefinition => WeaponOffer != null ? WeaponOffer.definition : null;
        public int Quantity => ItemOffer != null ? Mathf.Max(1, ItemOffer.quantity) : 1;
        public int Price => ItemOffer != null
            ? Mathf.Max(0, ItemOffer.price)
            : WeaponOffer != null
                ? Mathf.Max(0, WeaponOffer.price)
                : 0;
        public bool IsValid => Price > 0 && DefinitionBase != null;
    }

    [Serializable]
    public sealed class MerchantDefinition
    {
        public string merchantId = "merchant";
        public string displayName = "Merchant";
        public List<ItemOffer> itemOffers = new List<ItemOffer>();
        public List<WeaponOffer> weaponOffers = new List<WeaponOffer>();

        public IEnumerable<MerchantOfferView> EnumerateOffers()
        {
            if (weaponOffers != null)
            {
                for (int index = 0; index < weaponOffers.Count; index++)
                {
                    MerchantOfferView offer = new MerchantOfferView(weaponOffers[index]);
                    if (offer.IsValid)
                    {
                        yield return offer;
                    }
                }
            }

            if (itemOffers != null)
            {
                for (int index = 0; index < itemOffers.Count; index++)
                {
                    MerchantOfferView offer = new MerchantOfferView(itemOffers[index]);
                    if (offer.IsValid)
                    {
                        yield return offer;
                    }
                }
            }
        }
    }

    [SerializeField] private List<MerchantDefinition> merchants = new List<MerchantDefinition>();
    [Range(0.05f, 1f)]
    [SerializeField] private float itemSellbackMultiplier = 0.5f;
    [Range(0.05f, 1f)]
    [SerializeField] private float weaponSellbackMultiplier = 0.55f;

    public IReadOnlyList<MerchantDefinition> Merchants => merchants;

    public void Configure(float itemSellback, float weaponSellback, params MerchantDefinition[] merchantDefinitions)
    {
        itemSellbackMultiplier = Mathf.Clamp(itemSellback, 0.05f, 1f);
        weaponSellbackMultiplier = Mathf.Clamp(weaponSellback, 0.05f, 1f);
        merchants = new List<MerchantDefinition>();

        if (merchantDefinitions == null)
        {
            return;
        }

        for (int index = 0; index < merchantDefinitions.Length; index++)
        {
            MerchantDefinition source = merchantDefinitions[index];
            if (source == null)
            {
                continue;
            }

            merchants.Add(source);
        }
    }

    public int GetSellPrice(ItemDefinition definition, int quantity = 1)
    {
        if (definition == null || quantity <= 0)
        {
            return 0;
        }

        if (TryGetItemUnitPrice(definition, out float unitPrice))
        {
            return Mathf.Max(1, Mathf.RoundToInt(unitPrice * itemSellbackMultiplier * quantity));
        }

        float fallbackPrice = Mathf.Max(1f, definition.UnitWeight * 10f);
        return Mathf.Max(1, Mathf.RoundToInt(fallbackPrice * quantity));
    }

    public int GetSellPrice(PrototypeWeaponDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        if (TryGetWeaponPrice(definition, out int offerPrice))
        {
            return Mathf.Max(1, Mathf.RoundToInt(offerPrice * weaponSellbackMultiplier));
        }

        return definition.IsMeleeWeapon ? 10 : 18;
    }

    public int GetSellPrice(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined())
        {
            return 0;
        }

        return instance.IsWeapon
            ? GetSellPrice(instance.WeaponDefinition)
            : GetSellPrice(instance.Definition, instance.Quantity);
    }

    private bool TryGetItemUnitPrice(ItemDefinition definition, out float unitPrice)
    {
        unitPrice = 0f;
        if (definition == null || merchants == null)
        {
            return false;
        }

        for (int merchantIndex = 0; merchantIndex < merchants.Count; merchantIndex++)
        {
            MerchantDefinition merchant = merchants[merchantIndex];
            if (merchant == null)
            {
                continue;
            }

            foreach (MerchantOfferView offer in merchant.EnumerateOffers())
            {
                ItemDefinition itemDefinition = offer.DefinitionBase as ItemDefinition;
                if (itemDefinition == null || itemDefinition != definition || offer.Quantity <= 0)
                {
                    continue;
                }

                unitPrice = (float)offer.Price / offer.Quantity;
                return true;
            }
        }

        return false;
    }

    private bool TryGetWeaponPrice(PrototypeWeaponDefinition definition, out int price)
    {
        price = 0;
        if (definition == null || merchants == null)
        {
            return false;
        }

        for (int merchantIndex = 0; merchantIndex < merchants.Count; merchantIndex++)
        {
            MerchantDefinition merchant = merchants[merchantIndex];
            if (merchant == null)
            {
                continue;
            }

            foreach (MerchantOfferView offer in merchant.EnumerateOffers())
            {
                PrototypeWeaponDefinition weaponDefinition = offer.DefinitionBase as PrototypeWeaponDefinition;
                if (weaponDefinition == null || weaponDefinition != definition)
                {
                    continue;
                }

                price = offer.Price;
                return true;
            }
        }

        return false;
    }
}
