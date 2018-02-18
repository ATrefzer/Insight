using System;

namespace Insight.Shared.Model
{
    [Serializable]
    public abstract class Id
    {
        public static bool operator ==(Id obj1, Id obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (ReferenceEquals(obj1, null))
            {
                return false;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(Id obj1, Id obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return false;
            }

            if (ReferenceEquals(obj1, null))
            {
                return true;
            }

            return !obj1.Equals(obj2);
        }
    }
}