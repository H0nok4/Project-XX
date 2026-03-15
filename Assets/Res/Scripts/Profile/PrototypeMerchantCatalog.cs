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

        public bool IsValid => definition != null && quantity > 0 && price > 0;
    }

    [Serializable]
    public sealed class WeaponOffer
    {
        public PrototypeWeaponDefinition definition;
        [Min(1)]
        public int price = 1;

        public bool IsValid => definition != null && price > 0;
    }

    [Serializable]
    public sealed class RuntimeOffer
    {
        public string offerId = string.Empty;
        public ItemInstance itemInstance;
        [Min(1)]
        public int price = 1;

        public bool IsValid => itemInstance != null && itemInstance.IsDefined() && price > 0;
    }

    public readonly struct MerchantOfferView
    {
        public MerchantOfferView(RuntimeOffer runtimeOffer)
        {
            RuntimeOffer = runtimeOffer;
        }

        public RuntimeOffer RuntimeOffer { get; }
        public ItemInstance ItemInstance => RuntimeOffer != null ? RuntimeOffer.itemInstance : null;
        public string OfferId => RuntimeOffer != null ? RuntimeOffer.offerId : string.Empty;
        public ItemDefinitionBase DefinitionBase => ItemInstance != null ? ItemInstance.DefinitionBase : null;
        public bool IsWeapon => ItemInstance != null && ItemInstance.IsWeapon;
        public int Quantity => ItemInstance != null ? ItemInstance.Quantity : 0;
        public int Price => RuntimeOffer != null ? Mathf.Max(0, RuntimeOffer.price) : 0;
        public bool IsValid => RuntimeOffer != null && RuntimeOffer.IsValid;
    }

    [Serializable]
    public sealed class MerchantDefinition
    {
        public string merchantId = "merchant";
        public string displayName = "Merchant";
        [Range(1, 5)]
        public int merchantLevel = 1;
        public List<ItemOffer> itemOffers = new List<ItemOffer>();
        public List<WeaponOffer> weaponOffers = new List<WeaponOffer>();

        [NonSerialized] private List<RuntimeOffer> runtimeOffers = new List<RuntimeOffer>();

        public string MerchantId => string.IsNullOrWhiteSpace(merchantId) ? "merchant" : merchantId.Trim();
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? MerchantId : displayName.Trim();
        public int MerchantLevel => Mathf.Clamp(merchantLevel, 1, 5);
        public bool HasRuntimeOffers => runtimeOffers != null && runtimeOffers.Count > 0;
        public IReadOnlyList<RuntimeOffer> RuntimeOffers => runtimeOffers;

        public IEnumerable<MerchantOfferView> EnumerateOffers()
        {
            if (runtimeOffers == null)
            {
                yield break;
            }

            for (int index = 0; index < runtimeOffers.Count; index++)
            {
                MerchantOfferView offer = new MerchantOfferView(runtimeOffers[index]);
                if (offer.IsValid)
                {
                    yield return offer;
                }
            }
        }

        public void SetRuntimeOffers(List<RuntimeOffer> offers)
        {
            runtimeOffers = offers ?? new List<RuntimeOffer>();
        }

        public void ClearRuntimeOffers()
        {
            runtimeOffers = new List<RuntimeOffer>();
        }

        public void Sanitize()
        {
            merchantId = MerchantId;
            displayName = DisplayName;
            merchantLevel = MerchantLevel;
            SanitizeItemOffers(itemOffers);
            SanitizeWeaponOffers(weaponOffers);
        }

        private static void SanitizeItemOffers(List<ItemOffer> offers)
        {
            if (offers == null)
            {
                return;
            }

            for (int index = offers.Count - 1; index >= 0; index--)
            {
                ItemOffer offer = offers[index];
                if (offer == null || !offer.IsValid)
                {
                    offers.RemoveAt(index);
                    continue;
                }

                offer.quantity = Mathf.Max(1, offer.quantity);
                offer.price = Mathf.Max(1, offer.price);
            }
        }

        private static void SanitizeWeaponOffers(List<WeaponOffer> offers)
        {
            if (offers == null)
            {
                return;
            }

            for (int index = offers.Count - 1; index >= 0; index--)
            {
                WeaponOffer offer = offers[index];
                if (offer == null || !offer.IsValid)
                {
                    offers.RemoveAt(index);
                    continue;
                }

                offer.price = Mathf.Max(1, offer.price);
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

            source.Sanitize();
            source.ClearRuntimeOffers();
            merchants.Add(source);
        }
    }

    public void EnsureRuntimeInventories()
    {
        EnsureSanitized();
        if (merchants == null)
        {
            return;
        }

        for (int index = 0; index < merchants.Count; index++)
        {
            MerchantDefinition merchant = merchants[index];
            if (merchant == null)
            {
                continue;
            }

            if (!merchant.HasRuntimeOffers)
            {
                RegenerateMerchantInventory(merchant);
            }
        }
    }

    public void RegenerateRuntimeInventories()
    {
        EnsureSanitized();
        if (merchants == null)
        {
            return;
        }

        for (int index = 0; index < merchants.Count; index++)
        {
            MerchantDefinition merchant = merchants[index];
            if (merchant != null)
            {
                RegenerateMerchantInventory(merchant);
            }
        }
    }

    public void RegenerateMerchantInventory(MerchantDefinition merchant)
    {
        if (merchant == null)
        {
            return;
        }

        merchant.Sanitize();
        var runtimeOffers = new List<RuntimeOffer>();
        int nextOfferIndex = 0;

        if (merchant.weaponOffers != null)
        {
            for (int index = 0; index < merchant.weaponOffers.Count; index++)
            {
                WeaponOffer offer = merchant.weaponOffers[index];
                if (offer == null || !offer.IsValid)
                {
                    continue;
                }

                RuntimeOffer runtimeOffer = CreateRuntimeOffer(merchant, offer.definition, 1, offer.price, nextOfferIndex++);
                if (runtimeOffer != null && runtimeOffer.IsValid)
                {
                    runtimeOffers.Add(runtimeOffer);
                }
            }
        }

        if (merchant.itemOffers != null)
        {
            for (int index = 0; index < merchant.itemOffers.Count; index++)
            {
                ItemOffer offer = merchant.itemOffers[index];
                if (offer == null || !offer.IsValid)
                {
                    continue;
                }

                int quantity = offer.definition is ArmorDefinition ? 1 : Mathf.Max(1, offer.quantity);
                RuntimeOffer runtimeOffer = CreateRuntimeOffer(merchant, offer.definition, quantity, offer.price, nextOfferIndex++);
                if (runtimeOffer != null && runtimeOffer.IsValid)
                {
                    runtimeOffers.Add(runtimeOffer);
                }
            }
        }

        merchant.SetRuntimeOffers(runtimeOffers);
    }

    public int GetBuyPrice(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined())
        {
            return 0;
        }

        float baseReferencePrice = ResolveBaseReferencePrice(instance);
        return Mathf.Max(1, Mathf.RoundToInt(CalculateInstancePriceRaw(instance, baseReferencePrice)));
    }

    public int GetSellPrice(ItemDefinition definition, int quantity = 1)
    {
        if (definition == null || quantity <= 0)
        {
            return 0;
        }

        ItemInstance previewInstance = ItemInstance.Create(
            definition,
            quantity,
            ItemRarity.Common,
            null,
            null,
            false,
            null,
            false);
        return GetSellPrice(previewInstance);
    }

    public int GetSellPrice(PrototypeWeaponDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        ItemInstance previewInstance = ItemInstance.Create(
            definition,
            definition.IsMeleeWeapon ? 0 : definition.MagazineSize,
            1f,
            null,
            ItemRarity.Common,
            null,
            false,
            null,
            false);
        return GetSellPrice(previewInstance);
    }

    public int GetSellPrice(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined())
        {
            return 0;
        }

        float baseReferencePrice = ResolveBaseReferencePrice(instance);
        float sellbackMultiplier = instance.IsWeapon
            ? weaponSellbackMultiplier
            : itemSellbackMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(CalculateInstancePriceRaw(instance, baseReferencePrice) * sellbackMultiplier));
    }

    private void OnEnable()
    {
        EnsureSanitized();
    }

    private void OnValidate()
    {
        EnsureSanitized();
    }

    private void EnsureSanitized()
    {
        itemSellbackMultiplier = Mathf.Clamp(itemSellbackMultiplier, 0.05f, 1f);
        weaponSellbackMultiplier = Mathf.Clamp(weaponSellbackMultiplier, 0.05f, 1f);

        if (merchants == null)
        {
            merchants = new List<MerchantDefinition>();
            return;
        }

        var seenMerchantIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = merchants.Count - 1; index >= 0; index--)
        {
            MerchantDefinition merchant = merchants[index];
            if (merchant == null)
            {
                merchants.RemoveAt(index);
                continue;
            }

            merchant.Sanitize();
            if (!seenMerchantIds.Add(merchant.MerchantId))
            {
                merchants.RemoveAt(index);
                continue;
            }

            merchant.ClearRuntimeOffers();
        }
    }

    private RuntimeOffer CreateRuntimeOffer(
        MerchantDefinition merchant,
        ItemDefinitionBase definition,
        int quantity,
        int basePrice,
        int offerIndex)
    {
        ItemInstance templateInstance = CreateMerchantItemInstance(definition, quantity, merchant != null ? merchant.MerchantLevel : 1);
        if (templateInstance == null || !templateInstance.IsDefined())
        {
            return null;
        }

        string offerSuffix = definition != null ? definition.ItemId : $"offer_{offerIndex + 1}";
        return new RuntimeOffer
        {
            offerId = $"{merchant?.MerchantId ?? "merchant"}_{offerSuffix}_{offerIndex + 1}",
            itemInstance = templateInstance,
            price = Mathf.Max(1, Mathf.RoundToInt(CalculateInstancePriceRaw(templateInstance, Mathf.Max(1f, basePrice))))
        };
    }

    private static ItemInstance CreateMerchantItemInstance(ItemDefinitionBase definition, int quantity, int merchantLevel)
    {
        if (definition == null)
        {
            return null;
        }

        if (definition is PrototypeWeaponDefinition || definition is ArmorDefinition)
        {
            return ItemInstance.Create(
                definition,
                1,
                RollMerchantRarity(merchantLevel),
                null,
                null,
                true,
                null,
                true);
        }

        return ItemInstance.Create(
            definition,
            Mathf.Max(1, quantity),
            ItemRarity.Common,
            null,
            null,
            false,
            null,
            false);
    }

    private float ResolveBaseReferencePrice(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined())
        {
            return 0f;
        }

        if (instance.IsWeapon && instance.WeaponDefinition != null && TryGetWeaponPrice(instance.WeaponDefinition, out int weaponPrice))
        {
            return Mathf.Max(1, weaponPrice);
        }

        if (instance.Definition != null && TryGetItemUnitPrice(instance.Definition, out float unitPrice))
        {
            return Mathf.Max(1f, unitPrice * Mathf.Max(1, instance.Quantity));
        }

        return EstimateBaseReferencePrice(instance);
    }

    private float EstimateBaseReferencePrice(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined() || instance.DefinitionBase == null)
        {
            return 0f;
        }

        float unitPrice = EstimateUnitBasePrice(instance.DefinitionBase);
        return Mathf.Max(1f, unitPrice * Mathf.Max(1, instance.Quantity));
    }

    private static float EstimateUnitBasePrice(ItemDefinitionBase definition)
    {
        if (definition == null)
        {
            return 1f;
        }

        float basePrice = Mathf.Max(1f, definition.UnitWeight * 10f + definition.ItemLevel * 1.5f);
        if (definition is PrototypeWeaponDefinition weaponDefinition)
        {
            return weaponDefinition.IsMeleeWeapon
                ? Mathf.Max(10f, basePrice + weaponDefinition.MeleeDamage * 0.18f + weaponDefinition.PenetrationPower * 0.12f)
                : Mathf.Max(16f, basePrice + weaponDefinition.EffectiveRange * 0.08f + weaponDefinition.PenetrationPower * 0.18f + weaponDefinition.RoundsPerMinute * 0.01f);
        }

        if (definition is ArmorDefinition armorDefinition)
        {
            return Mathf.Max(12f, basePrice + armorDefinition.ArmorClass * 4f + armorDefinition.MaxDurability * 0.12f);
        }

        if (definition is MedicalItemDefinition medicalDefinition)
        {
            return Mathf.Max(4f,
                basePrice
                + medicalDefinition.HealAmount * 0.08f
                + medicalDefinition.RemovesLightBleeds * 1.5f
                + medicalDefinition.RemovesHeavyBleeds * 2.5f
                + medicalDefinition.CuresFractures * 3f
                + medicalDefinition.PainkillerDuration * 0.02f);
        }

        if (definition is AmmoDefinition ammoDefinition)
        {
            return Mathf.Max(1f, basePrice + ammoDefinition.DirectDamage * 0.12f + ammoDefinition.PenetrationPower * 0.08f + ammoDefinition.ArmorDamage * 0.05f);
        }

        return basePrice;
    }

    private static float CalculateInstancePriceRaw(ItemInstance instance, float baseReferencePrice)
    {
        if (instance == null || !instance.IsDefined())
        {
            return 0f;
        }

        ItemDefinitionBase definition = instance.DefinitionBase;
        float computedPrice = Mathf.Max(1f, baseReferencePrice);
        int itemLevel = definition != null ? definition.ItemLevel : 1;
        bool isGear = instance.IsWeapon || instance.IsArmor;

        computedPrice *= 1f + Mathf.Max(0, itemLevel - 1) * (isGear ? 0.08f : 0.04f);
        if (isGear)
        {
            computedPrice *= GetRarityPriceMultiplier(instance.Rarity);
            computedPrice *= 1f + (instance.Affixes != null ? instance.Affixes.Count : 0) * 0.18f;
            computedPrice *= 1f + (instance.Skills != null ? instance.Skills.Count : 0) * 0.45f;
            computedPrice *= GetConditionPriceMultiplier(instance);
        }

        return Mathf.Max(1f, computedPrice);
    }

    private static float GetConditionPriceMultiplier(ItemInstance instance)
    {
        if (instance == null || !instance.IsDefined())
        {
            return 1f;
        }

        if (instance.IsArmor && instance.Definition is ArmorDefinition armorDefinition)
        {
            float durabilityRatio = armorDefinition.MaxDurability > 0f
                ? Mathf.Clamp01(instance.CurrentDurability / armorDefinition.MaxDurability)
                : 1f;
            return Mathf.Lerp(0.45f, 1f, durabilityRatio);
        }

        if (instance.IsWeapon)
        {
            float durabilityRatio = Mathf.Clamp01(instance.CurrentDurability);
            return Mathf.Lerp(0.65f, 1f, durabilityRatio);
        }

        return 1f;
    }

    private static float GetRarityPriceMultiplier(ItemRarity rarity)
    {
        switch (ItemRarityUtility.Sanitize(rarity))
        {
            case ItemRarity.Uncommon:
                return 1.3f;

            case ItemRarity.Rare:
                return 1.75f;

            case ItemRarity.Epic:
                return 2.4f;

            case ItemRarity.Legendary:
                return 3.4f;

            default:
                return 1f;
        }
    }

    private static ItemRarity RollMerchantRarity(int merchantLevel)
    {
        switch (Mathf.Clamp(merchantLevel, 1, 5))
        {
            case 2:
                return ItemRarityUtility.RollWeighted(72f, 28f, 0f, 0f, 0f);

            case 3:
                return ItemRarityUtility.RollWeighted(0f, 62f, 38f, 0f, 0f);

            case 4:
                return ItemRarityUtility.RollWeighted(0f, 0f, 72f, 28f, 0f);

            case 5:
                return ItemRarityUtility.RollWeighted(0f, 0f, 0f, 78f, 22f);

            default:
                return ItemRarity.Common;
        }
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
                if (offer == null || !offer.IsValid || offer.definition != definition)
                {
                    continue;
                }

                unitPrice = (float)offer.price / Mathf.Max(1, offer.quantity);
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
                if (offer == null || !offer.IsValid || offer.definition != definition)
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
