using System.Collections.Generic;

public static class PrototypeQuestCatalog
{
    public static List<Quest> CreateDefaultQuests(PrototypeItemCatalog itemCatalog)
    {
        var quests = new List<Quest>
        {
            new Quest
            {
                questId = "chapter1_base_orientation",
                questName = "基地熟悉",
                description = "先把基地的基础动线和关键人员认清。向指挥官报到，熟悉仓库，再和武器商人确认补给。",
                type = QuestType.Main,
                giverNpcId = "commander",
                turnInNpcId = "commander",
                objectives = new List<QuestObjective>
                {
                    new TalkObjective
                    {
                        description = "与指挥官对话",
                        speakerId = "commander",
                        requiredProgress = 1
                    },
                    new ExploreObjective
                    {
                        description = "参观仓库区",
                        locationId = "base_warehouse",
                        requiredProgress = 1
                    },
                    new TalkObjective
                    {
                        description = "与武器商人对话",
                        speakerId = "weapons_trader",
                        requiredProgress = 1
                    }
                },
                reward = new QuestReward
                {
                    funds = 500,
                    experience = 80,
                    items = new List<QuestRewardItem>
                    {
                        new QuestRewardItem { definitionId = "sidearm_9mm", quantity = 1 }
                    }
                }
            },
            new Quest
            {
                questId = "chapter1_first_sortie",
                questName = "首次出击",
                description = "完成第一次标准出击流程：进入战局、击杀敌人、搜刮容器并成功撤离。",
                type = QuestType.Main,
                giverNpcId = "commander",
                turnInNpcId = "commander",
                prerequisiteQuests = new List<string> { "chapter1_base_orientation" },
                objectives = new List<QuestObjective>
                {
                    new CustomEventObjective
                    {
                        description = "进入战斗地图",
                        eventId = "raid_started",
                        requiredProgress = 1
                    },
                    new KillObjective
                    {
                        description = "击杀 5 个敌人",
                        requiredProgress = 5
                    },
                    new CustomEventObjective
                    {
                        description = "搜刮 3 个容器",
                        eventId = "loot_container_opened",
                        requiredProgress = 3
                    },
                    new ExtractObjective
                    {
                        description = "成功撤离",
                        requiredProgress = 1
                    }
                },
                reward = new QuestReward
                {
                    funds = 1000,
                    experience = 160,
                    items = new List<QuestRewardItem>
                    {
                        new QuestRewardItem { definitionId = "bandage_roll", quantity = 2 },
                        new QuestRewardItem { definitionId = "rifle_ammo", quantity = 30 }
                    }
                }
            },
            new Quest
            {
                questId = "chapter1_supply_run",
                questName = "物资补给",
                description = "情报官要你补上一批常用医疗和弹药。把物资放进仓库后回来汇报。",
                type = QuestType.Main,
                giverNpcId = "intel_officer",
                turnInNpcId = "intel_officer",
                prerequisiteQuests = new List<string> { "chapter1_first_sortie" },
                objectives = new List<QuestObjective>
                {
                    new DeliverObjective
                    {
                        description = "提交战地医疗包 x1",
                        itemId = "field_medkit",
                        requiredProgress = 1
                    },
                    new DeliverObjective
                    {
                        description = "提交 5.56 FMJ x20",
                        itemId = "rifle_ammo",
                        requiredProgress = 20
                    }
                },
                reward = new QuestReward
                {
                    funds = 1500,
                    experience = 200,
                    items = new List<QuestRewardItem>
                    {
                        new QuestRewardItem { definitionId = "armored_rig", quantity = 1 }
                    }
                }
            },
            new Quest
            {
                questId = "chapter1_danger_zone",
                questName = "危险区域",
                description = "训练官要你进入更危险的区域，搜查武器箱，击倒头目后安全撤离。",
                type = QuestType.Main,
                giverNpcId = "trainer",
                turnInNpcId = "trainer",
                prerequisiteQuests = new List<string> { "chapter1_supply_run" },
                objectives = new List<QuestObjective>
                {
                    new CustomEventObjective
                    {
                        description = "搜查武器箱",
                        eventId = "weapon_crate_opened",
                        requiredProgress = 1
                    },
                    new KillObjective
                    {
                        description = "击倒危险区域头目",
                        requireBoss = true,
                        requiredProgress = 1
                    },
                    new ExtractObjective
                    {
                        description = "携带情报安全撤离",
                        requiredProgress = 1
                    }
                },
                reward = new QuestReward
                {
                    funds = 3000,
                    experience = 280,
                    items = new List<QuestRewardItem>
                    {
                        new QuestRewardItem { definitionId = "carbine_alpha", quantity = 1 }
                    },
                    storyFlags = new List<string> { "chapter1_complete" }
                }
            },
            new MerchantQuest
            {
                questId = "merchant_weapons_supply",
                questName = "弹药补给委托",
                description = "武器商人需要一批可直接上架的 5.56 弹药。",
                type = QuestType.Daily,
                giverNpcId = "weapons_trader",
                turnInNpcId = "weapons_trader",
                merchantId = "weapons_trader",
                reputationReward = 35,
                objectives = new List<QuestObjective>
                {
                    new DeliverObjective
                    {
                        description = "提交 5.56 FMJ x60",
                        itemId = "rifle_ammo",
                        requiredProgress = 60
                    }
                },
                reward = new QuestReward
                {
                    funds = 180,
                    experience = 35
                }
            },
            new MerchantQuest
            {
                questId = "merchant_medical_supply",
                questName = "急救物资委托",
                description = "医药商人想收一套完整的战地医疗包。",
                type = QuestType.Daily,
                giverNpcId = "medical_trader",
                turnInNpcId = "medical_trader",
                merchantId = "medical_trader",
                reputationReward = 40,
                objectives = new List<QuestObjective>
                {
                    new DeliverObjective
                    {
                        description = "提交战地医疗包 x1",
                        itemId = "field_medkit",
                        requiredProgress = 1
                    }
                },
                reward = new QuestReward
                {
                    funds = 200,
                    experience = 40
                }
            },
            new MerchantQuest
            {
                questId = "merchant_armor_supply",
                questName = "防具整备委托",
                description = "护甲商人正在补货，想要一顶可用头盔做样品。",
                type = QuestType.Daily,
                giverNpcId = "armor_trader",
                turnInNpcId = "armor_trader",
                merchantId = "armor_trader",
                reputationReward = 45,
                objectives = new List<QuestObjective>
                {
                    new DeliverObjective
                    {
                        description = "提交原型头盔 x1",
                        itemId = "helmet_alpha",
                        requiredProgress = 1
                    }
                },
                reward = new QuestReward
                {
                    funds = 260,
                    experience = 45
                }
            },
            new MerchantQuest
            {
                questId = "merchant_general_supply",
                questName = "杂项补给委托",
                description = "杂货商人缺少止痛药，帮他补上两盒。",
                type = QuestType.Daily,
                giverNpcId = "general_trader",
                turnInNpcId = "general_trader",
                merchantId = "general_trader",
                reputationReward = 30,
                objectives = new List<QuestObjective>
                {
                    new DeliverObjective
                    {
                        description = "提交止痛药 x2",
                        itemId = "painkillers",
                        requiredProgress = 2
                    }
                },
                reward = new QuestReward
                {
                    funds = 120,
                    experience = 30
                }
            }
        };

        for (int index = 0; index < quests.Count; index++)
        {
            quests[index]?.Sanitize();
        }

        return quests;
    }
}
