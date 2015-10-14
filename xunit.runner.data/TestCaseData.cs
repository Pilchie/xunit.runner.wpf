using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace xunit.runner.data
{
    public sealed class TestCaseData
    {
        public string DisplayName { get; set; }
        public string AssemblyPath { get; set; }
        public Dictionary<string, List<string>> TraitMap { get; set; }

        public TestCaseData(string displayName, string assemblyPath, Dictionary<string, List<string>> traitMap)
        {
            DisplayName = displayName;
            AssemblyPath = assemblyPath;
            TraitMap = traitMap;
        }

        public static TestCaseData ReadFrom(BinaryReader reader)
        {
            var formatter = new BinaryFormatter();
            var displayName = reader.ReadString();
            var assemblyPath = reader.ReadString();
            var traitMap = (Dictionary<string, List<string>>)formatter.Deserialize(reader.BaseStream);
            return new TestCaseData(displayName, assemblyPath, traitMap);
        }

        public void WriteTo(BinaryWriter writer)
        {
            var formatter = new BinaryFormatter();
            writer.Write(DisplayName);
            writer.Write(AssemblyPath);
            formatter.Serialize(writer.BaseStream, TraitMap);
        }
    }
}
