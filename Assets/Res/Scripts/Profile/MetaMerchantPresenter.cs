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
            GUILayout.Label("No merchant catalog configured.", host.BodyStyle);
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

        host.BeginPanel(rect, merchant.displayName, accent, $"资金 {host.GetAvailableFunds()} {host.GetCurrencyLabel()}");
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(rect.height - 130f));

        if (merchant.weaponOffers != null && merchant.weaponOffers.Count > 0)
        {
            GUILayout.Label("Weapons", host.BodyStyle);
            for (int index = 0; index < merchant.weaponOffers.Count; index++)
            {
                PrototypeMerchantCatalog.WeaponOffer offer = merchant.weaponOffers[index];
                if (offer?.definition == null || offer.price <= 0)
                {
                    continue;
                }

                GUILayout.BeginVertical(host.ListStyle);
                GUILayout.Label(offer.definition.DisplayNameWithLevel, host.BodyStyle);
                GUILayout.Label($"{(offer.definition.IsMeleeWeapon ? "Melee" : "Firearm")}  Price {offer.price}", host.BodyStyle);
                if (GUILayout.Button("Buy", host.ButtonStyle, GUILayout.Width(96f)))
                {
                    BuyWeaponOffer(offer);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndVertical();
            }

            GUILayout.Space(8f);
        }

        if (merchant.itemOffers != null && merchant.itemOffers.Count > 0)
        {
            GUILayout.Label("Supplies", host.BodyStyle);
            for (int index = 0; index < merchant.itemOffers.Count; index++)
            {
                PrototypeMerchantCatalog.ItemOffer offer = merchant.itemOffers[index];
                if (offer?.definition == null || offer.quantity <= 0 || offer.price <= 0)
                {
                    continue;
                }

                GUILayout.BeginVertical(host.ListStyle);
                GUILayout.Label($"{offer.definition.DisplayNameWithLevel} x{offer.quantity}", host.BodyStyle);
                GUILayout.Label($"Price {offer.price}", host.BodyStyle);
                if (GUILayout.Button("Buy", host.ButtonStyle, GUILayout.Width(96f)))
                {
                    BuyItemOffer(offer);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndVertical();
            }
        }

        GUILayout.EndScrollView();
        host.EndPanel();
    }

    private void BuyItemOffer(PrototypeMerchantCatalog.ItemOffer offer)
    {
        if (offer?.definition == null || offer.quantity <= 0 || offer.price <= 0 || host.StashInventory == null)
        {
            return;
        }

        if (!host.TrySpendFunds(offer.price, "Not enough cash in warehouse."))
        {
            return;
        }

        if (!host.StashInventory.TryAddItem(offer.definition, offer.quantity, out int addedQuantity) || addedQuantity < offer.quantity)
        {
            host.TryAddFunds(offer.price);
            host.SetFeedback("Warehouse has no space for that purchase.");
            return;
        }

        host.SetFeedback($"Bought {offer.definition.DisplayNameWithLevel} x{offer.quantity}.");
        host.AutoSaveIfNeeded();
    }

    private void BuyWeaponOffer(PrototypeMerchantCatalog.WeaponOffer offer)
    {
        if (offer?.definition == null || offer.price <= 0)
        {
            return;
        }

        if (!host.TrySpendFunds(offer.price, "Not enough cash in warehouse."))
        {
            return;
        }

        int startingAmmo = offer.definition.IsMeleeWeapon ? 0 : offer.definition.MagazineSize;
        WeaponInstance weaponInstance = WeaponInstance.Create(offer.definition, startingAmmo, 1f);
        host.WeaponLocker.Add(weaponInstance);
        host.SetFeedback($"Bought {weaponInstance.DisplayName}.");
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
                itemOffers = new List<PrototypeMerchantCatalog.ItemOffer>
                {
                    CreateItemOffer(helmet, 1, 14),
                    CreateItemOffer(rig, 1, 20)
                }
            });

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
