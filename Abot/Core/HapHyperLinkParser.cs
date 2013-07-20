﻿using Abot.Poco;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;

namespace Abot.Core
{
    /// <summary>
    /// Parser that uses Html Agility Pack http://htmlagilitypack.codeplex.com/ to parse page links
    /// </summary>
    public class HapHyperLinkParser : HyperLinkParser
    {
        Func<string, string> _cleanURLFunc;
        protected override string ParserType
        {
            get { return "HtmlAgilityPack"; }
        }
        public HapHyperLinkParser()
        {
        }
        public HapHyperLinkParser(Func<string, string> cleanURLFunc)
        {
            _cleanURLFunc = cleanURLFunc;
        }
        protected override IEnumerable<string> GetHrefValues(CrawledPage crawledPage)
        {
            List<string> hrefValues = new List<string>();
            if (1 == 2)
            {
                HtmlNode robotsNode = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//meta[@name='robots']");
                if (robotsNode != null)
                {
                    var robotContent = robotsNode.GetAttributeValue("content", "");
                    // TODO: write desc somewhere
                    if (robotContent != null && robotContent.ToLower() == "nofollow")
                    {
                        return hrefValues;
                    }
                }
            }
            HtmlNodeCollection aTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//a[@href]");
            HtmlNodeCollection areaTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//area[@href]");

            hrefValues.AddRange(GetLinks(aTags));
            hrefValues.AddRange(GetLinks(areaTags));

            return hrefValues;
        }

        protected override string GetBaseHrefValue(CrawledPage crawledPage)
        {
            string hrefValue = "";
            HtmlNode node = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//base");

            //Must use node.InnerHtml instead of node.InnerText since "aaa<br />bbb" will be returned as "aaabbb"
            if (node != null)
                hrefValue = node.GetAttributeValue("href", "").Trim();

            return hrefValue;
        }

        private List<string> GetLinks(HtmlNodeCollection nodes)
        {
            List<string> hrefs = new List<string>();

            if (nodes == null)
                return hrefs;

            string hrefValue = "";
            foreach (HtmlNode node in nodes)
            {
                hrefValue = _cleanURLFunc != null ? _cleanURLFunc(node.Attributes["href"].Value) : node.Attributes["href"].Value;
                if (!string.IsNullOrWhiteSpace(hrefValue))
                    hrefs.Add(hrefValue);
            }

            return hrefs;
        }
    }
}