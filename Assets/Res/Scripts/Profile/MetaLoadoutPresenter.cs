using UnityEngine;

public sealed class MetaLoadoutPresenter
{
    private readonly PrototypeMainMenuController host;
    private Vector2 homeSummaryScroll;

    public MetaLoadoutPresenter(PrototypeMainMenuController host)
    {
        this.host = host;
    }

    public void DrawHomePage()
    {
        Rect panelRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), 380f);
        GUI.Box(panelRect, string.Empty, host.SectionStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 16f, panelRect.width - 36f, panelRect.height - 32f));
        GUILayout.Label("Ready Room", host.SectionStyle);
        GUILayout.Space(10f);
        GUILayout.Label(
            "Warehouse items and weapon locker weapons are safe. The raid backpack, equipped firearms, and armor are risky. The melee slot, secure container, and special equipment slots are protected and survive raid death.",
            host.BodyStyle);

        GUILayout.Space(18f);
        GUILayout.Label($"可用资金：{host.GetAvailableFunds()} {host.GetCurrencyLabel()}", host.BodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label($"Level: {host.PlayerLevel}", host.BodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label(BuildSummaryText(), host.BodyStyle);

        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enter Battle", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.StartRaid();
        }

        if (GUILayout.Button("Open Warehouse", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Warehouse;
        }

        if (GUILayout.Button("Visit Merchants", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Merchants;
        }

        GUILayout.EndHorizontal();

        if (host.ShouldShowBaseHubEntry())
        {
            GUILayout.Space(10f);
            if (GUILayout.Button("Enter Base Hub", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(42f)))
            {
                host.EnterBaseHub();
            }
        }

        GUILayout.EndArea();
    }

    public void DrawHomePageCompact()
    {
        float panelHeight = Mathf.Max(420f, Screen.height - 220f);
        Rect panelRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), panelHeight);
        GUI.Box(panelRect, string.Empty, host.SectionStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 16f, panelRect.width - 36f, panelRect.height - 32f));
        GUILayout.Label("Ready Room", host.SectionStyle);
        GUILayout.Space(10f);
        GUILayout.Label(
            "Warehouse items and weapon locker weapons are safe. The raid backpack, equipped firearms, and armor are risky. The melee slot, secure container, and special equipment slots are protected and survive raid death.",
            host.BodyStyle);

        GUILayout.Space(18f);
        GUILayout.Label($"Funds: {host.GetAvailableFunds()} {host.GetCurrencyLabel()}", host.BodyStyle);
        GUILayout.Space(8f);

        GUILayout.Label($"Level: {host.PlayerLevel}", host.BodyStyle);
        GUILayout.Space(8f);

        float summaryHeight = Mathf.Clamp(panelRect.height - 236f, 120f, 340f);
        homeSummaryScroll = GUILayout.BeginScrollView(homeSummaryScroll, GUILayout.Height(summaryHeight));
        GUILayout.Label(BuildHomePageSummaryText(), host.BodyStyle);
        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        GUILayout.Space(16f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enter Battle", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.StartRaid();
        }

        if (GUILayout.Button("Open Warehouse", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Warehouse;
        }

        if (GUILayout.Button("Visit Merchants", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Merchants;
        }

        GUILayout.EndHorizontal();

        if (host.ShouldShowBaseHubEntry())
        {
            GUILayout.Space(10f);
            if (GUILayout.Button("Enter Base Hub", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(42f)))
            {
                host.EnterBaseHub();
            }
        }

        GUILayout.EndArea();
    }

    private string BuildHomePageSummaryText()
    {
        return
            $"Funds: {host.GetAvailableFunds()} {host.GetCurrencyLabel()}" +
            $"Warehouse: item stacks {host.GetInventoryStackCount(host.StashInventory)}  weapons {host.WeaponLocker.Count}" +
            $"Raid backpack: {host.GetInventoryStackCount(host.RaidBackpackInventory)}  Secure: {host.GetInventoryStackCount(host.SecureContainerInventory)}" +
            $"Special: {host.GetInventoryStackCount(host.SpecialEquipmentInventory)}  Armor: {host.EquippedArmor.Count}" +
            $"Melee: {(host.EquippedMeleeWeapon != null ? host.EquippedMeleeWeapon.DisplayName : "Empty")}" +
            $"Primary: {(host.EquippedPrimaryWeapon != null ? host.EquippedPrimaryWeapon.DisplayName : "Empty")}" +
            $"Secondary: {(host.EquippedSecondaryWeapon != null ? host.EquippedSecondaryWeapon.DisplayName : "Empty")}";
    }

    private string BuildSummaryText()
    {
        return
            $"资金：{host.GetAvailableFunds()} {host.GetCurrencyLabel()}" +
            $"Warehouse item stacks: {host.GetInventoryStackCount(host.StashInventory)}" +
            $"Warehouse weapons: {host.WeaponLocker.Count}" +
            $"Raid backpack stacks: {host.GetInventoryStackCount(host.RaidBackpackInventory)}" +
            $"Secure container stacks: {host.GetInventoryStackCount(host.SecureContainerInventory)}" +
            $"Special equipment stacks: {host.GetInventoryStackCount(host.SpecialEquipmentInventory)}" +
            $"Protected melee slot: {(host.EquippedMeleeWeapon != null ? host.EquippedMeleeWeapon.DisplayName : "Empty")}" +
            $"Primary: {(host.EquippedPrimaryWeapon != null ? host.EquippedPrimaryWeapon.DisplayName : "Empty")}" +
            $"Secondary: {(host.EquippedSecondaryWeapon != null ? host.EquippedSecondaryWeapon.DisplayName : "Empty")}" +
            $"Armor pieces: {host.EquippedArmor.Count}" +
            $"Profile file: {PrototypeProfileService.SavePath}";
    }
}
