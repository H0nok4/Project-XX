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

    [Serializable]
    public sealed class MerchantDefinition
    {
        public string merchantId = "merchant";
        public string displayName = "Merchant";
        public List<ItemOffer> itemOffers = new List<ItemOffer>();
        public List<WeaponOffer> weaponOffers = new List<WeaponOffer>();
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
            if (merchant?.itemOffers == null)
            {
                continue;
            }

            for (int offerIndex = 0; offerIndex < merchant.itemOffers.Count; offerIndex++)
            {
                ItemOffer offer = merchant.itemOffers[offerIndex];
                if (offer?.definition != definition || offer.quantity <= 0 || offer.price <= 0)
                {
                    continue;
                }

                unitPrice = (float)offer.price / offer.quantity;
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
            if (merchant?.weaponOffers == null)
            {
                continue;
            }

            for (int offerIndex = 0; offerIndex < merchant.weaponOffers.Count; offerIndex++)
            {
                WeaponOffer offer = merchant.weaponOffers[offerIndex];
                if (offer?.definition != definition || offer.price <= 0)
                {
                    continue;
                }

                price = offer.price;
                return true;
            }
        }

        return false;
    }
}
