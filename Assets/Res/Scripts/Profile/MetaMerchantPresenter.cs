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

        List<PrototypeMerchantCatalog.MerchantDefinition> displayMerchants = BuildMerchantDisplayOrder(merchantCatalog);
        if (displayMerchants.Count == 0)
        {
            return;
        }

        float panelTop = 140f;
        float panelHeight = Mathf.Max(400f, Screen.height - 220f);
        float panelWidth = Mathf.Max(220f, (Screen.width - 308f - Mathf.Max(0, displayMerchants.Count - 1) * 16f) / Mathf.Max(1, displayMerchants.Count));
        float firstX = 292f;

        for (int merchantIndex = 0; merchantIndex < displayMerchants.Count; merchantIndex++)
        {
            Vector2 scroll = merchantIndex == 0
                ? weaponMerchantScroll
                : merchantIndex == 1
                    ? medicalMerchantScroll
                    : merchantIndex == 2
                        ? armorMerchantScroll
                        : Vector2.zero;

            DrawMerchantPanel(
                new Rect(firstX + (panelWidth + 16f) * merchantIndex, panelTop, panelWidth, panelHeight),
                displayMerchants[merchantIndex],
                ResolveMerchantAccent(merchantIndex),
                ref scroll);

            if (merchantIndex == 0)
            {
                weaponMerchantScroll = scroll;
            }
            else if (merchantIndex == 1)
            {
                medicalMerchantScroll = scroll;
            }
            else if (merchantIndex == 2)
            {
                armorMerchantScroll = scroll;
            }
        }
    }

    private void DrawMerchantPanel(Rect rect, PrototypeMerchantCatalog.MerchantDefinition merchant, Color accent, ref Vector2 scroll)
    {
        if (merchant == null)
        {
            return;
        }

        string subtitle = host.IsMerchantFocused(merchant.MerchantId)
            ? $"当前交互  ·  等级 {merchant.MerchantLevel}  资金 {host.GetAvailableFunds()} {host.GetCurrencyLabel()}"
            : $"等级 {merchant.MerchantLevel}  资金 {host.GetAvailableFunds()} {host.GetCurrencyLabel()}";
        host.BeginPanel(rect, merchant.DisplayName, accent, subtitle);
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

    private List<PrototypeMerchantCatalog.MerchantDefinition> BuildMerchantDisplayOrder(PrototypeMerchantCatalog merchantCatalog)
    {
        var orderedMerchants = new List<PrototypeMerchantCatalog.MerchantDefinition>();
        if (merchantCatalog == null || merchantCatalog.Merchants == null)
        {
            return orderedMerchants;
        }

        PrototypeMerchantCatalog.MerchantDefinition focusedMerchant = host.GetFocusedMerchant();
        if (focusedMerchant != null)
        {
            orderedMerchants.Add(focusedMerchant);
            return orderedMerchants;
        }

        for (int merchantIndex = 0; merchantIndex < merchantCatalog.Merchants.Count; merchantIndex++)
        {
            PrototypeMerchantCatalog.MerchantDefinition merchant = merchantCatalog.Merchants[merchantIndex];
            if (merchant == null || ReferenceEquals(merchant, focusedMerchant))
            {
                continue;
            }

            orderedMerchants.Add(merchant);
        }

        return orderedMerchants;
    }

    private Color ResolveMerchantAccent(int merchantIndex)
    {
        switch (merchantIndex % 4)
        {
            case 1:
                return host.BackpackColor;

            case 2:
                return host.ProtectedColor;

            case 3:
                return host.LockerColor;

            default:
                return host.StashColor;
        }
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

    internal void BuyOffer(PrototypeMerchantCatalog.MerchantOfferView offer)
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
        ItemDefinition secureCase = itemCatalog.FindByItemId("secure_case_alpha");
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
            },
            new PrototypeMerchantCatalog.MerchantDefinition
            {
                merchantId = "general_trader",
                displayName = "杂货商人",
                merchantLevel = 2,
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(painkiller, 1, 4),
                    CreateItemOffer(splint, 1, 4),
                    CreateItemOffer(secureCase, 1, 32)
                },
                weaponOffers = new List<PrototypeMerchantCatalog.WeaponOffer>
                {
                    CreateWeaponOffer(knife, 11)
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
