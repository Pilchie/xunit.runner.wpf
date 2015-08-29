using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.data
{
    public sealed class TestCaseData
    {
        public string SerializedForm { get; set; }
        public string DisplayName { get; set; }
        public string AssemblyPath { get; set; }
        public Dictionary<string, List<string>> TraitMap { get; set; }

        public TestCaseData(string serializedForm, string displayName, string assemblyPath, Dictionary<string, List<string>> traitMap)
        {
            SerializedForm = serializedForm;
            DisplayName = displayName;
            AssemblyPath = assemblyPath;
            TraitMap = traitMap;
        }

        public static TestCaseData ReadFrom(BinaryReader reader)
        {
            var formatter = new BinaryFormatter();
            var serializedForm = reader.ReadString();
            var displayName = reader.ReadString();
            var assemblyPath = reader.ReadString();
            var traitMap = (Dictionary<string, List<string>>)formatter.Deserialize(reader.BaseStream);
            return new TestCaseData(serializedForm, displayName, assemblyPath, traitMap);
        }

        public void WriteTo(BinaryWriter writer)
        {
            var formatter = new BinaryFormatter();
            writer.Write(SerializedForm);
            writer.Write(DisplayName);
            writer.Write(AssemblyPath);
            formatter.Serialize(writer.BaseStream, TraitMap);
        }
    }
}
