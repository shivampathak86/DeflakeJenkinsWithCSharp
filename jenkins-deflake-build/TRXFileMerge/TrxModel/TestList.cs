using System;

namespace TRXFileMerge
{
    public class TestList
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is TestList))
                return false;
            return (obj as TestList).Id == Id;
        }
        public override int GetHashCode()
        {
            return Guid.Parse(Id).GetHashCode();
        }
    }
}
