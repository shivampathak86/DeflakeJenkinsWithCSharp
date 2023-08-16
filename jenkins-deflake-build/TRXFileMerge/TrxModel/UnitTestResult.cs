namespace TRXFileMerge
{
    public class UnitTestResult
    {
        public string ExecutionId { get; set; }
        public string TestId { get; set; }
        public string TestName { get; set; }
        public string ComputerName { get; set; }
        public string Duration { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string TestType { get; set; }
        public string Outcome { get; set; }
        public string TestListId { get; set; }
        public string RelativeResultsDirectory { get; set; }
        public UnitTestResultOutput Output { get; set; }
    }
}
