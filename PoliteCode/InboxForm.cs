using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PoliteCode
{
    public partial class InboxForm : Form
    {
        // רשימת קטעי הקוד המוכנים
        private List<CodeTemplate> codeTemplates;

        // פרופרטי להעברת הקוד שנבחר בחזרה לטופס הראשי
        public string SelectedCode { get; private set; }

        // מבנה נתונים לדוגמאות קוד
        public class CodeTemplate
        {
            public string Title { get; set; }
            public string Code { get; set; }
        }

        public InboxForm()
        {
            InitializeComponent();
            InitializeCodeTemplates();
            InitializeCodePanel();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // InboxForm
            // 
            this.ClientSize = new System.Drawing.Size(900, 700);
            this.Name = "InboxForm";
            this.Text = "Code Templates";
            this.Load += new EventHandler(InboxForm_Load);
            this.ResumeLayout(false);
        }

        private void InitializeCodeTemplates()
        {
            // רשימת תבניות הקוד המובנות
            codeTemplates = new List<CodeTemplate>
    {
        new CodeTemplate
        {
            Title = "פונקציית חיבור שני מספרים",
            Code = @"please define function integer add(integer a integer b) {
    thank you for returning a add b
}

please define function void main() {
    please create integer result
    result equals please call add(5, 3)
    thank you for printing result
}"
        },

        new CodeTemplate
        {
            Title = "פונקציה שמחזירה true אם המספר חיובי",
            Code = @"please define function boolean isPositive(integer num) {
    thank you for checking if num greater then 0 {
        thank you for returning true
    }
    thank you for returning false
}

please define function void main() {
    please create integer number equals 42
    please create boolean result
    result equals please call isPositive(number)
    thank you for printing result
}"
        },

        new CodeTemplate
        {
            Title = "פונקציית ברכה",
            Code = @"please define function text greet(text name) {
    thank you for returning ""Hello, "" add name
}

please define function void main() {
    please create text message
    message equals please call greet(""World"")
    thank you for printing message
}"
        },

        new CodeTemplate
        {
            Title = "לולאת for פשוטה",
            Code = @"please define function void main() {
    please create integer x equals 0
    thank you for looping from 1 to 5 {
        thank you for printing x

    }
}"
        },

        new CodeTemplate
        {
            Title = "לולאת while להצגת ספירה לאחור",
            Code = @"please define function void main() {
    please create integer countdown equals 10
    
    thank you for looping while countdown greater then 0 {
        thank you for printing countdown
        countdown equals countdown sub 1
    }
    
    thank you for printing ""Blastoff!""
}"
        },

        new CodeTemplate
        {
            Title = "תוכנית חישוב ממוצע",
            Code = @"please define function decimal calculateAverage(integer a, integer b, integer c) {
    please create decimal sum equals a add b add c
    thank you for returning sum div 3
}

please define function void main() {
    please create integer num1 equals 10
    please create integer num2 equals 20
    please create integer num3 equals 30
    
    please create decimal average
    average equals please call calculateAverage(num1, num2, num3)
    thank you for printing average
}"
        },

        new CodeTemplate
        {
            Title = "בדיקת מספר זוגי או אי-זוגי",
            Code = @"please define function boolean isEven(integer num) {
    please create integer remainder equals num div 2 mul 2
    
    thank you for checking if num equals remainder {
        thank you for returning true
    }
    
    thank you for returning false
}

please define function void main() {
    please create integer number equals 7
    please create boolean result
    
    result equals please call isEven(number)
    thank you for checking if result {
        thank you for printing number
    }
    
    thank you for checking if result different from true {
        thank you for printing number
    }
}"
        },

        new CodeTemplate
        {
            Title = "חישוב עצרת (factorial)",
            Code = @"please define function integer factorial(integer n) {
    thank you for checking if n less or equal to 1 {
        thank you for returning 1
    }
    
    please create integer recResult
    recResult equals please call factorial(n sub 1)
    thank you for returning n mul recResult
}

please define function void main() {
    please create integer number equals 5
    please create integer result
    result equals please call factorial(number)
    thank you for printing result
}"
        },

        new CodeTemplate
        {
            Title = "המרת טמפרטורה מצלזיוס לפרנהייט",
            Code = @"please define function decimal celsiusToFahrenheit(decimal celsius) {
    thank you for returning celsius mul 9 div 5 add 32
}

please define function void main() {
    please create decimal tempC equals 25
    please create decimal tempF
    tempF equals please call celsiusToFahrenheit(tempC)
    thank you for printing tempF
}"
        },

        new CodeTemplate
        {
            Title = "בדיקת מספר ראשוני",
            Code = @"please define function boolean isPrime(integer num) {
    thank you for checking if num less then 2 {
        thank you for returning false
    }
    
    please create integer i equals 2
    
    thank you for looping while i mul i less or equal to num {
        thank you for checking if num div i mul i equal to num {
            thank you for returning false
        }
        i equals i add 1
    }
    
    thank you for returning true
}

please define function void main() {
    please create integer number equals 17
    please create boolean isPrimeResult
    
    isPrimeResult equals please call isPrime(number)
    thank you for checking if isPrimeResult {
        thank you for printing number
    }
    

}"
        }
    };
        }

        private void InboxForm_Load(object sender, EventArgs e)
        {
            // מרכוז הטופס ועיצוב כותרת
            this.CenterToScreen();
            Label titleLabel = new Label
            {
                Text = "inbox",
                Font = new Font("Consolas", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 40,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(titleLabel);
        }

        private void InitializeCodePanel()
        {
            // יצירת פאנל עם גלילה
            Panel mainPanel = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            this.Controls.Add(mainPanel);

            // הוספת כל תבנית קוד לפאנל
            int yPos = 10;
            foreach (var template in codeTemplates)
            {
                // כותרת הקוד
                Label titleLabel = new Label
                {
                    Text = "code",
                    Font = new Font("Consolas", 14, FontStyle.Bold),
                    Location = new Point(10, yPos),
                    Size = new Size(100, 30)
                };
                mainPanel.Controls.Add(titleLabel);

                // תיבת טקסט לקוד
                TextBox codeTextBox = new TextBox
                {
                    Text = template.Code,
                    Font = new Font("Consolas", 12),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new Point(10, yPos + 30),
                    Size = new Size(mainPanel.Width - 150, 120),
                    BorderStyle = BorderStyle.FixedSingle
                };
                mainPanel.Controls.Add(codeTextBox);

                // כפתור USE
                Button useButton = new Button
                {
                    Text = "use",
                    Font = new Font("Consolas", 12, FontStyle.Bold),
                    Location = new Point(mainPanel.Width - 120, yPos + 60),
                    Size = new Size(100, 40),
                    Tag = template // שומר את התבנית כדי לדעת איזה קוד נבחר
                };
                useButton.Click += UseButton_Click;
                mainPanel.Controls.Add(useButton);

                // קו הפרדה
                Panel separator = new Panel
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(10, yPos + 160),
                    Size = new Size(mainPanel.Width - 30, 1),
                    BackColor = Color.LightGray
                };
                mainPanel.Controls.Add(separator);

                yPos += 180; // מרווח לדוגמה הבאה
            }
        }

        private void UseButton_Click(object sender, EventArgs e)
        {
            // קבל את התבנית הקשורה לכפתור הנלחץ
            Button btn = sender as Button;
            CodeTemplate template = btn.Tag as CodeTemplate;

            // שמור את הקוד שנבחר להעברה בחזרה לטופס הראשי
            SelectedCode = template.Code;

            // סגור את הטופס עם תוצאת DialogResult.OK
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}