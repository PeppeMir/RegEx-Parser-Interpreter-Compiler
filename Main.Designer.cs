namespace Regex
{
    partial class Main
    {
        /// <summary>
        /// Variabile di progettazione necessaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Pulire le risorse in uso.
        /// </summary>
        /// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Codice generato da Progettazione Windows Form

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_regex = new System.Windows.Forms.TextBox();
            this.label_regex = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_string = new System.Windows.Forms.TextBox();
            this.button_parse = new System.Windows.Forms.Button();
            this.textBox_stats = new System.Windows.Forms.TextBox();
            this.button_compile = new System.Windows.Forms.Button();
            this.button_interpretate = new System.Windows.Forms.Button();
            this.button_all = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox_regex
            // 
            this.textBox_regex.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_regex.Location = new System.Drawing.Point(12, 35);
            this.textBox_regex.Name = "textBox_regex";
            this.textBox_regex.Size = new System.Drawing.Size(409, 22);
            this.textBox_regex.TabIndex = 0;
            this.textBox_regex.Text = "[a-g]*.(a|b*b|c?)";
            // 
            // label_regex
            // 
            this.label_regex.AutoSize = true;
            this.label_regex.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_regex.Location = new System.Drawing.Point(12, 19);
            this.label_regex.Name = "label_regex";
            this.label_regex.Size = new System.Drawing.Size(84, 13);
            this.label_regex.TabIndex = 1;
            this.label_regex.Text = "Insert RegEx:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Insert string:";
            // 
            // textBox_string
            // 
            this.textBox_string.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_string.Location = new System.Drawing.Point(12, 83);
            this.textBox_string.Multiline = true;
            this.textBox_string.Name = "textBox_string";
            this.textBox_string.Size = new System.Drawing.Size(409, 92);
            this.textBox_string.TabIndex = 2;
            this.textBox_string.Text = "abccdgkbb";
            // 
            // button_parse
            // 
            this.button_parse.Location = new System.Drawing.Point(435, 35);
            this.button_parse.Name = "button_parse";
            this.button_parse.Size = new System.Drawing.Size(79, 22);
            this.button_parse.TabIndex = 4;
            this.button_parse.Text = "Parse regex";
            this.button_parse.UseVisualStyleBackColor = true;
            this.button_parse.Click += new System.EventHandler(this.button_parse_Click);
            // 
            // textBox_stats
            // 
            this.textBox_stats.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_stats.Location = new System.Drawing.Point(12, 195);
            this.textBox_stats.Multiline = true;
            this.textBox_stats.Name = "textBox_stats";
            this.textBox_stats.ReadOnly = true;
            this.textBox_stats.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox_stats.Size = new System.Drawing.Size(506, 168);
            this.textBox_stats.TabIndex = 7;
            this.textBox_stats.WordWrap = false;
            // 
            // button_compile
            // 
            this.button_compile.Location = new System.Drawing.Point(435, 112);
            this.button_compile.Name = "button_compile";
            this.button_compile.Size = new System.Drawing.Size(79, 22);
            this.button_compile.TabIndex = 11;
            this.button_compile.Text = "Compile";
            this.button_compile.UseVisualStyleBackColor = true;
            this.button_compile.Click += new System.EventHandler(this.button_compile_Click);
            // 
            // button_interpretate
            // 
            this.button_interpretate.Location = new System.Drawing.Point(435, 81);
            this.button_interpretate.Name = "button_interpretate";
            this.button_interpretate.Size = new System.Drawing.Size(79, 25);
            this.button_interpretate.TabIndex = 12;
            this.button_interpretate.Text = "Interpretate";
            this.button_interpretate.UseVisualStyleBackColor = true;
            this.button_interpretate.Click += new System.EventHandler(this.button_interpretate_Click);
            // 
            // button_all
            // 
            this.button_all.Location = new System.Drawing.Point(435, 140);
            this.button_all.Name = "button_all";
            this.button_all.Size = new System.Drawing.Size(79, 44);
            this.button_all.TabIndex = 13;
            this.button_all.Text = "Interpretate\r\n + Compile";
            this.button_all.UseVisualStyleBackColor = true;
            this.button_all.Click += new System.EventHandler(this.button_all_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 375);
            this.Controls.Add(this.button_all);
            this.Controls.Add(this.button_interpretate);
            this.Controls.Add(this.button_compile);
            this.Controls.Add(this.textBox_stats);
            this.Controls.Add(this.button_parse);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_string);
            this.Controls.Add(this.label_regex);
            this.Controls.Add(this.textBox_regex);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Main";
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_regex;
        private System.Windows.Forms.Label label_regex;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_string;
        private System.Windows.Forms.Button button_parse;
        public System.Windows.Forms.TextBox textBox_stats;
        private System.Windows.Forms.Button button_compile;
        private System.Windows.Forms.Button button_interpretate;
        private System.Windows.Forms.Button button_all;
    }
}

