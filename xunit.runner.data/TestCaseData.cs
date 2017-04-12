using System.Collections.Generic;
using System.IO;

namespace Xunit.Runner.Data
{
    public sealed class TestCaseData
    {
        public string DisplayName { get; set; }
        public string UniqueID { get; set; }
        public string SkipReason { get; set; }
        public string AssemblyPath { get; set; }
        public Dictionary<string, List<string>> TraitMap { get; set; }

        public TestCaseData(string displayName, string uniqueID, string skipReason, string assemblyPath, Dictionary<string, List<string>> traitMap)
        {
            DisplayName = displayName;
            UniqueID = uniqueID;
            SkipReason = skipReason;
            AssemblyPath = assemblyPath;
            TraitMap = traitMap;
        }

        public static TestCaseData ReadFrom(BinaryReader reader)
        {
            var displayName = reader.ReadString();
            var uniqueID = reader.ReadString();
            var skipReason = reader.ReadString();
            var assemblyPath = reader.ReadString();
            var count = reader.ReadInt32();
            var traitMap = new Dictionary<string, List<string>>(count);

            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var valueCount = reader.ReadInt32();
                var values = new List<string>(valueCount);

                for (int j = 0; j < valueCount; j++)
                {
                    values.Add(reader.ReadString());
                }

                traitMap.Add(key, values);
            }

            return new TestCaseData(displayName, uniqueID, skipReason, assemblyPath, traitMap);
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(DisplayName);
            writer.Write(UniqueID);
            writer.Write(SkipReason ?? string.Empty);
            writer.Write(AssemblyPath);
            writer.Write(TraitMap.Count);

            foreach (var pair in TraitMap)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.Count);

                foreach (var value in pair.Value)
                {
                    writer.Write(value);
                }
            }
        }
    }
}
