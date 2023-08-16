using System;
using System.Collections.Generic;

namespace TRXFileMerge
{
    public class TestRun
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RunUser { get; set; } 
        public Times Times { get; set; }
        public string Duration
        {
            get
            {
                return (DateTime.Parse(Times.Finish) - DateTime.Parse(Times.Start)).ToString("hh\\:mm\\:ss");
            }
        }
        public List<UnitTestResult> Results { get; set; }
        public List<UnitTest> TestDefinitions { get; set; }
        public List<TestEntry> TestEntries { get; set; }
        public List<TestList> TestLists { get; set; }
        public ResultSummary ResultSummary { get; set; }
    }
}
