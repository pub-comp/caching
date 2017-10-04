namespace PubComp.Caching.Core
{
    internal struct CacheName
    {
        public readonly string Name;
        public readonly string Prefix;
        public readonly bool DoEnableAnySuffix;

        public CacheName(string name)
        {
            Name = string.IsNullOrEmpty(name) ? "*" : name;
            
            if (Name.EndsWith("*"))
            {
                Prefix = Name.Substring(0, Name.Length - 1);
                DoEnableAnySuffix = true;
            }
            else
            {
                Prefix = Name;
                DoEnableAnySuffix = false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is CacheName == false)
                return false;

            var other = (CacheName)obj;
            return (other.Prefix == Prefix && other.DoEnableAnySuffix == DoEnableAnySuffix);
        }

        public override int GetHashCode()
        {
            return (Prefix ?? string.Empty).GetHashCode() ^ DoEnableAnySuffix.GetHashCode();
        }

        public int GetMatchLevel(string cacheName)
        {
            if (Prefix == cacheName)
                return Prefix.Length;

            if (DoEnableAnySuffix && cacheName.StartsWith(Prefix))
                return Prefix.Length;

            return 0;
        }
    }
}
