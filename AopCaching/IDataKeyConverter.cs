using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.AopCaching
{
    public interface IDataKeyConverter<TKey, TData>
    {
        TKey GetKey(TData data);
    }
}
