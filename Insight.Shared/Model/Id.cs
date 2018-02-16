using System;

namespace Insight.Shared.Model
{
    [Serializable]
    public abstract class Id
    {
        public static bool operator ==(Id obj1, Id obj2)
        {
             if (ReferenceEquals(obj1, null) ||
                 ReferenceEquals(obj2, null))
            {
                return false;
            }
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Id obj1, Id obj2)
        {
            if (ReferenceEquals(obj1, null) ||
                ReferenceEquals(obj2, null))
            {
                return false;
            }
            return !obj1.Equals(obj2);
        }
    }
}