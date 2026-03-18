using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MerchantManager
{
    public readonly struct TradeUpdateResult
    {
        public TradeUpdateResult(bool levelChanged, bool reputationChanged)
        {
            LevelChanged = levelChanged;
            ReputationChanged = reputationChanged;
        }

        public bool LevelChanged { get; }
        public bool ReputationChanged { get; }
        public bool AnyChange => LevelChanged || ReputationChanged;
    }

    public readonly struct SupplyRequest
    {
        public SupplyRequest(string merchantId, string title, string requestedItemId, int requestedQuantity, int reputationReward)
        {
            MerchantId = merchantId ?? string.Empty;
            Title = title ?? string.Empty;
            RequestedItemId = requestedItemId ?? string.Empty;
            RequestedQuantity = Mathf.Max(1, requestedQuantity);
            ReputationReward = Mathf.Max(1, reputationReward);
        }

        public string MerchantId { get; }
        public string Title { get; }
        public string RequestedItemId { get; }
        public int RequestedQuantity { get; }
        public int ReputationReward { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(MerchantId) && !string.IsNullOrWhiteSpace(RequestedItemId);
    }

    private static readonly SupplyRequest[] DefaultSupplyRequests =
    {
        new SupplyRequest("weapons_trader", "弹药补给委托", "rifle_ammo", 60, 35),
        new SupplyRequest("medical_trader", "急救物资委托", "field_medkit", 1, 40),
        new SupplyRequest("armor_trader", "防具整备委托", "helmet_alpha", 1, 45),
        new SupplyRequest("general_trader", "杂项补给委托", "painkillers", 2, 30)
    };

    private readonly WorldStateData worldState;
    private readonly Dictionary<string, MerchantData> merchantLookup = new Dictionary<string, MerchantData>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SupplyRequest> supplyRequestLookup = new Dictionary<string, SupplyRequest>(StringComparer.OrdinalIgnoreCase);

    public MerchantManager(WorldStateData worldStateData)
    {
        worldState = worldStateData ?? new WorldStateData();
        RebuildLookup();
    }

    public void EnsureMerchantsFromCatalog(PrototypeMerchantCatalog catalog)
    {
        if (catalog == null || catalog.Merchants == null)
        {
            return;
        }

        for (int merchantIndex = 0; merchantIndex < catalog.Merchants.Count; merchantIndex++)
        {
            PrototypeMerchantCatalog.MerchantDefinition merchant = catalog.Merchants[merchantIndex];
            if (merchant == null)
            {
                continue;
            }

            MerchantData data = GetOrCreateMerchantData(merchant.MerchantId, merchant.MerchantLevel);
            data.Sanitize(merchant.MerchantId, merchant.MerchantLevel);
        }
    }

    public MerchantData GetMerchantData(string merchantId, int defaultStartingLevel = MerchantData.MinLevel)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return null;
        }

        string sanitizedMerchantId = merchantId.Trim();
        if (merchantLookup.TryGetValue(sanitizedMerchantId, out MerchantData data))
        {
            data.Sanitize(sanitizedMerchantId, data.StartingLevel);
            return data;
        }

        return GetOrCreateMerchantData(sanitizedMerchantId, defaultStartingLevel);
    }

    public TradeUpdateResult RecordTrade(string merchantId, int amount)
    {
        MerchantData data = GetMerchantData(merchantId);
        if (data == null || amount <= 0)
        {
            return default;
        }

        bool levelChanged = data.AddTradeAmount(amount);
        return new TradeUpdateResult(levelChanged, false);
    }

    public TradeUpdateResult AddReputation(string merchantId, int points)
    {
        MerchantData data = GetMerchantData(merchantId);
        if (data == null || points <= 0)
        {
            return default;
        }

        bool reputationChanged = data.AddReputation(points);
        return new TradeUpdateResult(false, reputationChanged);
    }

    public int GetEffectiveMerchantLevel(PrototypeMerchantCatalog.MerchantDefinition merchant)
    {
        if (merchant == null)
        {
            return MerchantData.MinLevel;
        }

        MerchantData data = GetMerchantData(merchant.MerchantId, merchant.MerchantLevel);
        return data != null ? data.Level : merchant.MerchantLevel;
    }

    public float GetPriceMultiplier(string merchantId)
    {
        MerchantData data = GetMerchantData(merchantId);
        return data != null ? data.GetPriceMultiplier() : 1f;
    }

    public SupplyRequest GetSupplyRequest(string merchantId)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return default;
        }

        return supplyRequestLookup.TryGetValue(merchantId.Trim(), out SupplyRequest request)
            ? request
            : default;
    }

    private MerchantData GetOrCreateMerchantData(string merchantId, int defaultStartingLevel)
    {
        string sanitizedMerchantId = string.IsNullOrWhiteSpace(merchantId) ? string.Empty : merchantId.Trim();
        if (string.IsNullOrWhiteSpace(sanitizedMerchantId))
        {
            return null;
        }

        if (merchantLookup.TryGetValue(sanitizedMerchantId, out MerchantData existing))
        {
            return existing;
        }

        worldState.merchantProgress ??= new List<MerchantData>();
        var created = new MerchantData
        {
            merchantId = sanitizedMerchantId,
            startingLevel = Mathf.Clamp(defaultStartingLevel, MerchantData.MinLevel, MerchantData.MaxLevel),
            level = Mathf.Clamp(defaultStartingLevel, MerchantData.MinLevel, MerchantData.MaxLevel)
        };
        created.Sanitize(sanitizedMerchantId, defaultStartingLevel);
        worldState.merchantProgress.Add(created);
        merchantLookup[sanitizedMerchantId] = created;
        return created;
    }

    private void RebuildLookup()
    {
        merchantLookup.Clear();
        worldState.merchantProgress ??= new List<MerchantData>();

        for (int index = worldState.merchantProgress.Count - 1; index >= 0; index--)
        {
            MerchantData data = worldState.merchantProgress[index];
            if (data == null)
            {
                worldState.merchantProgress.RemoveAt(index);
                continue;
            }

            data.Sanitize(data.merchantId, data.startingLevel);
            if (string.IsNullOrWhiteSpace(data.MerchantId) || merchantLookup.ContainsKey(data.MerchantId))
            {
                worldState.merchantProgress.RemoveAt(index);
                continue;
            }

            merchantLookup.Add(data.MerchantId, data);
        }

        supplyRequestLookup.Clear();
        for (int index = 0; index < DefaultSupplyRequests.Length; index++)
        {
            SupplyRequest request = DefaultSupplyRequests[index];
            if (request.IsValid)
            {
                supplyRequestLookup[request.MerchantId] = request;
            }
        }
    }
}
