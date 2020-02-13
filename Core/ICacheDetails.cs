using System.Collections.Generic;

namespace PubComp.Caching.Core
{
    public interface ICacheDetails
    {
        bool IsActive { get; }
     
        string Name { get; }
        string Type { get; }

        dynamic Policy { get; }
    }
}