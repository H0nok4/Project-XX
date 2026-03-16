using System.Collections.Generic;
using UnityEngine;

public sealed class MetaMerchantPresenter
{
    private readonly PrototypeMainMenuController host;
    private Vector2 weaponMerchantScroll;
    private Vector2 medicalMerchantScroll;
    private Vector2 armorMerchantScroll;

    public MetaMerchantPresenter(PrototypeMainMenuController host)
    {
        this.host = host;
    }

    public void DrawMerchantsPage()
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null || merchantCatalog.Merchants == null || merchantCatalog.Merchants.Count == 0)
        {
            Rect emptyRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), 220f);
            host.BeginPanel(emptyRect, "商人", host.LockerColor, $"资金 {host.GetAvailableFunds()} {host.GetCurrencyLabel()}");
            GUILayout.Label("未配置商人目录。", host.BodyStyle);
            host.EndPanel();
            return;
        }

        float panelTop = 140f;
        float panelHeight = Mathf.Max(400f, Screen.height - 220f);
        float panelWidth = Mathf.Max(250f, (Screen.width - 356f) / 3f);
        float firstX = 292f;

        DrawMerchantPanel(new Rect(firstX, panelTop, panelWidth, panelHeight), 0, host.StashColor, ref weaponMerchantScroll);
        if (merchantCatalog.Merchants.Count > 1)
        {
            DrawMerchantPanel(new Rect(firstX + panelWidth + 16f, panelTop, panelWidth, panelHeight), 1, host.BackpackColor, ref medicalMerchantScroll);
        }

        if (merchantCatalog.Merchants.Count > 2)
        {
            DrawMerchantPanel(new Rect(firstX + (panelWidth + 16f) * 2f, panelTop, panelWidth, panelHeight), 2, host.ProtectedColor, ref armorMerchantScroll);
        }
    }

    private void DrawMerchantPanel(Rect rect, int merchantIndex, Color accent, ref Vector2 scroll)
    {
        PrototypeMerchantCatalog merchantCatalog = host.MerchantCatalog;
        if (merchantCatalog == null || merchantCatalog.Merchants == null || merchantIndex < 0 || merchantIndex >= merchantCatalog.Merchants.Count)
        {
            return;
        }

        PrototypeMerchantCatalog.MerchantDefinition merchant = merchantCatalog.Merchants[merchantIndex];
        if (merchant == null)
        {
            return;
        }

        host.BeginPanel(rect, merchant.DisplayName, accent, $"等级 {merchant.MerchantLevel}  资金 {host.GetAvailableFunds()} {host.GetCurrencyLabel()}");
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(rect.height - 130f));

        bool drewAnyOffer = false;
        foreach (PrototypeMerchantCatalog.MerchantOfferView offer in merchant.EnumerateOffers())
        {
            if (!offer.IsValid || offer.ItemInstance == null)
            {
                continue;
            }

            drewAnyOffer = true;
            DrawOfferEntry(offer);
        }

        if (!drewAnyOffer)
        {
            GUILayout.Label("当前没有可售商品。", host.BodyStyle);
        }

        GUILayout.EndScrollView();
        host.EndPanel();
    }

    private void DrawOfferEntry(PrototypeMerchantCatalog.MerchantOfferView offer)
    {
        ItemInstance item = offer.ItemInstance;
        if (item == null)
        {
            return;
        }

        GUILayout.BeginVertical(host.ListStyle);
        string title = item.Quantity > 1 ? $"{item.RichDisplayName} x{item.Quantity}" : item.RichDisplayName;
        GUILayout.Label(title, host.BodyStyle);

        string detail = PrototypeMainMenuController.BuildItemInstanceDetail(item);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            GUILayout.Label(detail, host.BodyStyle);
        }

        GUILayout.Label($"价格 {offer.Price} {host.GetCurrencyLabel()}", host.BodyStyle);
        if (GUILayout.Button("购买", host.ButtonStyle, GUILayout.Width(96f)))
        {
            BuyOffer(offer);
            GUIUtility.ExitGUI();
        }

        GUILayout.EndVertical();
    }

    private void BuyOffer(PrototypeMerchantCatalog.MerchantOfferView offer)
    {
        if (!offer.IsValid || offer.ItemInstance == null)
        {
            return;
        }

        if (!host.TrySpendFunds(offer.Price, "仓库里的现金不足。"))
        {
            return;
        }

        ItemInstance purchasedItem = offer.ItemInstance.Clone();
        bool addedSuccessfully;
        if (purchasedItem.IsWeapon)
        {
            host.WeaponLocker.Add(purchasedItem);
            addedSuccessfully = true;
        }
        else
        {
            addedSuccessfully = host.StashInventory != null && host.StashInventory.TryAddItemInstance(purchasedItem);
        }

        if (!addedSuccessfully)
        {
            host.TryAddFunds(offer.Price);
            host.SetFeedback("仓库空间不足，无法完成购买。");
            return;
        }

        string purchasedLabel = purchasedItem.Quantity > 1
            ? $"{purchasedItem.DisplayName} x{purchasedItem.Quantity}"
            : purchasedItem.DisplayName;
        host.SetFeedback($"已购买 {purchasedLabel}。");
        host.AutoSaveIfNeeded();
    }

    public static PrototypeMerchantCatalog CreateRuntimeMerchantCatalog(PrototypeItemCatalog itemCatalog)
    {
        if (itemCatalog == null)
        {
            return null;
        }

        PrototypeMerchantCatalog runtimeCatalog = ScriptableObject.CreateInstance<PrototypeMerchantCatalog>();
        runtimeCatalog.hideFlags = HideFlags.HideAndDontSave;

        ItemDefinition rifleAmmo = itemCatalog.FindByItemId("rifle_ammo");
        ItemDefinition pistolAmmo = itemCatalog.FindByItemId("pistol_ammo");
        ItemDefinition medkit = itemCatalog.FindByItemId("field_medkit");
        ItemDefinition bandage = itemCatalog.FindByItemId("bandage_roll");
        ItemDefinition tourniquet = itemCatalog.FindByItemId("tourniquet");
        ItemDefinition splint = itemCatalog.FindByItemId("field_splint");
        ItemDefinition painkiller = itemCatalog.FindByItemId("painkillers");
        ItemDefinition helmet = itemCatalog.FindByItemId("helmet_alpha");
        ItemDefinition rig = itemCatalog.FindByItemId("armored_rig");
        PrototypeWeaponDefinition carbine = itemCatalog.FindWeaponById("carbine_alpha");
        PrototypeWeaponDefinition sidearm = itemCatalog.FindWeaponById("sidearm_9mm");
        PrototypeWeaponDefinition knife = itemCatalog.FindWeaponById("combat_knife");

        runtimeCatalog.Configure(
            0.55f,
            0.6f,
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "weapons_trader",
                displayName = "武器商人",
                merchantLevel = 4,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(rifleAmmo, 30, 6),
                    CreateItemOffer(pistolAmmo, 24, 4)
                },
                weaponOffers = new List<PrototypeMerchantCatalog.WeaponOffer>
                {
                    CreateWeaponOffer(carbine, 24),
                    CreateWeaponOffer(sidearm, 16),
                    CreateWeaponOffer(knife, 10)
                }
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "medical_trader",
                displayName = "医药商人",
                merchantLevel = 3,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(medkit, 1, 10),
                    CreateItemOffer(bandage, 1, 3),
                    CreateItemOffer(tourniquet, 1, 5),
                    CreateItemOffer(splint, 1, 4),
                    CreateItemOffer(painkiller, 1, 4)
                }
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "armor_trader",
                displayName = "护甲商人",
                merchantLevel = 4,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(helmet, 1, 14),
                    CreateItemOffer(rig, 1, 20)
                }
            });

        runtimeCatalog.EnsureRuntimeInventories();
        return runtimeCatalog;
    }

    private static PrototypeMerchantCatalog.ItemOffer CreateItemOffer(ItemDefinition definition, int quantity, int price)
    {
        if (definition == null || quantity <= 0 || price <= 0)
        {
            return null;
        }

        return new PrototypeMerchantCatalog.ItemOffer
        {
            definition = definition,
            quantity = quantity,
            price = price
        };
    }

    private static PrototypeMerchantCatalog.WeaponOffer CreateWeaponOffer(PrototypeWeaponDefinition definition, int price)
    {
        if (definition == null || price <= 0)
        {
            return null;
        }

        return new PrototypeMerchantCatalog.WeaponOffer
        {
            definition = definition,
            price = price
        };
    }
}
