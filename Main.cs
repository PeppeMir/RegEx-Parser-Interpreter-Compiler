using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace Regex
{
    public partial class Main : Form
    {
        /*** regular expression to parse ***/
        private RExpr _regEx;
        Stopwatch timer = new Stopwatch();
        private string parsing_stat = "";
        
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            // istanziate the regular expression
            this._regEx = new RExpr(this);
        }

        /*** perform the parsing of the given regex (regex textbox) ***/
        private void button_parse_Click(object sender, EventArgs e)
        {
            // empty input regex
            if (this.textBox_regex.Text.Length == 0)
            {
                MessageBox.Show("Cannot verify empty regular expression.", "Empty Regex", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            try
            {
                this.textBox_stats.Text = "";
                // perform the parse of the regular expression given in input and, if it is syntattically correct, create a NFA-DFA automata model for this regular expression
                string automata_statistics = _regEx.ParseRegexAndCreateAutomata(timer, this.textBox_regex.Text).ToString();
                
                parsing_stat = "***************************\r\nParsing Elapsed Time:\r\n" + timer.ElapsedMilliseconds + " ms\r\n" + timer.ElapsedTicks + " ticks\r\n****************************\r\n";

                if (sender != null)
                    this.textBox_stats.Text = parsing_stat;

                this.textBox_stats.Text += automata_statistics;
            }
            catch (SyntaxException exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        /*** perform parsing + compilation: generate a C# class capable to recognize strings contained in the regex codomain (string textbox) ***/
        private void button_compile_Click(object sender, EventArgs e)
        {
            StringBuilder _str = new StringBuilder();

            // perform parsing generating DFAm automata
            this.button_parse_Click(null, null);

            _str.AppendLine("******************************************");
            _str.AppendLine("**** COMPILING RESULT (AGAIN C# CODE) ****");
            _str.AppendLine("******************************************");

            try
            {
                // generating compiled C# code for DFAm automata equivalent to input regex
                string cSharpProgram = _regEx.CompileAutomata();
                _str.Append(cSharpProgram);
            }
            catch (CompilationException exc)
            {
                this.textBox_stats.Text = "Error occurred during compilation :\r\n" + exc.Message;
                return;
            }

            // add information of compiling to stats
            this.textBox_stats.Text += _str.ToString();

            // run the compiled version of isMatch
            bool compilationMatching_Result = _regEx.isMatch(timer, textBox_string.Text);

            if (compilationMatching_Result)
            {
                this.textBox_stats.Text = "\r\n -----> MATCH <----- \r\n\r\nCompiled Version Exec Time:\r\n" + timer.ElapsedMilliseconds + " ms\r\n" + timer.ElapsedTicks + " ticks\r\n******************************************\r\n\r\n" + this.textBox_stats.Text;
            }
            else
            {
                this.textBox_stats.Text = "\r\n -----> MISMATCH <----- \r\n\r\n******************************************\r\n\r\n" + this.textBox_stats.Text;
            }

            this.textBox_stats.Text = parsing_stat + "******************************************\r\n MATCHING RESULT for \"" + this.textBox_string.Text + "\" :\r\n" + this.textBox_stats.Text;

        }

        /*** perform parsing + interpretation: through on DFAm to recognize strings contained in the regex codomain (string textbox) ***/
        private void button_interpretate_Click(object sender, EventArgs e)
        {
            StringBuilder _str = new StringBuilder();

            // perform parsing generating DFAm automata
            this.button_parse_Click(null, null);

            // *** no compile requires ***

            // run the interpreted version of isMatch
            bool interprMatching_Result = _regEx.isMatch(timer, textBox_string.Text);

            if (interprMatching_Result)
            {
                this.textBox_stats.Text = "\r\n -----> MATCH <----- \r\n\r\nInterpretated Version Exec Time:\r\n" + timer.ElapsedMilliseconds + " ms\r\n" + timer.ElapsedTicks + " ticks\r\n******************************************\r\n\r\n" + this.textBox_stats.Text;
            }
            else
            {
                this.textBox_stats.Text = "\r\n -----> MISMATCH <----- \r\n"+ this.textBox_stats.Text + "\r\n******************************************\r\n\r\n";
            }

            this.textBox_stats.Text = parsing_stat + "******************************************\r\n MATCHING RESULT for \"" + this.textBox_string.Text + "\" :\r\n" + this.textBox_stats.Text;

        }

        /*** perform parsing + interpretation + compilation (see above) ***/
        private void button_all_Click(object sender, EventArgs e)
        {
            // 1) perform parsing generating DFAm automata
            this.button_parse_Click(null, null);

            // adding parsing statistics
            this.textBox_stats.Text += parsing_stat;

            // 2) run interpreted version of isMatch
            bool interprMatching_Result = _regEx.isMatch(timer, textBox_string.Text);
            long ticksInterp = timer.ElapsedTicks;
            long msInterp = timer.ElapsedMilliseconds;

            timer.Reset();

            // 3) run compiled version of isMatch
            try
            {
                // generating compiled C# code for DFAm automata equivalent to input regex
                _regEx.CompileAutomata();
            }
            catch (CompilationException exc)
            {
                this.textBox_stats.Text = "Error occurred during compilation :\r\n" + exc.Message;
                return;
            }

            bool compilationMatching_Result = _regEx.isMatch(timer, textBox_string.Text);

            // 4) adding interpretation + compilation statistics
            this.textBox_stats.Text += "\r\nMATCHING RESULTS for \"" + textBox_string.Text +":\r\nInterpretate matching: " + ((interprMatching_Result) ? "MATCH" : "MISMATCH") + "\r\nCompilated matching: " + ((compilationMatching_Result) ? "MATCH" : "MISMATCH");
            this.textBox_stats.Text += "\r\n\r\nEXECUTION TIME:\r\nInterpretate matching: " + ticksInterp + " ticks (" + msInterp + " ms)\r\nCompilated matching: " + timer.ElapsedTicks + "ticks (" + timer.ElapsedMilliseconds + " ms)";             
        }
    }
}
