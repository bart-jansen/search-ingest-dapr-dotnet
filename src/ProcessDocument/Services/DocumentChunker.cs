using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProcessDocument.Models;

namespace ProcessDocument.Services
{
    public static class DocumentChunker
    {
        private const int MAX_SECTION_LENGTH = 1000;
        private const int SENTENCE_SEARCH_LIMIT = 100;
        private const int SECTION_OVERLAP = 100;

        public static async Task<List<Section>> CreateSections(string filename, List<PageMap> pageMap, string docId, string ingestionId)
        {
            var sections = new List<Section>();
            var allText = string.Join("", pageMap.Select(p => p.Text));
            var length = allText.Length;
            var start = 0;
            var end = length;

            while (start + SECTION_OVERLAP < length)
            {
                var lastWord = -1;
                end = start + MAX_SECTION_LENGTH;

                if (end > length)
                {
                    end = length;
                }
                else
                {
                    while (end < length && (end - start - MAX_SECTION_LENGTH) < SENTENCE_SEARCH_LIMIT && !".!?".Contains(allText[end]))
                    {
                        if (",;: ()[]{}\t\n".Contains(allText[end]))
                        {
                            lastWord = end;
                        }
                        end++;
                    }
                    if (end < length && !".!?".Contains(allText[end]) && lastWord > 0)
                    {
                        end = lastWord;
                    }
                }
                if (end < length)
                {
                    end++;
                }

                lastWord = -1;
                while (start > 0 && start > end - MAX_SECTION_LENGTH - 2 * SENTENCE_SEARCH_LIMIT && !".!?".Contains(allText[start]))
                {
                    if (",;: ()[]{}\t\n".Contains(allText[start]))
                    {
                        lastWord = start;
                    }
                    start--;
                }
                if (!".!?".Contains(allText[start]) && lastWord > 0)
                {
                    start = lastWord;
                }
                if (start > 0)
                {
                    start++;
                }

                var sectionText = allText[start..end];
                sections.Add(new Section
                {
                    Id = $"{ingestionId}-{docId}-section-{sections.Count}",
                    Content = sectionText,
                    Category = "",
                    SourcePage = FindPage(pageMap, start),
                    SourceFile = filename
                });

                var lastTableStart = sectionText.LastIndexOf("<table");
                if (lastTableStart > 2 * SENTENCE_SEARCH_LIMIT && lastTableStart > sectionText.LastIndexOf("</table"))
                {
                    start = Math.Min(end - SECTION_OVERLAP, start + lastTableStart);
                }
                else
                {
                    start = end - SECTION_OVERLAP;
                }
            }

            if (start + SECTION_OVERLAP < end)
            {
                sections.Add(new Section
                {
                    Id = $"{ingestionId}-{docId}-section-{sections.Count}",
                    Content = allText[start..end],
                    Category = "",
                    SourcePage = FindPage(pageMap, start),
                    SourceFile = filename
                });
            }

            return sections;
        }

        private static int FindPage(List<PageMap> pageMap, int offset)
        {
            for (var i = 0; i < pageMap.Count - 1; i++)
            {
                if (offset >= pageMap[i].Offset && offset < pageMap[i + 1].Offset)
                {
                    return i;
                }
            }
            return pageMap.Count - 1;
        }
    }
}