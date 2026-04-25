using System.Collections.Generic;

namespace FileRenamer
{
    internal static class SelectionAfterDeleteCalculator
    {
        public static int? GetOriginalIndexToSelectAfterRemoval(
            int countBefore,
            HashSet<int> deleteSet,
            IReadOnlyList<int> sortedDeleteIndices)
        {
            if (sortedDeleteIndices.Count == 0)
            {
                return null;
            }

            var lastDel = sortedDeleteIndices[sortedDeleteIndices.Count - 1];

            for (var j = lastDel + 1; j < countBefore; j++)
            {
                if (!deleteSet.Contains(j))
                {
                    return j;
                }
            }

            for (var j = lastDel - 1; j >= 0; j--)
            {
                if (!deleteSet.Contains(j))
                {
                    return j;
                }
            }

            return null;
        }
    }
}
