﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Simple1C.Impl.Helpers;

namespace Generator
{
    public class CsProjectFileUpdater
    {
        private const string namespaceName = "http://schemas.microsoft.com/developer/msbuild/2003";
        private readonly string csprojFilePath;
        private readonly string autogeneratedSourcesPath;
        private readonly XmlNamespaceManager xmlNamespaceManager;

        public CsProjectFileUpdater(string csprojFilePath, string autogeneratedSourcesPath)
        {
            this.csprojFilePath = csprojFilePath;
            this.autogeneratedSourcesPath = autogeneratedSourcesPath;
            xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("ns", namespaceName);
        }

        public void Update()
        {
            XDocument xProjDocument;
            using (var fs = File.Open(csprojFilePath, FileMode.Open, FileAccess.Read))
                xProjDocument = XDocument.Load(fs);
            var autogeneratedCodeRelativePath = PathHelpers.GetRelativePath(csprojFilePath, autogeneratedSourcesPath);
            const string xpathFormat = "/ns:Project/ns:ItemGroup/ns:Compile[starts-with(@Include, \"{0}\")]";
            var xpath = string.Format(xpathFormat, autogeneratedCodeRelativePath);
            var csprojItems = xProjDocument
                .XPathSelectElements(xpath, xmlNamespaceManager)
                .Select(x => new
                {
                    item = x,
                    include = x.Attribute("Include").Value
                })
                .ToArray();
            var currentFiles = Directory.Exists(autogeneratedSourcesPath)
                ? Directory.GetFiles(autogeneratedSourcesPath, "*.cs", SearchOption.AllDirectories)
                    .ToSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();
            var csprojDirectoryPath = PathHelpers.GetDirectoryName(csprojFilePath);
            var actualCsprojItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            XElement lastExistingCompileItem = null;
            foreach (var item in csprojItems)
            {
                if (currentFiles.Contains(Path.Combine(csprojDirectoryPath, item.include)))
                {
                    actualCsprojItems.Add(item.include);
                    lastExistingCompileItem = item.item;
                }
                else
                    item.item.Remove();
            }
            XElement itemGroup = null;
            if (lastExistingCompileItem == null)
            {
                var xProj = xProjDocument.Element(XName.Get("Project", namespaceName));
                if (xProj == null)
                    throw new InvalidOperationException("assertion failure");
                itemGroup = xProj.Elements(XName.Get("ItemGroup", namespaceName)).FirstOrDefault();
                if (itemGroup == null)
                {
                    itemGroup = new XElement(XName.Get("ItemGroup", namespaceName));
                    xProj.Add(itemGroup);
                }
            }
            var csprojDirectoryPathLength = PathHelpers.IncludeTrailingDirectorySlash(csprojDirectoryPath).Length;
            foreach (var currentFile in currentFiles)
            {
                var relativePath = currentFile.Substring(csprojDirectoryPathLength);
                if (actualCsprojItems.Contains(relativePath))
                    continue;
                var newCompileItem = new XElement(XName.Get("Compile", namespaceName),
                    new XAttribute("Include", relativePath));
                if (lastExistingCompileItem != null)
                    lastExistingCompileItem.AddAfterSelf(newCompileItem);
                else
                    itemGroup.Add(newCompileItem);
                lastExistingCompileItem = newCompileItem;
            }
            using (var fs = File.Open(csprojFilePath, FileMode.Truncate, FileAccess.Write))
                xProjDocument.Save(fs);
        }
    }
}