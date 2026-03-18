using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerSkillTree
{
    public List<string> unlockedNodeIds = new List<string>();

    public bool IsUnlocked(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || unlockedNodeIds == null)
        {
            return false;
        }

        string sanitizedNodeId = nodeId.Trim();
        for (int index = 0; index < unlockedNodeIds.Count; index++)
        {
            if (string.Equals(unlockedNodeIds[index], sanitizedNodeId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public int GetUnlockedCount()
    {
        return unlockedNodeIds != null ? unlockedNodeIds.Count : 0;
    }

    public void Sanitize()
    {
        unlockedNodeIds ??= new List<string>();

        var seenNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = unlockedNodeIds.Count - 1; index >= 0; index--)
        {
            string nodeId = unlockedNodeIds[index];
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                unlockedNodeIds.RemoveAt(index);
                continue;
            }

            string sanitizedNodeId = nodeId.Trim();
            if (!seenNodeIds.Add(sanitizedNodeId))
            {
                unlockedNodeIds.RemoveAt(index);
                continue;
            }

            unlockedNodeIds[index] = sanitizedNodeId;
        }
    }
}
