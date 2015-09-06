using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Regex
{
    public class RExpr_Parser
    {
        /*** Define all the tokens used for parsing ***/
        internal class Tokens
        {
            // Tokens for first-step parser
            public const char
                        TOK_OR = '|',
                        TOK_KLEENE = '*',
                        TOK_ZERO_ONE = '?',
                        TOK_CIRCLE_LPAREN = '(',
                        TOK_CIRCLE_RPAREN = ')',
                        TOK_QUAD_LPAREN = '[',
                        TOK_QUAD_RPAREN = ']',
                        TOK_ESCAPE = '\\',                  // discriminate escape characters
                        TOK_RANGE = '-',
                        TOK_START = '^',                    // specify the begining of the string
                        TOK_END = '$',                      // specify the end of the string
                        TOK_CONCAT = '#',
                        TOK_SOMECHAR = '.',                 // '.' specify the SINGLE character can be replaced with any one ASCII char (es. a.b -> akb)
                        NEW_LINE = 'n',
                        TAB = 't';

            // Tokens for automata
            public const string
                        TOK_SOMECHAR_TRANS = "AnyChar",     // specify the current transition-symbol for automata
                        EPSILON = "epsilon";
        }

        /*** Istance variables ***/
        private char curChar;
        private int curChar_index = -1, regexLength = 0;
        private string globalRegex = "";
        private StringBuilder output_str = null;

        private bool detect_concat = false;
        private bool detect_or = false;

        /*** Constructor Method ***/
        public RExpr_Parser()
        {

        }

        /*** Perform the PARSING of the "regex" regular expression given in input. If an error occur, throw SyntaxException (captured out). 
             Otherwise return the parsed string. ***/
        public string ParseRExpr(string regex)
        {
            // empty string case
            if (regex.Length == 0)
            {
                throw new SyntaxException("Cannot parse empty regular expression.");
            }

            // initialize global variables for parsing (global regex and its length, index of the stream)
            output_str = new StringBuilder();
            globalRegex = regex;
            detect_concat = detect_or = false;
            regexLength = globalRegex.Length;
            curChar_index = -1;
            curChar = '\0';

            // get first symbol on the stream (advancing stream-index)
            AdvanceInputOnStream();

            #region initialChecks
            // if the regex is == "^" || "$" || "^$" 
            if ( (regex == Tokens.TOK_START.ToString()) || (regex == Tokens.TOK_END.ToString()) || (regex == (Tokens.TOK_START + "" + Tokens.TOK_END)) )
            {
               // throw new SyntaxException("Cannot parse a regular expression with only \"" + regex + "\".");
            }

            // try to match TOK_START in first position of regex
            if (regex[0] != Tokens.TOK_START)
            {
               // throw new SyntaxException("Mismatch start token \"" + Tokens.TOK_START +"\" in regular expression \"" + regex + "\".");
            }
            else
            {
                TryToConsumeToken(Tokens.TOK_START);
            }

            // try to match TOK_END in the last position of regex
            if (regex[regexLength - 1] != Tokens.TOK_END)
            {
                //throw new SyntaxException("Mismatch end token \"" + Tokens.TOK_END + "\" in regular expression \"" + regex + "\".");
            }
            else
            {
                regexLength--;
            }
            #endregion

            // loop on remaining stream chars/tokens
            while (curChar_index < regexLength)
            {
                switch (curChar)
                {
                    case Tokens.TOK_OR:
                    case Tokens.TOK_KLEENE:
                    case Tokens.TOK_ZERO_ONE:
                        {
                            throw new SyntaxException("Missing operand before '" + curChar + "' parenthesis.");  // "|RE" or "*" or "?" input regex
                        }
                    case Tokens.TOK_CIRCLE_RPAREN:
                        {
                            throw new SyntaxException("Missing ')' parenthesis.");  // "(A(D*)"
                        }
                    default:
                        {
                            parse_expression();
                            break;
                        }
                }
            }

            return this.output_str.ToString();
        }

        /*** implements the generic parsing choise of the predictive recursive descendant parser ***/
        private void parse_expression()
        {
            while (TryToConsumeToken(Tokens.TOK_ESCAPE))    // "\"
            {
                AddConcatAtEnd();

                if (!ExpectEscapeChar())
                {
                    throw new SyntaxException("Unexpected escape character.");
                }

                TryToConsumeUnaryToken();
                detect_concat = true;
            }

            while (TryToConsumeToken(Tokens.TOK_CONCAT))    // "#"
            {
                AddConcatAtEnd();
                this.output_str.Append(Tokens.TOK_ESCAPE);
                this.output_str.Append(Tokens.TOK_CONCAT);
                TryToConsumeUnaryToken();
                detect_concat = true;
            }

            while (TryToConsumeNonEscapeToken())            // "somechar"
            {
                TryToConsumeUnaryToken();
                detect_concat = true;
                parse_expression();
            }

            if (TryToConsumeToken(Tokens.TOK_CIRCLE_LPAREN))    // "("
            {
                int ePos = curChar_index - 1;
                AddConcatAtEnd();

                this.output_str.Append(Tokens.TOK_CIRCLE_LPAREN);

                parse_expression();

                if (!ExactlyExpect(Tokens.TOK_CIRCLE_RPAREN))
                {
                    throw new SyntaxException("Expected token ')'.");       // again "("
                }

                this.output_str.Append(Tokens.TOK_CIRCLE_RPAREN);

                if ((curChar_index - ePos) == 2)
                {
                    throw new SyntaxException("Expected expression between \"()\"");    // "()"
                }

                TryToConsumeUnaryToken();
                detect_concat = true;

                // recursion
                parse_expression();
            }

            if (TryToConsumeToken(Tokens.TOK_QUAD_LPAREN))      // "["
            {
                int ePos2 = curChar_index - 1;

                AddConcatAtEnd();

                string tmp_output_str = this.output_str.ToString();

                this.output_str = new StringBuilder();
                detect_or = false;

                // try to perform range-search parsing (for [char-char])
                checkRangeRE();

                if (!ExactlyExpect(Tokens.TOK_QUAD_RPAREN))
                {
                    throw new SyntaxException("Expected ']'");   // "["
                }

                if ((curChar_index - ePos2) == 2)
                {
                    throw new SyntaxException("Expected expression between \"[]\"");  // "[]"
                }
                else
                {
                    string sCharset = this.output_str.ToString();
                    this.output_str = new StringBuilder();
                    this.output_str.Append(tmp_output_str);

                    this.output_str.Append(Tokens.TOK_CIRCLE_LPAREN);
                    this.output_str.Append(sCharset);
                    this.output_str.Append(Tokens.TOK_CIRCLE_RPAREN);
                }

                TryToConsumeUnaryToken();

                detect_concat = true;

                parse_expression();
            }


            if (TryToConsumeToken(Tokens.TOK_OR))   // "|"
            {
                int ePos3 = curChar_index - 1;
                detect_concat = false;
                this.output_str.Append(Tokens.TOK_OR);

                parse_expression();

                if ((curChar_index - ePos3) == 1)
                {
                    throw new SyntaxException("Missing operand after '|'");    // "RE|"
                }

                parse_expression();
            }
        }

        #region OutputAppendMethods
        
        /*** attach a concatanation (logic and) token at the end of the output string ***/
        private void AddConcatAtEnd()
        {
            if (detect_concat)
            {
                this.output_str.Append(Tokens.TOK_CONCAT);
                detect_concat = false;
            }
        }

        /*** attach an alternation (logic or) token at the end of the output string ***/
        private void AddOrAtAnd()
        {
            if (detect_or)
            {
                this.output_str.Append(Tokens.TOK_OR);
                detect_or = false;
            }
        }
        #endregion

        #region ExpectedMethos

        /*** switch on curchar to find an escape char. If is finded, modify the output string and return true. Otherwise return false. ***/
        private bool ExpectEscapeChar()
        {
            switch (curChar)
            {       
                case Tokens.TOK_OR:                 // "|", ".", "[", ")", "\", "(", "*" or finally "?"
                case Tokens.TOK_SOMECHAR:
                case Tokens.TOK_QUAD_LPAREN:
                case Tokens.TOK_CIRCLE_RPAREN:
                case Tokens.TOK_ESCAPE:
                case Tokens.TOK_CIRCLE_LPAREN:
                case Tokens.TOK_KLEENE:
                case Tokens.TOK_ZERO_ONE:
                    {
                        this.output_str.Append(Tokens.TOK_ESCAPE);
                        this.output_str.Append(curChar);
                        TryToConsumeToken(curChar);
                        break;
                    }

                case Tokens.NEW_LINE:               // "\n"
                    {
                        this.output_str.Append('\n');
                        TryToConsumeToken(curChar);
                        break;
                    }

                case Tokens.TAB:                    // "\t"
                    {
                        this.output_str.Append('\t');
                        TryToConsumeToken(curChar);
                        break;
                    }

                default:
                    {
                        return false;
                    }
            }

            return true;
        }

        /*** check if the expected token is exactly matched with the passed character ***/
        private bool ExactlyExpect(char chr)
        {
            return TryToConsumeToken(chr);
        }

        /*** check if in a range [...] there is a escape character. ***/
        private string ExpectEscapeInRangeInterval()
        {
            char chr = curChar;
            switch (curChar)
            {
                case Tokens.TOK_QUAD_RPAREN:            // "[" or "\"
                case Tokens.TOK_ESCAPE:
                    {
                        TryToConsumeToken(curChar);
                        return Tokens.TOK_ESCAPE.ToString() + chr.ToString();
                    }

                case Tokens.NEW_LINE:                   // "\n"
                    {
                        TryToConsumeToken(curChar);
                        return ('\n').ToString();
                    }
                case Tokens.TAB:                        // "\t"
                    {
                        TryToConsumeToken(curChar);
                        return ('\t').ToString();
                    }
                default:
                    {
                        return "";
                    }
            }
        }

        #endregion

        #region TryToConsumeTokensMethods

        /*** Advance the index-counter and if exists another char on the stream, set the current char to it. Otherwise set the current char to null character. ***/
        private void AdvanceInputOnStream()
        {
            curChar_index++;
            if (curChar_index >= regexLength)
            {
                curChar = '\0';     // null char
            }
            else
            {
                curChar = globalRegex[curChar_index];
            }
        }

        /*** Check if the current char on the stream match with the passed char. If it match, consume the curren token and return true. Otherwise return false. ***/
        private bool TryToConsumeToken(char chr)
        {
            if (curChar != chr)
            {
                return false;
            }

            AdvanceInputOnStream();
            return true;
        }

        /*** Switch on curchar to find a postfix operator. If find '*' or '?', append it on output string and return true. Otherwise return false. ***/
        private bool TryToConsumeUnaryToken()
        {
            switch (curChar)
            {
                case Tokens.TOK_KLEENE:
                case Tokens.TOK_ZERO_ONE:   // "*" or "?"
                    {
                        this.output_str.Append(curChar);
                        return TryToConsumeToken(curChar);
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        /*** Switch on curchar and return false it is a token-operator. Otherwise append TOK_CONCAT and current token, consume the next token and return true. ***/
        private bool TryToConsumeNonEscapeToken()
        {
            switch (curChar)
            {
                case Tokens.TOK_OR:
                case Tokens.TOK_QUAD_LPAREN:
                case Tokens.TOK_CIRCLE_RPAREN:
                case Tokens.TOK_ESCAPE:
                case Tokens.TOK_CIRCLE_LPAREN:
                case Tokens.TOK_KLEENE:
                case Tokens.TOK_ZERO_ONE:
                case Tokens.TOK_CONCAT:
                case '\0':
                    {
                        return false;
                    }

                default:
                    {
                        AddConcatAtEnd();
                        this.output_str.Append(curChar);
                        TryToConsumeToken(curChar);
                        break;
                    }
            }

            return true;
        }

        private string TryToConsumeNonEscapeInRange()
        {
            char chr = curChar;

            switch (chr)
            {
                case Tokens.TOK_QUAD_RPAREN:
                case Tokens.TOK_ESCAPE:
                case '\0':
                    {
                        return "";
                    }

                case Tokens.TOK_OR:
                case Tokens.TOK_SOMECHAR:
                case Tokens.TOK_CIRCLE_RPAREN:
                case Tokens.TOK_CIRCLE_LPAREN:
                case Tokens.TOK_KLEENE:
                case Tokens.TOK_ZERO_ONE:
                case Tokens.TOK_CONCAT:
                    {
                        TryToConsumeToken(curChar);
                        return Tokens.TOK_ESCAPE.ToString() + chr.ToString();
                    }
                default:
                    {
                        TryToConsumeToken(curChar);
                        return chr.ToString();
                    }
            }
        }

        #endregion

        #region RegexRangeMethods
        private void checkRangeRE()
        {
            int nRangeFormStartAt = -1, rangeStartIndex = -1, nLength = -1;

            // aa-aa range syntax
            string sLeft = String.Empty;
            string sRange = String.Empty;
            string sRight = String.Empty;


            string sTmp = String.Empty;

            while (true)
            {
                sTmp = String.Empty;

                rangeStartIndex = curChar_index;

                if (TryToConsumeToken(Tokens.TOK_ESCAPE))
                {
                    if ((sTmp = ExpectEscapeInRangeInterval()) == String.Empty)
                    {
                        throw new SyntaxException("Invalid escape character.");
                    }

                    nLength = 2;
                }

                if (sTmp == String.Empty)
                {
                    sTmp = TryToConsumeNonEscapeInRange();
                    nLength = 1;
                }

                if (sTmp == String.Empty)
                {
                    break;
                }

                if (sLeft == String.Empty)
                {
                    nRangeFormStartAt = rangeStartIndex;
                    sLeft = sTmp;
                    AddOrAtAnd();

                    this.output_str.Append(sTmp);
                    detect_or = true;

                    continue;
                }

                if (sRange == String.Empty)
                {
                    if (sTmp != Tokens.TOK_RANGE.ToString())
                    {
                        nRangeFormStartAt = rangeStartIndex;
                        sLeft = sTmp;
                        AddOrAtAnd();

                        this.output_str.Append(sTmp);
                        detect_or = true;

                        continue;
                    }
                    else
                    {
                        sRange = sTmp;
                    }
                    continue;
                }

                sRight = sTmp;


                bool bOk = ExtendRange(sLeft, sRight);

                if (bOk == false)
                {
                    int nSubstringLen = (rangeStartIndex + nLength) - nRangeFormStartAt;
                    throw new SyntaxException("Invalid range specified in \"[]\".");
                }

                sLeft = String.Empty;
                sRange = String.Empty;
                sRange = String.Empty;
            }

            if (sRange != String.Empty)
            {
                AddOrAtAnd();

                this.output_str.Append(sRange);
                
                detect_or = true;
            }

        }

        /*** Given the limits (sx && dx) of the range, try to expand it to make the max expandible range poxible with the stream tokens ***/
        private bool ExtendRange(string sLeft, string sRight)
        {
            char chLeft = (sLeft.Length > 1 ? sLeft[1] : sLeft[0]);
            char chRight = (sRight.Length > 1 ? sRight[1] : sRight[0]);

            if (chLeft > chRight)
            {
                return false;
            }

            chLeft++;
            while (chLeft <= chRight)
            {
                AddOrAtAnd();

                switch (chLeft)
                {
                    case Tokens.TOK_OR:
                    case Tokens.TOK_SOMECHAR:
                    case Tokens.TOK_CIRCLE_RPAREN:
                    //case Tokens.COMPLEMENT:
                    case Tokens.TOK_CONCAT:
                    case Tokens.TOK_ESCAPE:
                    //case Tokens.ONE_OR_MORE:
                    case Tokens.TOK_KLEENE:
                    case Tokens.TOK_ZERO_ONE:
                    case Tokens.TOK_CIRCLE_LPAREN:
                        {
                            this.output_str.Append(Tokens.TOK_ESCAPE);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                this.output_str.Append(chLeft);
                detect_or = true;
                chLeft++;
            }

            return true;

        }
        #endregion
    }
}
