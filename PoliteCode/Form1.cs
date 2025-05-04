using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PoliteCode
{
    public partial class Form1 : Form
    {
        // רכיבי ליבה
        private Tokenizer _tokenizer;
        private CodeGenerator _codeGenerator;
        private PoliteCodeTools _tools;
        private Parser _parser;

        /// <summary>
        /// בנאי המחלקה הראשית
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            InitializeComponents();
        }

        /// <summary>
        /// אתחול רכיבי המערכת
        /// </summary>
        private void InitializeComponents()
        {
            // יצירת מופעים של הרכיבים
            _tokenizer = new Tokenizer();
            _tools = new PoliteCodeTools();
            _codeGenerator = new CodeGenerator(_tokenizer);
            _parser = new Parser(_tokenizer, _codeGenerator, _tools);
        }

        /// <summary>
        /// אירוע לחיצה על כפתור התרגום
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // איפוס מצב המפענח
                _parser.Reset();

                // פיצול הקלט לשורות
                string[] inputLines = input.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (!_parser.SetInput(inputLines))
                {
                    _tools.ShowInfo("No code to process.");
                    return;
                }

                // עיבוד הקוד
                if (_parser.ProcessCode())
                {
                    // הצגת הקוד הסופי
                    ShowFinalCode();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}\n\n{ex.StackTrace}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// הצגת הקוד המתורגם
        /// </summary>
        private void ShowFinalCode()
        {
            CodeC.Text = _parser.GetCSharpCode();
        }

        /// <summary>
        /// טיפול באירוע טעינת הטופס
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            // אתחול נוסף בעת טעינת הטופס אם נדרש
        }

        /// <summary>
        /// אירוע לחיצה על כפתור האינבוקס
        /// </summary>
        private void btnInbox_Click(object sender, EventArgs e)
        {
            // יצירת מופע של טופס האינבוקס
            InboxForm inboxForm = new InboxForm();

            // הצגת הטופס כדיאלוג והמתנה לתוצאה
            DialogResult result = inboxForm.ShowDialog();

            // אם המשתמש בחר קוד (לחץ על 'use')
            if (result == DialogResult.OK && !string.IsNullOrEmpty(inboxForm.SelectedCode))
            {
                // הצב את הקוד הנבחר בתיבת הטקסט של הקלט
                input.Text = inboxForm.SelectedCode;
            }
        }
    }
}