using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Regex
{
    public class AutomataState
    {
        // static part of class
        private static int globalIdCounter = 0;

        // istance part of class
        private int instance_stateId = -1;
        private List<Tuple<string, AutomataState>> transitions = new List<Tuple<string,AutomataState>>();
        private bool isFinalstate = false;

        /*** Restart the static counter for automata state numeration ***/
        public static void resetStatesGlobalCounter()
        {
            globalIdCounter = 0;
        }

        #region Properties
        /*** Only Get the id of the state ***/
        public int StateID
        {
            get { return this.instance_stateId; }
        }

        /*** Get/ Set the property tha t is or not an accepting state of the automata ***/
        public bool IsFinalState
        {
            get { return this.isFinalstate; }
            set { this.isFinalstate = value; }
        }
        #endregion

        /*** Constructor method that create a new istance of the class that have the next id available ***/
        public AutomataState()
        {
            this.instance_stateId = globalIdCounter++;
        }

        /*** add the transition between the start state "this" and the end state "endState", using 'trans_char', to the list of outgoing transition of this ***/
        public void addOutgoingTransition(string trans_char, AutomataState endState)
        {
            Tuple<string, AutomataState> trans = new Tuple<string, AutomataState>(trans_char, endState);
            transitions.Add(trans);
        }

        /*** Return the list of all first element of transitions (list of tuple), so the list of all character that can cause a transition from this to some state ***/
        public List<string> getAllTransitionsChars()
        {
            List<string> allCharsList = new List<string>();

            for (int i = 0; i < transitions.Count; i++)
            {
                string tmp_s = transitions[i].Item1;
                if (!allCharsList.Contains(tmp_s))
                    allCharsList.Add(tmp_s);
            }

                return allCharsList;
        }

        /*** Return the "toState" of the first transition from "this" to "toState" with "transChr" ***/
        public AutomataState getExactTransition(string transChr)
        {
            foreach (Tuple<string,AutomataState> t in transitions)
            {
                if (t.Item1 == transChr)
                {
                    return t.Item2;
                }
            }

            return null;
        }

        /*** Return the list of all destination states of the automata which the transition is caused by "trans_string" ***/
        public List<AutomataState> getAllDestStatesWithStringTransaction(string trans_string)
        {
            List<AutomataState> allStatesWithThoseTrans = new List<AutomataState>();
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].Item1 == trans_string && !allStatesWithThoseTrans.Contains(transitions[i].Item2))
                {
                    allStatesWithThoseTrans.Add(transitions[i].Item2);
                }
            }

            return allStatesWithThoseTrans;
        }

        /*** Replace the endstate (with "newState") of which transitions s.t. endstate is equal to "oldState" ***/
        public int replaceEndstateForTransitions(AutomataState oldState, AutomataState newState)
        {
            int count = 0;
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].Item2 == oldState)
                {
                    transitions[i] = new Tuple<string, AutomataState>(transitions[i].Item1, newState);
                    count++;
                }
            }
            return count;
        }

        /*** verify if exists a closed "dead" state ***/
        public bool isAClosedState()
        {
            if (isFinalstate || transitions.Count == 0)
            {
                return false;
            }
            
            foreach (Tuple<string,AutomataState> t in transitions)
            {
                if (!t.Item2.Equals(this))
                {
                    return false;
                }
            }

            return true;
        }

        /*** @overrides ToString() method ***/
        public override string ToString()
        {
            string s = "s" + this.StateID.ToString();
            if (this.IsFinalState)
            {
                // "{Si}" notation for final/accept states Si
                s = "{" + s + "}";
            }
            return s;
        }

    }
}
