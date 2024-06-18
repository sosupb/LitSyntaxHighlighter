using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace LitSyntaxHighlighter.Utility
{
    internal class SnapshotSpanComparer : IComparer<SnapshotSpan>
    {
        public int Compare(SnapshotSpan a, SnapshotSpan b)
        {
           if(a.Snapshot.Version.VersionNumber < b.Snapshot.Version.VersionNumber) 
                return -1;
           else if(a.Snapshot.Version.VersionNumber > b.Snapshot.Version.VersionNumber)
                return 1;
           else return a.Start.Position - b.Start.Position;
        }
    }
}
