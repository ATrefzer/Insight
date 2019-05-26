using System;

namespace Insight.Metrics
{
    [Serializable]
    public class LinesOfCode
    {
        public int Blanks;
        public int Code;
        public int Comments;
    }
}