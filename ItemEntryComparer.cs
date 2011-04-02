using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpClient
{
    class ItemEntryComparer : IEqualityComparer<ItemType>
    {
        public bool Equals(ItemType i1, ItemType i2)
        {
            if (i1.quality == i2.quality && i1.type == i2.type)
            {
                if (((i1.has_sockets == false) && (i2.has_sockets == false || i2.sockets == uint.MaxValue)) || i1.sockets == i2.sockets)
                    return true;
            }
            if (i1.type == "rv1")
                return true;
            return false;
        }


        public int GetHashCode(ItemType ix)
        {
            uint hCode = ix.sockets;
            return hCode.GetHashCode();
        }
    }
}
