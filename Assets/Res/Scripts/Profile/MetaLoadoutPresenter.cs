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
        GUILayout.Label($"资金：{host.GetAvailableFunds()} {host.GetCurrencyLabel()}", host.BodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label($"等级：{host.PlayerLevel}", host.BodyStyle);
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
        GUILayout.Label($"资金：{host.GetAvailableFunds()} {host.GetCurrencyLabel()}", host.BodyStyle);
        GUILayout.Space(8f);
        GUILayout.Label($"等级：{host.PlayerLevel}", host.BodyStyle);
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
        GUILayout.Label("战备室", host.SectionStyle);
        GUILayout.Space(10f);
        GUILayout.Label(
            "仓库物品和武器柜中的武器是安全的。战局背包、已装备枪械和护甲存在丢失风险。近战槽、安全箱和特殊装备槽属于保护栏位，角色在战斗中死亡后仍会保留。",
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
        GUILayout.Label("出击地图", host.SectionStyle);
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
        if (GUILayout.Button("进入战斗", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.StartRaid();
        }

        if (GUILayout.Button("打开仓库", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Warehouse;
        }

        if (GUILayout.Button("拜访商人", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Merchants;
        }

        GUILayout.EndHorizontal();

        if (host.ShouldShowBaseHubEntry())
        {
            GUILayout.Space(10f);
            if (GUILayout.Button("进入基地", host.ButtonStyle, GUILayout.Width(180f), GUILayout.Height(42f)))
            {
                host.EnterBaseHub();
            }
        }
    }

    private string BuildHomePageSummaryText()
    {
        return
            $"已选地图：{host.GetSelectedRaidSceneDisplayName()}\n" +
            $"资金：{host.GetAvailableFunds()} {host.GetCurrencyLabel()}\n" +
            $"仓库：物品堆叠 {host.GetInventoryStackCount(host.StashInventory)}  武器 {host.WeaponLocker.Count}\n" +
            $"战局背包：{host.GetInventoryStackCount(host.RaidBackpackInventory)}  安全箱：{host.GetInventoryStackCount(host.SecureContainerInventory)}\n" +
            $"特殊装备：{host.GetInventoryStackCount(host.SpecialEquipmentInventory)}  护甲：{host.EquippedArmor.Count}\n" +
            $"近战：{(host.EquippedMeleeWeapon != null ? host.EquippedMeleeWeapon.DisplayName : "空")}\n" +
            $"主武器：{(host.EquippedPrimaryWeapon != null ? host.EquippedPrimaryWeapon.DisplayName : "空")}\n" +
            $"副武器：{(host.EquippedSecondaryWeapon != null ? host.EquippedSecondaryWeapon.DisplayName : "空")}";
    }

    private string BuildSummaryText()
    {
        return
            $"已选地图：{host.GetSelectedRaidSceneDisplayName()}\n" +
            $"资金：{host.GetAvailableFunds()} {host.GetCurrencyLabel()}\n" +
            $"仓库物品堆叠：{host.GetInventoryStackCount(host.StashInventory)}\n" +
            $"仓库武器：{host.WeaponLocker.Count}\n" +
            $"战局背包堆叠：{host.GetInventoryStackCount(host.RaidBackpackInventory)}\n" +
            $"安全箱堆叠：{host.GetInventoryStackCount(host.SecureContainerInventory)}\n" +
            $"特殊装备堆叠：{host.GetInventoryStackCount(host.SpecialEquipmentInventory)}\n" +
            $"保护近战槽：{(host.EquippedMeleeWeapon != null ? host.EquippedMeleeWeapon.DisplayName : "空")}\n" +
            $"主武器：{(host.EquippedPrimaryWeapon != null ? host.EquippedPrimaryWeapon.DisplayName : "空")}\n" +
            $"副武器：{(host.EquippedSecondaryWeapon != null ? host.EquippedSecondaryWeapon.DisplayName : "空")}\n" +
            $"护甲件数：{host.EquippedArmor.Count}\n" +
            $"档案文件：{PrototypeProfileService.SavePath}";
    }
}
