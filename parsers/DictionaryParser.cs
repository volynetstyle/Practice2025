using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using practice.entities;

public class DictionaryParser
{
    private const string HeadwordXPath = "//h1[contains(@class, 'elMfuCTjKMwxtSEEnUsi')]";
    private const string RegionalVariantsXPath = ".//div[contains(@class, 'yVEBrFtF5G2MtP0lUVBk')]/section";
    private const string PronunciationXPath = ".//div[@data-type='pronunciation-toggle']";
    private const string PartsOfSpeechXPath = ".//section[contains(@data-type, 'part-of-speech-module')]/div";
    private const string DefinitionsXPath = ".//ol[@data-type='definition-content-list']/li";
    private const string ExamplesXPath = ".//div[contains(@class, 'NZKOFkdkcvYgD3lqOIJw')]//i";
    private const string SynonymsXPath = ".//p[@class='OsvsNC770LPFqkTe32Ng']/a[starts-with(@data-linkname, 'view-synonyms')]";
    private const string AntonymsXPath = ".//p[@class='OsvsNC770LPFqkTe32Ng']/a[starts-with(@data-linkname, 'view-antonyms')]";
    private const string AdditionalSectionsXPath = ".//div[contains(@class, 'YommMxopETPCP_wzxPxE')]";

    public  WordCard Parse(HtmlDocument html)
    {
        return new WordCard
        {
            Headword = ParseHeadword(html),
            RegionalVariants = ParseRegionalVariants(html)
        };
    }

    private static string ParseHeadword(HtmlDocument htmlDoc) =>
        htmlDoc.DocumentNode.SelectSingleNode(HeadwordXPath)?.InnerText.Trim() ?? string.Empty;

    private static Dictionary<string, WordCardVariant> ParseRegionalVariants(HtmlDocument htmlDoc)
    {
        var result = new Dictionary<string, WordCardVariant>();
        var posNodes = htmlDoc.DocumentNode.SelectNodes(RegionalVariantsXPath);

        if (posNodes == null) return result;

        foreach (var posNode in posNodes)
        {
            var region = posNode.GetAttributeValue("data-type", "");
            if (string.IsNullOrEmpty(region)) continue;

            var regKey = region.Split('-', 2)[0];
            if (string.IsNullOrEmpty(regKey)) continue;

            result[regKey] = new WordCardVariant
            {
                VariantName = regKey,
                Pronunciations = ParsePronunciations(posNode),
                PartsOfSpeechSections = ParsePartsOfSpeech(posNode),
                AdditionalSections = ParseAdditionalSections(posNode)
            };
        }
        return result;
    }

    private static List<Pronunciation> ParsePronunciations(HtmlNode node)
    {
        return node.SelectNodes(PronunciationXPath)?
            .Select(n => {
                var textNode = n.SelectSingleNode(".//p");
                var phonetic = textNode?.InnerText.Trim() ?? "";
                
                return new Pronunciation {
                    Phonetic = phonetic,
                    Ipa = n.SelectSingleNode(".//input[@value='ipa' and @checked]") != null 
                        ? phonetic 
                        : ""
                };
            })
            .ToList() ?? [];
    }

    private static List<PartOfSpeechEntry> ParsePartsOfSpeech(HtmlNode node)
    {
        return node.SelectNodes(PartsOfSpeechXPath)?
            .Select(posNode => {
                var header = posNode.SelectSingleNode(".//h2");
                return header == null 
                    ? null 
                    : new PartOfSpeechEntry {
                        PartOfSpeech = header.InnerText.Trim(),
                        Definitions = ParseDefinitions(posNode)
                    };
            })
            .Where(entry => entry != null)
            .Select(entry => entry!)
            .ToList() ?? [];
    }

    private static List<Definition> ParseDefinitions(HtmlNode node)
    {
        return node.SelectNodes(DefinitionsXPath)?
            .Select(defNode => new Definition {
                DefinitionText = defNode.SelectSingleNode(".//div[1]")?.InnerText.Trim() ?? "",
                Examples = ParseExamples(defNode),
                Synonyms = ParseSynonyms(defNode),
                Antonyms = ParseAntonyms(defNode)
            })
            .ToList() ?? [];
    }

    private static List<string> ParseExamples(HtmlNode node) =>
        node.SelectNodes(ExamplesXPath)?
            .Select(n => n.InnerText.Trim())
            .ToList() ?? [];

    private static List<string> ParseSynonyms(HtmlNode node) =>
        node.SelectNodes(SynonymsXPath)?
            .Select(n => n.InnerText.Trim())
            .ToList() ?? [];

    private static List<string> ParseAntonyms(HtmlNode node) =>
        node.SelectNodes(AntonymsXPath)?
            .Select(n => n.InnerText.Trim())
            .ToList() ?? [];


    public static List<AdditionalSection?> ParseAdditionalSections(HtmlNode sectionNode)
    {
        var sectionResults = new List<AdditionalSection?>();  
        var paragraphs = sectionNode.SelectNodes(AdditionalSectionsXPath);
        var contentBuilder = new StringBuilder(); 

        var sectionResult = new AdditionalSection();  

        if (paragraphs != null)
        {
            foreach (var paragraph in paragraphs)
            {
                string paragraphText = paragraph.InnerText.Trim();  
                contentBuilder.AppendLine(paragraphText);

                // Извлечение ссылок
                var links = ExtractLinks(paragraph);
                if (links != null && links.Count > 0)
                {
                    foreach (var link in links)
                    {
                        contentBuilder.AppendLine($"Link: {link.Text}, URL: {link.Url}");  
                    }
                }
            }
        }

        var listItems = sectionNode.SelectNodes(".//ul/li");
        if (listItems != null)
        {
            foreach (var listItem in listItems)
            {
                var listLink = listItem.SelectSingleNode(".//a");
                if (listLink != null)
                {
                    contentBuilder.AppendLine($"Item: {listLink.InnerText.Trim()}, URL: {listLink.GetAttributeValue("href", "#")}");
                }
                else
                {
                    contentBuilder.AppendLine($"Item: {listItem.InnerText.Trim()}");
                }
            }
        }

        sectionResult.Content = contentBuilder.ToString();  

        sectionResults.Add(sectionResult);

        return sectionResults;  
    }

    private static List<(string Text, string Url)> ExtractLinks(HtmlNode paragraphNode)
    {
        var links = new List<(string Text, string Url)>();  

        var linkNodes = paragraphNode.SelectNodes(".//a");
        if (linkNodes != null)
        {
            foreach (var linkNode in linkNodes)
            {
                string linkText = linkNode.InnerText.Trim();
                string linkUrl = linkNode.GetAttributeValue("href", "#");
                links.Add((linkText, linkUrl));
            }
        }

        return links;  // Возвращаем список ссылок
    }

}