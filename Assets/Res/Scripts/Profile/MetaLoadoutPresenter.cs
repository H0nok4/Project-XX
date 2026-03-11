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
        Rect panelRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), 420f);
        GUI.Box(panelRect, string.Empty, host.SectionStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 16f, panelRect.width - 36f, panelRect.height - 32f));
        DrawReadyRoomIntro();
        DrawRaidSelection();

        GUILayout.Space(16f);
        GUILayout.Label($"Funds: {host.GetAvailableFunds()} {host.GetCurrencyLabel()}", host.BodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label($"Level: {host.PlayerLevel}", host.BodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label(BuildSummaryText(), host.BodyStyle);

        GUILayout.FlexibleSpace();
        DrawActionButtons();
        GUILayout.EndArea();
    }

    public void DrawHomePageCompact()
    {
        float panelHeight = Mathf.Max(460f, Screen.height - 220f);
        Rect panelRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), panelHeight);
        GUI.Box(panelRect, string.Empty, host.SectionStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 16f, panelRect.width - 36f, panelRect.height - 32f));
        DrawReadyRoomIntro();
        DrawRaidSelection();

        GUILayout.Space(16f);
        GUILayout.Label($"Funds: {host.GetAvailableFunds()} {host.GetCurrencyLabel()}", host.BodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label($"Level: {host.PlayerLevel}", host.BodyStyle);
        GUILayout.Space(8f);

        float summaryHeight = Mathf.Clamp(panelRect.height - 300f, 96f, 260f);
        homeSummaryScroll = GUILayout.BeginScrollView(homeSummaryScroll, GUILayout.Height(summaryHeight));
        GUILayout.Label(BuildHomePageSummaryText(), host.BodyStyle);
        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        DrawActionButtons();
        GUILayout.EndArea();
    }

    private void DrawReadyRoomIntro()
    {
        GUILayout.Label("Ready Room", host.SectionStyle);
        GUILayout.Space(10f);
        GUILayout.Label(
            "Warehouse items and weapon locker weapons are safe. The raid backpack, equipped firearms, and armor are risky. The melee slot, secure container, and special equipment slots are protected and survive raid death.",
            host.BodyStyle);
    }

    private void DrawRaidSelection()
    {
        int optionCount = host.GetRaidSceneOptionCount();
        if (optionCount <= 0)
        {
            return;
        }

        GUILayout.Space(18f);
        GUILayout.Label("Deployment Target", host.SectionStyle);
        GUILayout.Space(6f);
        GUILayout.Label(host.GetSelectedRaidSceneDisplayName(), host.BodyStyle);
        GUILayout.Label(host.GetSelectedRaidSceneDescription(), host.BodyStyle);

        if (optionCount == 1)
        {
            return;
        }

        GUILayout.Space(10f);
        GUILayout.BeginHorizontal();
        for (int index = 0; index < optionCount; index++)
        {
            bool selected = index == host.GetSelectedRaidSceneIndex();
            string label = selected
                ? $"> {host.GetRaidSceneOptionDisplayName(index)}"
                : host.GetRaidSceneOptionDisplayName(index);
            if (GUILayout.Button(label, host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(36f)))
            {
                host.SelectRaidScene(index);
            }
        }

        GUILayout.EndHorizontal();
    }

    private void DrawActionButtons()
    {
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
    }

    private string BuildHomePageSummaryText()
    {
        return
            $"Selected raid: {host.GetSelectedRaidSceneDisplayName()}\n" +
            $"Funds: {host.GetAvailableFunds()} {host.GetCurrencyLabel()}\n" +
            $"Warehouse: item stacks {host.GetInventoryStackCount(host.StashInventory)}  weapons {host.WeaponLocker.Count}\n" +
            $"Raid backpack: {host.GetInventoryStackCount(host.RaidBackpackInventory)}  Secure: {host.GetInventoryStackCount(host.SecureContainerInventory)}\n" +
            $"Special: {host.GetInventoryStackCount(host.SpecialEquipmentInventory)}  Armor: {host.EquippedArmor.Count}\n" +
            $"Melee: {(host.EquippedMeleeWeapon != null ? host.EquippedMeleeWeapon.DisplayName : "Empty")}\n" +
            $"Primary: {(host.EquippedPrimaryWeapon != null ? host.EquippedPrimaryWeapon.DisplayName : "Empty")}\n" +
            $"Secondary: {(host.EquippedSecondaryWeapon != null ? host.EquippedSecondaryWeapon.DisplayName : "Empty")}";
    }

    private string BuildSummaryText()
    {
        return
            $"Selected raid: {host.GetSelectedRaidSceneDisplayName()}\n" +
            $"Funds: {host.GetAvailableFunds()} {host.GetCurrencyLabel()}\n" +
            $"Warehouse item stacks: {host.GetInventoryStackCount(host.StashInventory)}\n" +
            $"Warehouse weapons: {host.WeaponLocker.Count}\n" +
            $"Raid backpack stacks: {host.GetInventoryStackCount(host.RaidBackpackInventory)}\n" +
            $"Secure container stacks: {host.GetInventoryStackCount(host.SecureContainerInventory)}\n" +
            $"Special equipment stacks: {host.GetInventoryStackCount(host.SpecialEquipmentInventory)}\n" +
            $"Protected melee slot: {(host.EquippedMeleeWeapon != null ? host.EquippedMeleeWeapon.DisplayName : "Empty")}\n" +
            $"Primary: {(host.EquippedPrimaryWeapon != null ? host.EquippedPrimaryWeapon.DisplayName : "Empty")}\n" +
            $"Secondary: {(host.EquippedSecondaryWeapon != null ? host.EquippedSecondaryWeapon.DisplayName : "Empty")}\n" +
            $"Armor pieces: {host.EquippedArmor.Count}\n" +
            $"Profile file: {PrototypeProfileService.SavePath}";
    }
}
