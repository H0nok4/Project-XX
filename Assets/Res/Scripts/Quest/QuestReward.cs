using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class QuestRewardItem
{
    public string definitionId = string.Empty;
    public int quantity = 1;

    public string DefinitionId => string.IsNullOrWhiteSpace(definitionId) ? string.Empty : definitionId.Trim();
    public int Quantity => Mathf.Max(1, quantity);

    public void Sanitize()
    {
        definitionId = DefinitionId;
        quantity = Quantity;
    }
}

[Serializable]
public sealed class QuestReward
{
    public int funds;
    public int experience;
    public List<QuestRewardItem> items = new List<QuestRewardItem>();
    public List<string> storyFlags = new List<string>();
    public List<string> unlockedNpcIds = new List<string>();
    public List<string> unlockedMerchantIds = new List<string>();

    public void Sanitize()
    {
        funds = Mathf.Max(0, funds);
        experience = Mathf.Max(0, experience);
        items ??= new List<QuestRewardItem>();
        storyFlags ??= new List<string>();
        unlockedNpcIds ??= new List<string>();
        unlockedMerchantIds ??= new List<string>();

        for (int index = items.Count - 1; index >= 0; index--)
        {
            QuestRewardItem item = items[index];
            if (item == null)
            {
                items.RemoveAt(index);
                continue;
            }

            item.Sanitize();
            if (string.IsNullOrWhiteSpace(item.DefinitionId))
            {
                items.RemoveAt(index);
            }
        }

        SanitizeStringList(storyFlags);
        SanitizeStringList(unlockedNpcIds);
        SanitizeStringList(unlockedMerchantIds);
    }

    private static void SanitizeStringList(List<string> values)
    {
        if (values == null)
        {
            return;
        }

        for (int index = values.Count - 1; index >= 0; index--)
        {
            string sanitized = string.IsNullOrWhiteSpace(values[index]) ? string.Empty : values[index].Trim();
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                values.RemoveAt(index);
                continue;
            }

            values[index] = sanitized;
        }
    }
}
