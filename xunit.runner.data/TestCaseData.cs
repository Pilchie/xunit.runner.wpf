using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.data
{
    public sealed class TestCaseData
    {
        public string SerializedForm { get; set; }
        public string DisplayName { get; set; }
        public string AssemblyPath { get; set; }

        public TestCaseData(string serializedForm, string displayName, string assemblyPath)
        {
            SerializedForm = serializedForm;
            DisplayName = displayName;
            AssemblyPath = assemblyPath;
        }

        public static TestCaseData ReadFrom(BinaryReader reader)
        {
            var serializedForm = reader.ReadString();
            var displayName = reader.ReadString();
            var assemblyPath = reader.ReadString();
            return new TestCaseData(serializedForm, displayName, assemblyPath);
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(SerializedForm);
            writer.Write(DisplayName);
            writer.Write(AssemblyPath);
        }
    }
}
