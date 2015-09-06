using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Regex
{
    public class AutomataDFAState
    {
        // marked flag for "subset construction" algorithm
        public bool isMark = false;

        // sets of NFA states (DFA states created from them)
        public Set EpsClos = null;
    }
}
