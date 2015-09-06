using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Regex
{
    public class SetHashtable : Hashtable
    {
        public void Add(Set key, AutomataState mapTo)
        {
            Set set = null;
            if (base.Contains(key))
            {
                set = (Set)base[key];
            }
            else
            {
                set = new Set();
            }

            set.addElement(mapTo);
            base[key] = set;
        }

        public Set this[Set key]
        {
            get
            {
                return this[key];
            }
            set
            {
                this.Add(key, value);
            }
        }
    }
}
