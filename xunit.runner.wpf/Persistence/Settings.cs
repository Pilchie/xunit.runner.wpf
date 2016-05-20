using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;

namespace Xunit.Runner.Wpf.Persistence
{
    internal sealed class Settings
    {
        private const string SettingsFileName = "settings.xml";

        private const string RecentAssembliesElementName = "recent_assemblies";
        private const string RecentAssemblyElementName = "recent_assembly";
        private const string SettingsElementName = "settings";
        private const string VersionAttributeName = "version";

        private const int MaxRecentAssemblies = 10;

        private static readonly Version s_latestVersion = new Version(1, 0, 0, 0);

        private List<string> recentAssemblies;

        private Settings()
        {
            recentAssemblies = new List<string>();
        }

        public void AddRecentAssembly(string filePath)
        {
            for (int i = recentAssemblies.Count - 1; i > 0; i--)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(recentAssemblies[i], filePath))
                {
                    recentAssemblies.RemoveAt(i);
                }
            }

            recentAssemblies.Insert(0, filePath);

            if (recentAssemblies.Count > MaxRecentAssemblies)
            {
                recentAssemblies.RemoveRange(MaxRecentAssemblies - 1, recentAssemblies.Count - MaxRecentAssemblies);
            }
        }

        public ImmutableArray<string> GetRecentAssemblies()
        {
            return this.recentAssemblies.ToImmutableArray();
        }

        public void Save()
        {
            using (var xmlFile = Storage.CreateXmlFile(SettingsFileName))
            {
                var xml = new XElement(SettingsElementName, new XAttribute(VersionAttributeName, s_latestVersion));

                if (this.recentAssemblies.Count > 0)
                {
                    var recentAssembliesElement = new XElement(RecentAssembliesElementName);
                    foreach (var recentAssembly in this.recentAssemblies)
                    {
                        recentAssembliesElement.Add(new XElement(RecentAssemblyElementName, recentAssembly));
                    }

                    xml.Add(recentAssembliesElement);
                }

                xml.Save(xmlFile);
            }
        }

        public static Settings Load()
        {
            using (var xmlFile = Storage.OpenXmlFile(SettingsFileName))
            {
                var settings = new Settings();

                if (xmlFile == null || xmlFile.EOF)
                {
                    return settings;
                }

                try
                {
                    xmlFile.MoveToContent();
                }
                catch (XmlException)
                {
                    return settings;
                }

                var xml = XElement.Load(xmlFile);

                var recentAssembliesElement = xml.Element(RecentAssembliesElementName);
                if (recentAssembliesElement != null)
                {
                    var recentAssemblyElements = recentAssembliesElement.Elements(RecentAssemblyElementName);
                    foreach (var recentAssemblyElement in recentAssemblyElements)
                    {
                        var filePath = (string)recentAssemblyElement;
                        settings.AddRecentAssembly(filePath);
                    }
                }

                return settings;
            }
        }
    }
}
