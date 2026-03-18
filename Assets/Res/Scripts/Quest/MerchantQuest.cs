using System;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class MerchantQuest : Quest
{
    public string merchantId = string.Empty;
    public int reputationReward;

    public string MerchantId => string.IsNullOrWhiteSpace(merchantId) ? string.Empty : merchantId.Trim();
    public int ReputationReward => Mathf.Max(0, reputationReward);

    public override void Sanitize()
    {
        base.Sanitize();
        merchantId = MerchantId;
        reputationReward = ReputationReward;
    }

    public override string GrantRewards(QuestManager manager)
    {
        StringBuilder builder = new StringBuilder(128);
        string baseRewardSummary = base.GrantRewards(manager);
        if (!string.IsNullOrWhiteSpace(baseRewardSummary))
        {
            builder.Append(baseRewardSummary);
        }

        if (manager != null && ReputationReward > 0 && !string.IsNullOrWhiteSpace(MerchantId))
        {
            string reputationSummary = manager.AddMerchantReputationReward(MerchantId, ReputationReward);
            if (!string.IsNullOrWhiteSpace(reputationSummary))
            {
                if (builder.Length > 0)
                {
                    builder.Append("，");
                }

                builder.Append(reputationSummary);
            }
        }

        return builder.ToString();
    }
}
