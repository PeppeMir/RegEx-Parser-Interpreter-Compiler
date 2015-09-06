using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ICompilableProject;
using Microsoft.CSharp;

namespace Regex
{
    public class RExpr
    {
        /*** global generics ***/
        private RExpr_Parser RExpr_parser = new RExpr_Parser();
        private Hashtable DFAStatesTable = new Hashtable();     // hashtable for DFAmin states
        private Main parent = null;

        /*** vars for compiling ***/
        private AutomataState Sz_DFAmin = null;
        private Assembly compilationBlock = null;
        private ICompilable compilationResult = null;
        private bool isCompiled = false;

        /*** Define a priority system for some chars ***/
        internal class CharPriority
        {
            public const int
                P_CIRCLE_LPAREN = 0,
                P_OR = 1,
                P_CONC = 2,
                P_UNARY = 3,
                MAX_PRIORITY = 5;
        }

        /*** constructor method with Main form as parent ***/
        public RExpr(Main p)
        {
            parent = p;
        }

        /*** Given a string contains a regular expression, perform the parse of it. If the parse is positive, produces a Minimum DFA model. Otherwise throw SyntaxException. ***/
        public StringBuilder ParseRegexAndCreateAutomata(Stopwatch timer, string regex)
        {
            StringBuilder statistics_s = new StringBuilder();
            this.isCompiled = false;

            // set the global start id for states to 0
            AutomataState.resetStatesGlobalCounter();

            // reset hashtable for DFA
            DFAStatesTable.Clear();

            // start timing for parsing
            timer.Reset();
            timer.Start();

            // invoke the parser (it can generate SyntaxException) and set the parsed string as Text of "label_formatted_regex" in Main form
            string parsed_regex = RExpr_parser.ParseRExpr(regex);

            // arrange parsed regex string applying postfixing tecnique
            string arranged_regex = ArrangeRegex(parsed_regex);

            /*** 1) Create a NFA starting from re-arranged parsed regex ***/
            AutomataState Szero_NFA = BuildNFA(arranged_regex);

            /*** 2) Create a DFA starting from NFA start state ***/
            AutomataState.resetStatesGlobalCounter();
            AutomataState Szero_DFA = NFAtoDFA(Szero_NFA);

            /*** 3) Create min DFA from DFA start state ***/
            AutomataState SzeroDFAmin = MinimizeDFA(Szero_DFA);

            Sz_DFAmin = SzeroDFAmin;

            // stop timing for parsing
            timer.Stop();

            return statistics_s;
        }

        #region automataRegion
        /*** Create a minimized version of the DFA (called DFAmin) ***/
        private AutomataState MinimizeDFA(AutomataState Szero_DFA)
        {
            // 1) partition the DFS states (filling 2nd and 3rd arguments)
            Set transSymbols = new Set();
            Set DFAstates = new Set();
            FillStatesAndSymbols(DFAstates, transSymbols, Szero_DFA);
            ArrayList arr = createDFASets(DFAstates, transSymbols);

            // 2) through all the groups and select a pivot for each group.
            AutomataState stateStartReducedDfa = null;
            foreach (object objGroup in arr)
            {
                Set setGroup = (Set)objGroup;

                // check final states SzeroDFA in the group
                bool finalStInGroup = ExistFinalStateInSet(setGroup);
                bool startDFAinGroup = setGroup.Contains(Szero_DFA);

                AutomataState pivotState = (AutomataState)setGroup[0];

                if (startDFAinGroup)
                {
                    stateStartReducedDfa = pivotState;
                }

                if (finalStInGroup)
                {
                    pivotState.IsFinalState = true;
                }

                // if there are only once element, continue loop
                if (setGroup.NumOfElement == 1)
                {
                    continue;
                }

                // 3) [innerLoop] remove the pivot from its group and replace all the references of the remaining member of the group
                setGroup.removeElFromSet(pivotState);

                // state to be replaced with the group of pivot
                AutomataState stateToBeReplaced = null;   

                int numOfReplace = 0;
                foreach (object objStateToReplaced in setGroup)
                {
                    stateToBeReplaced = (AutomataState)objStateToReplaced;

                    DFAstates.removeElFromSet(stateToBeReplaced);

                    foreach (object objState in DFAstates)
                    {
                        numOfReplace = numOfReplace + ((AutomataState)objState).replaceEndstateForTransitions(stateToBeReplaced, pivotState);
                    }
                }
            } 

            //  4) finally can remove all "close states"
            for (int n = 0; n < DFAstates.Count; n++)
            {
                AutomataState state = (AutomataState)DFAstates[n];
                if (state.isAClosedState())
                {
                    DFAstates.RemoveAt(n);
                    continue;
                }
            }

            return stateStartReducedDfa;
        }

        /*** Build states and transions of DFA starting from the start state of NFA, using subset construction algorithm. Return the start state of DFA.***/
        private AutomataState NFAtoDFA(AutomataState Szero_NFA)
        {
            // 1) fill the 2nd and 3rd arguments 
            Set setAllSym = new Set();
            Set setAllState = new Set();
            FillStatesAndSymbols(setAllState, setAllSym, Szero_NFA);
            setAllSym.removeElFromSet(RExpr_Parser.Tokens.EPSILON);

            Set epsClos = getEpsClosureFromState(Szero_NFA);
            AutomataState Szero_DFA = new AutomataState(); 

            if (ExistFinalStateInSet(epsClos))
            {
                // set the start state as also final state
                Szero_DFA.IsFinalState = true;
            }

            CreateNewDFAState(epsClos, Szero_DFA);

            // 2) Compute "subset construction" algorithm
            string transChar = "";
            AutomataState T = null, U = null;
            Set epsClosureT = null, mvs = null;

            // main loop (breakable with null checking)
            while (true)
            {
                // until exists an unmarked state T
                T = getUnmarkedState();
                if (T == null)
                    break;

                // "Mark" this state
                markState(T);   

                // get the EpsClosure of T
                epsClosureT = getEpsClosure(T);

                // for each symbol in the set of all symbols (inner for of the "construction set" algorithm)
                foreach (object obj in setAllSym)
                {
                    transChar = obj.ToString();
                    mvs = getMoves(transChar, epsClosureT);

                    if (mvs.NumOfElement != 0)
                    {
                        // U = E-clos(move(T,inputSym))
                        epsClos = getEpsClosureFromStateSet(mvs);
                        U = FindDfaStateByEclosure(epsClos);

                        // if in this set of NFA's states exists a final state, the set is a final state of DFA
                        if (U == null)
                        {
                            U = new AutomataState();
                            if (ExistFinalStateInSet(epsClos))
                            {
                                U.IsFinalState = true;
                            }

                            // add a new (unmarked) state
                            CreateNewDFAState(epsClos, U);  
                        }

                        // add to DFA transitions (T,transChar) -> U
                        T.addOutgoingTransition(transChar, U);
                    }
                }
            }

            return Szero_DFA;
        }

        /*** Build states and transions of NFA by Thompson's construction and return the start state of NFA. ***/
        private AutomataState BuildNFA(string arranged_regex)
        {
            bool detected_escape = false;
            Stack<Tuple<AutomataState, AutomataState>> NFAstack = new Stack<Tuple<AutomataState, AutomataState>>();

            // each of them representing set-of-states expression to built
            Tuple<AutomataState, AutomataState> A, B, FINAL, NEW = null;  

            foreach (char chr in arranged_regex)
            {
                // escape case of Thompson's constructions
                if (!detected_escape && chr == RExpr_Parser.Tokens.TOK_ESCAPE)
                {
                    detected_escape = true;
                    continue;
                }

                if (detected_escape)
                {
                    NEW = new Tuple<AutomataState, AutomataState>(new AutomataState(), new AutomataState());
                    NEW.Item1.addOutgoingTransition(chr.ToString(), NEW.Item2);

                    NFAstack.Push(NEW);

                    detected_escape = false;
                    continue;
                }

                // performs basic Thompson's construction from the string
                switch (chr)
                {
                    case RExpr_Parser.Tokens.TOK_KLEENE:  // apply "A*" rule
                        {
                            A = NFAstack.Pop();
                            NEW = new Tuple<AutomataState, AutomataState>(new AutomataState(), new AutomataState());

                            A.Item2.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, A.Item1);
                            A.Item2.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, NEW.Item2);

                            NEW.Item1.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, A.Item1);
                            NEW.Item1.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, NEW.Item2);

                            NFAstack.Push(NEW);

                            break;
                        }

                    case RExpr_Parser.Tokens.TOK_OR:  // apply "A|B" rule
                        {
                            B = NFAstack.Pop();
                            A = NFAstack.Pop();

                            NEW = new Tuple<AutomataState, AutomataState>(new AutomataState(), new AutomataState());

                            A.Item2.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, NEW.Item2);
                            B.Item2.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, NEW.Item2);

                            NEW.Item1.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, A.Item1);
                            NEW.Item1.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, B.Item1);

                            NFAstack.Push(NEW);

                            break;
                        }

                    case RExpr_Parser.Tokens.TOK_CONCAT:  // apply "AB" rule
                        {
                            B = NFAstack.Pop();
                            A = NFAstack.Pop();

                            A.Item2.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, B.Item1);

                            NEW = new Tuple<AutomataState, AutomataState>(A.Item1, B.Item2);

                            NFAstack.Push(NEW);

                            break;
                        }

                    case RExpr_Parser.Tokens.TOK_ZERO_ONE:  // apply "A?" rule (can see as "A | empty")
                        {
                            A = NFAstack.Pop();
                            NEW = new Tuple<AutomataState, AutomataState>(new AutomataState(), new AutomataState());

                            NEW.Item1.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, A.Item1);
                            NEW.Item1.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, NEW.Item2);
                            A.Item2.addOutgoingTransition(RExpr_Parser.Tokens.EPSILON, NEW.Item2);

                            NFAstack.Push(NEW);

                            break;
                        }

                    case RExpr_Parser.Tokens.TOK_SOMECHAR:  // apply "." (any char of the alphabet) rule
                        {
                            NEW = new Tuple<AutomataState, AutomataState>(new AutomataState(), new AutomataState());
                            NEW.Item1.addOutgoingTransition(RExpr_Parser.Tokens.TOK_SOMECHAR_TRANS, NEW.Item2);
                            NFAstack.Push(NEW);
                            break;
                        }

                    default:        // apply base simple case rule
                        {
                            NEW = new Tuple<AutomataState, AutomataState>(new AutomataState(), new AutomataState());
                            NEW.Item1.addOutgoingTransition(chr.ToString(), NEW.Item2);

                            NFAstack.Push(NEW);

                            break;
                        }

                } // end switch

            } 

            FINAL = NFAstack.Pop();
            FINAL.Item2.IsFinalState = true;

            return FINAL.Item1; // return the START state (Item1) of the
        }
        #endregion

        #region compilationRegion

        /*** Transform the DFAmin in a equivalent C# code-string (successivly compiled) ***/
        public string CompileAutomata()
        {
            StringBuilder generateClass = new StringBuilder();
            this.isCompiled = true;

            // 1) get all states and symbol of DFAmin
            Set allStates = new Set();
            Set inputSymbols = new Set();
            FillStatesAndSymbols(allStates, inputSymbols, Sz_DFAmin);
            
            // 2) generating a string that contains the translation of DFAmin in C# code (twice recognize the same set of strings)
            generateClass.AppendLine("using System;");
            generateClass.AppendLine("using System.Collections.Generic;");
            generateClass.AppendLine("using ICompilableProject;");      // needs to adding .dll with ICompilable interface
            generateClass.AppendLine("");
            generateClass.AppendLine("namespace Regex");    
            generateClass.AppendLine("{");
            generateClass.AppendLine("   public class CompiledCodeClass: ICompilable");
            generateClass.AppendLine("   {");
            generateClass.AppendLine("      public bool IsMatch(string str)");
            generateClass.AppendLine("      {");
            generateClass.Append("         int[] finalStates = new int[]{");

            // detect final states
            int countFinalStates = 0;
            foreach (AutomataState st in allStates)
            {
                if (st.IsFinalState)
                {
                    countFinalStates++;
                }
            }

            foreach (AutomataState st in allStates)
            {
                if (st.IsFinalState)
                {
                    generateClass.Append(st.StateID.ToString());
                    if (countFinalStates - 1 > 0)
                    {
                        generateClass.Append(", ");
                        countFinalStates--;
                    }
                }
            }
            generateClass.Append("};");
            generateClass.AppendLine("");
            generateClass.AppendLine("         int curState = " + Sz_DFAmin.StateID + ";");
            generateClass.AppendLine("         char curChar;");
            generateClass.AppendLine("         for(int i = 0; i < str.Length; i++)");
            generateClass.AppendLine("         {");
            generateClass.AppendLine("            curChar = str[i];");

            // generate conditions correspondent to DFAmin transitions
            foreach (AutomataState st in allStates)
            {
                foreach (string chr in inputSymbols)
                {
                    AutomataState nextS = ((AutomataState)st).getExactTransition(chr);
                    if (nextS == null || chr == RExpr_Parser.Tokens.TOK_SOMECHAR_TRANS) continue;
                    generateClass.AppendLine("            if(curState == " + st.StateID + " && curChar == '" + chr + "'){curState = " + nextS.StateID + "; continue;}");
                }

                AutomataState dotS = ((AutomataState)st).getExactTransition(RExpr_Parser.Tokens.TOK_SOMECHAR_TRANS);
                if (dotS == null) continue;
                generateClass.AppendLine("            if(curState == " + st.StateID + "){curState = " + dotS.StateID + "; continue;}");
            }

            generateClass.AppendLine("");
            generateClass.AppendLine("            // if there are no matched transitions from curState, we have a mismatch");
            generateClass.AppendLine("            return false;");
            generateClass.AppendLine("         }");
            generateClass.AppendLine("         return (new List<int>(finalStates)).Contains(curState);");
            generateClass.AppendLine("      }");
            generateClass.AppendLine("   }");
            generateClass.AppendLine("}");

            // compile generated string-class (generate "compilationBlock" assembly code)
            compileCSharpCode(generateClass.ToString());

            return generateClass.ToString();
        }

        /*** Given in input a C# code program-string, compile it producing the assembly in "compilationBlock" global variable ***/
        private bool compileCSharpCode(string cSharpCode)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;

            // refers .dll that contain the definition of the interface "ICompilable"
            parameters.ReferencedAssemblies.Add("ICompilableProject.dll");

            // run compiler producing assembly
            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, cSharpCode);

            // check compilation errors
            if (!results.Errors.HasErrors)
            {
                this.compilationBlock = results.CompiledAssembly;
                foreach (Type type in compilationBlock.GetTypes())
                {

                    if (!type.IsClass || type.IsNotPublic) 
                        continue;

                    // getting interfaces
                    Type[] interfaces = type.GetInterfaces();

                    if (((IList)interfaces).Contains(typeof(ICompilable)))
                    {
                        object obj = Activator.CreateInstance(type);
                        compilationResult = (ICompilable)obj;
                    }
                }
            }
            else
            {
                // if has errors, enumerate its
                string allErrors = "";
                for (int i = 0; i < results.Errors.Count; i++)
                {
                    allErrors += results.Errors[i] + "\n";
                }

                throw new CompilationException(allErrors);
            }

            return this.compilationBlock != null;
        }

        /*** Run the assembly code in "compilationBlock" (result of C# code compilation) ***/
        public bool runCompiledCode(Stopwatch timer, string input)
        {
            // start timer to calculate execution time
            timer.Reset();
            timer.Start();

            // compute matching on compilated automata
            bool res = compilationResult.IsMatch(input);

            // stop timing
            timer.Stop();

            return res;
        }

        #endregion

        #region UtilityMethods

        /*** check if the transition from "currState" is caused by "." ***/
        private static bool checkSomeChar(AutomataState currState)
        {
            return currState.getExactTransition(RExpr_Parser.Tokens.TOK_SOMECHAR_TRANS) != null;
        }

        /*** Find in which set of the "arrayOfSet" the state "state" is contained. Return null if there are no set that contains "state". ***/
        private Set findInWhichSet(ArrayList arrayOfSet, AutomataState state)
        {
            foreach (object objSet in arrayOfSet)
            {
                Set set = (Set)objSet;

                if (set.Contains(state))
                {
                    return set;
                }
            }

            return null;
        }

        /*** Given the DFA set of states, generate the DFAmin set of states. ***/
        private ArrayList createDFASets(Set DFAset, Set chrSet)
        {
            // array of all set of DFA states
            ArrayList arr = new ArrayList();

            // to tracks transitions
            SetHashtable hashSet = new SetHashtable();

            // to track states
            Set emtySet = new Set();
            Set finalStates = new Set();
            Set otherStates = new Set();

            // 1) divide on final and non-final states criteria
            foreach (object objState in DFAset)
            {
                AutomataState state = (AutomataState)objState;
                if (!state.IsFinalState)
                {
                    otherStates.addElement(state);
                }
                else
                {
                    finalStates.addElement(state);
                }
            }

            // 2) fill arr with states (final + eventually other)
            if (otherStates.NumOfElement > 0)
            {
                arr.Add(otherStates);
            }

            arr.Add(finalStates);

            // 3) iterate on set of symbols
            IEnumerator chrIterator = chrSet.GetEnumerator();
            chrIterator.Reset();
            while (chrIterator.MoveNext())
            {
                // get transition symbol
                string sym = chrIterator.Current.ToString();

                int i = 0;
                while (i < arr.Count)
                {
                    Set setToBePartitioned = (Set)arr[i++];

                    // cannot divide a set with once or less elements
                    if (setToBePartitioned.NumOfElement == 0 || setToBePartitioned.NumOfElement == 1)
                    {
                        continue;
                    }

                    foreach (object objState in setToBePartitioned)
                    {
                        AutomataState state = (AutomataState)objState;
                        List<AutomataState> statesList = state.getAllDestStatesWithStringTransaction(sym.ToString());

                        if (statesList != null && statesList.Count > 0)
                        {
                            // could be only once
                            AutomataState stateTransionTo = statesList[0];

                            // get the set that contains "stateTransitionTo"
                            Set foundedSet = findInWhichSet(arr, stateTransionTo);
                            hashSet.Add(foundedSet, state);
                        }
                        else
                        {
                            // if there are no transition, add a transition to emptyset
                            hashSet.Add(emtySet, state);
                        }
                    }

                    if (hashSet.Count > 1)  // means some states transition into different groups
                    {
                        arr.Remove(setToBePartitioned);
                        foreach (DictionaryEntry de in hashSet)
                        {
                            Set setValue = (Set)de.Value;
                            arr.Add(setValue);
                        }

                        // restart from begin (reset to initial loop state)
                        i = 0;
                        chrIterator.Reset();
                    }

                    hashSet.Clear();
                }
            }

            return arr;
        }

        /*** Return the set of all ToStates s.t. exist a move with "trans_chr" ***/
        private Set getMoves(string trans_chr, Set stateSet)
        {
            Set set = new Set();
            AutomataState state = null;
            foreach (object obj in stateSet)
            {
                state = (AutomataState)obj;
                Set setMove = getMoves(trans_chr, state);

                // perform a union between the sets (removing duplicates)
                set.setUnion(setMove);
            }

            return set;
        }

        /*** Get all the states moves as above but starting from a single state  ***/
        private Set getMoves(string trans_chr, AutomataState state)
        {
            Set s = new Set();
            List<AutomataState> lTrns = state.getAllDestStatesWithStringTransaction(trans_chr);

            if (lTrns != null && lTrns.Count > 0)
            {
                s.addElementRange(lTrns.ToArray());
            }

            return s;

        }

        /*** Check if exist at least a final state in the parameter set ***/
        private bool ExistFinalStateInSet(Set set)
        {
            AutomataState state = null;
            foreach (object objState in set)
            {
                state = (AutomataState)objState;

                if (state.IsFinalState)
                {
                    return true;
                }
            }

            return false;
        }

        /*** Finds all states that are "toStates" by a startState via E-transition ***/
        private Set getEpsClosureFromState(AutomataState startState)
        {
            // set for dividing states
            Set processedStates = new Set();
            Set unprocessedStates = new Set();

            // add start state to unprocessed set
            unprocessedStates.addElement(startState);

            // until exists unprocessed states
            while (unprocessedStates.Count > 0)
            {
                // get all "toState" of E-transitions from "state"
                AutomataState state = (AutomataState)unprocessedStates[0];
                List<AutomataState> l_trns = state.getAllDestStatesWithStringTransaction(RExpr_Parser.Tokens.EPSILON);

                // process state
                processedStates.addElement(state);
                unprocessedStates.removeElFromSet(state);

                if (l_trns != null && l_trns.Count > 0)
                {
                    foreach (AutomataState Estate in l_trns)
                    {
                        if (!processedStates.Contains(Estate))
                        {
                            unprocessedStates.addElement(Estate);
                        }
                    }
                }
            }

            return processedStates;
        }

        /*** Finds all states that are "toStates" by a set of startState via E-transition ***/
        private Set getEpsClosureFromStateSet(Set states)
        {
            Set setAllEclosure = new Set();
            AutomataState state = null;
            foreach (object obj in states)
            {
                state = (AutomataState)obj;

                Set setEclosure = getEpsClosureFromState(state);
                setAllEclosure.setUnion(setEclosure);
            }
            return setAllEclosure;
        }

        /*** Fill the sets "allStates" and "allSym" (respectivly) with all the states and the symbols of the automata (discriminate NFA-DFA-DFAmin from starting state) ***/
        private static void FillStatesAndSymbols(Set allStates, Set allSym, AutomataState stateStart)
        {
            Set unprocessedStates = new Set();

            // add first state to unprocessed
            unprocessedStates.addElement(stateStart);

            // untile the are unprocessed states
            while (unprocessedStates.Count > 0)
            {
                AutomataState state = (AutomataState)unprocessedStates[0];

                allStates.addElement(state);
                unprocessedStates.removeElFromSet(state);

                // for each transition char of transitions of "state"
                foreach (string s in state.getAllTransitionsChars())
                {
                    allSym.addElement(s);

                    // get all "Tostate"s from state by "s"
                    List<AutomataState> l_trns = state.getAllDestStatesWithStringTransaction(s);

                    if (l_trns != null && l_trns.Count > 0)
                    {
                        foreach (AutomataState EpsState in l_trns)
                        {
                            if (!allStates.Contains(EpsState))
                            {
                                unprocessedStates.addElement(EpsState);
                            }
                        }
                    }

                }

            }

        }

        /*** Perform a re-arrange of the parsed regex string applying postfixing tecnique. Return the postfixed-operatior of the parse string. ***/
        private string ArrangeRegex(string parsed_regex)
        {
            bool detect_escape = false;
            Stack<char> charsStack = new Stack<char>();
            Queue<char> postfixOperandsQueue = new Queue<char>();

            for (int i = 0; i < parsed_regex.Length; i++)
            {
                char cur_char = parsed_regex[i];

                // if escape character detected AND is the first escape char encountered, put it in the queue and go to next loop
                if (!detect_escape && cur_char == RExpr_Parser.Tokens.TOK_ESCAPE)
                {
                    postfixOperandsQueue.Enqueue(cur_char);
                    detect_escape = true;
                    continue;
                }

                // if finds another escape char, make newsly accessible the first "if" (anther escape char admitted)
                if (detect_escape)
                {
                    postfixOperandsQueue.Enqueue(cur_char);
                    detect_escape = false;
                    continue;
                }

                switch (cur_char)
                {
                    case RExpr_Parser.Tokens.TOK_CIRCLE_LPAREN:     // '('
                        {
                            charsStack.Push(cur_char);
                            break;
                        }

                    case RExpr_Parser.Tokens.TOK_CIRCLE_RPAREN:     // ')'
                        {
                            while (charsStack.Peek() != RExpr_Parser.Tokens.TOK_CIRCLE_LPAREN)
                            {
                                postfixOperandsQueue.Enqueue(charsStack.Pop());
                            }

                            charsStack.Pop();  // pop the finded '(' (necessary exists since the regex pass the parsing)

                            break;
                        }

                    default:
                        {
                            // until stack is not empty
                            while (charsStack.Count > 0)
                            {
                                char chPeeked = charsStack.Peek();
                                string s = getCharPriority(chPeeked).ToString();

                                // if the peeked char have the precedence
                                if (getCharPriority(chPeeked) >=  getCharPriority(cur_char))
                                {
                                    postfixOperandsQueue.Enqueue(charsStack.Pop());
                                }
                                else
                                {
                                    break;
                                }
                            }

                            charsStack.Push(cur_char);
                            break;
                        }
                }

            }

            // emptying the remaining stack considering considering the remaining chars "enqueable"
            while (charsStack.Count > 0)
            {
                postfixOperandsQueue.Enqueue(charsStack.Pop());
            }

            StringBuilder postifed_str = new StringBuilder();

            // the result string is the total dequeue of chars previously enqueued
            while (postfixOperandsQueue.Count > 0)
            {
                postifed_str.Append(postfixOperandsQueue.Dequeue());
            }

            return postifed_str.ToString();
        }

        /*** Define a system of priority based on operators and integer. High-number ==> high.priority ***/
        private int getCharPriority(char chr)
        {
            switch (chr)
            {
                case RExpr_Parser.Tokens.TOK_CIRCLE_LPAREN:
                    {
                        return CharPriority.P_CIRCLE_LPAREN;
                    }
                case RExpr_Parser.Tokens.TOK_OR:
                    {
                        return CharPriority.P_OR;
                    }

                case RExpr_Parser.Tokens.TOK_CONCAT:
                    {
                        return CharPriority.P_CONC;
                    }

                case RExpr_Parser.Tokens.TOK_ZERO_ONE:
                case RExpr_Parser.Tokens.TOK_KLEENE:
                    //case RExpr_Parser.Tokens.ONE_OR_MORE:
                    {
                        return CharPriority.P_UNARY;
                    }
                /*case RExpr_Parser.Tokens.COMPLEMENT:
                    return 4;*/
                default:
                    {
                        return CharPriority.MAX_PRIORITY;
                    }

            }
        }

        #endregion

        /*** Try to parse the input string ***/
        public bool isMatch(Stopwatch timer, string str)
        {
            if (isCompiled)
            {
                // run assembly
                return runCompiledCode(timer, str);
            }

            // start timer to calculate execution time
            timer.Reset();
            timer.Start();

            // otherwise run the interpretated version
            AutomataState stateStart = Sz_DFAmin, toState = null, currState = stateStart;

            for (int i = 0; i < str.Length; i++)
            {
                // get current transition char
                char chInputSymbol = str[i];
                
                // get "to" state
                toState = currState.getExactTransition(chInputSymbol.ToString());

                if (toState == null)
                {
                    if (checkSomeChar(currState))
                    {
                        toState = currState.getExactTransition(RExpr_Parser.Tokens.TOK_SOMECHAR_TRANS);
                    }
                    else
                    {
                        return false;
                    }
                }

                currState = toState;
            }

            // stop timing
            timer.Stop();

            return currState.IsFinalState;
        }

        /****************************************************************************************/
        /****************************************************************************************/

        #region DFAMethodsRegion
        /*** Add a new DFA state to the hashtable ***/
        public void CreateNewDFAState(Set Eclos, AutomataState DFAstate)
        {
            AutomataDFAState stateRecord = new AutomataDFAState();
            stateRecord.EpsClos = Eclos;

            DFAStatesTable[DFAstate] = stateRecord;
        }

        /*** Try to find a DFA state using the states in the passed E-closure. ***/
        public AutomataState FindDfaStateByEclosure(Set Eclos)
        {
            AutomataDFAState stateRecord = null;

            foreach (DictionaryEntry de in DFAStatesTable)
            {
                stateRecord = (AutomataDFAState)de.Value;
                if (stateRecord.EpsClos.equalTo(Eclos))
                {
                    return (AutomataState)de.Key;
                }
            }
            return null;

        }

        /*** Given an input state, get it E-closure. ***/
        public Set getEpsClosure(AutomataState state)
        {
            AutomataDFAState s = (AutomataDFAState)DFAStatesTable[state];
            return (s == null) ? null : s.EpsClos;
        }

        /*** Return the first unmarked state of the hashtable, if exist. Otherwise return null. ***/
        public AutomataState getUnmarkedState()
        {
            AutomataDFAState stateRecord = null;

            foreach (DictionaryEntry de in DFAStatesTable)
            {
                stateRecord = (AutomataDFAState)de.Value;

                if (!stateRecord.isMark)
                {
                    return (AutomataState)de.Key;
                }

            }

            return null;
        }

        /*** Mark the passed state of the hashtable ***/
        public void markState(AutomataState s)
        {
            ((AutomataDFAState)DFAStatesTable[s]).isMark = true;
        }

        /*** Get the set of all the states of the hashtable ***/
        public Set getAllDFAStates()
        {
            Set setState = new Set();

            foreach (object objKey in DFAStatesTable.Keys)
            {
                setState.addElement(objKey);
            }

            return setState;
        }
        #endregion
    }
}
