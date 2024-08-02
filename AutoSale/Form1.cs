using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Tesseract;
using Timer = System.Windows.Forms.Timer;

namespace AutoSale
{
    public partial class Form1 : Form
    {
        private Rectangle selectedArea;
        private Rectangle valueArea;
        private Rectangle colorArea;
        private Timer timer;
        private Timer timer2;
        private Timer timer3;
        private string? lastCapturedText;
        private string? secondText;
        private string? thirdText;
        private const string FIXED_COLOR = "#ED4428";

        int count = 0;


        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_RETURN = 0x0D;


        // Import các hàm API từ user32.dll
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 1;
        private const uint MOD_CONTROL = 0x0002; // Ctrl
        private const uint VK_O = 0x4F; // Phím O


        public Form1()
        {
            InitializeComponent();

            numericUpDown1.Value = 3;

            this.Load += new EventHandler(Form1_Load);
            this.FormClosed += new FormClosedEventHandler(Form1_FormClosed);

            this.TopMost = true;
            this.TopLevel = true;
            selectedArea = Rectangle.Empty;
            checkBox1.Checked = true;
          
            radioButton1.Checked = true;
            InitBuyLocation();
            InitCopyValue();
            InitColorValue();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Đăng ký tổ hợp phím Ctrl + O
            if (!RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL, VK_O))
            {
                MessageBox.Show("Không thể đăng ký tổ hợp phím Ctrl + O.");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Hủy đăng ký tổ hợp phím khi form đóng
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                if (m.WParam.ToInt32() == HOTKEY_ID)
                {
                    timer.Stop();
                }
            }
            base.WndProc(ref m);
        }


        //chose area
        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            using (var form = new SelectAreaForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    selectedArea = form.SelectedArea;
                    label1.Text = "Selected";
                }
            }
            this.Show();
        }

        //start
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            lastCapturedText = string.IsNullOrEmpty(textBox4.Text) ? null : textBox4.Text;
            try
            {
                if (selectedArea != Rectangle.Empty)
                {
                    StartAutoClick("Auto1.mcr");
                    Thread.Sleep(1000);
                    SendKeys.Send("^p");

                    timer = new Timer();
                    timer.Interval = 10;
                    timer.Tick += Timer_Tick;
                    timer.Start();
                }
                else
                {
                    MessageBox.Show("Please select an area first.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //stop
        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            count++;

            label4.Text = $"Run {count} times with value";

            if (GetColorCode() != FIXED_COLOR)
            {
                timer.Start();
                return;
            }

            var capturedText = GetTextFromClipboard();

            label4.Text = $"Run {count} times with value {capturedText}";

            //if (string.IsNullOrEmpty(capturedText))
            //{
            //    timer.Start();
            //    return;
            //}

            if (!string.IsNullOrEmpty(capturedText) && string.IsNullOrEmpty(lastCapturedText))
            {
                lastCapturedText = capturedText;
                timer.Start();
                return;
            }

            var convertedCapture = capturedText;

            if (IsDiffV2(convertedCapture))
            {
                label2.Text = $"Value changed from {lastCapturedText} to {convertedCapture} at {DateTime.Now.ToString("HH:mm")}";
                lastCapturedText = convertedCapture;
                //timer.Stop();
                EndTaskAutoClick();
                timer = null;

                ClickSelectedArea();


                if (checkBox1.Checked)
                {
                    timer2 = new Timer();
                    timer2.Interval = 10;
                    timer2.Tick += Timer_Tick_2;

                    timer3 = new Timer();
                    timer3.Interval = 10;
                    timer3.Tick += Timer_Tick_3;

                    if(numericUpDown1.Value >= 2)
                    {
                        RunNextAuto("Auto2.mcr");
                        timer2.Start();
                    }
                    
                }

            } else
            {
                SendKeys.Send("{ESC}");
                SendKeys.Send("^p");
                timer.Start();
            }
        }

        private void RunNextAuto(string fileName)
        {
            StartAutoClick(fileName);
            Thread.Sleep(500);
            SendKeys.Send("^p");
        }

        private void Timer_Tick_2(object sender, EventArgs e)
        {
            timer2.Stop();
            if (GetColorCode() == FIXED_COLOR)
            {
                EndTaskAutoClick();
                ClickSelectedArea();

                if(numericUpDown1.Value >= 3) {
                    RunNextAuto("Auto3.mcr");
                    timer3.Start();
                }
                
            }
            else timer2.Start();
        }

        private void Timer_Tick_3(object sender, EventArgs e)
        {
            timer3.Stop();
            if (GetColorCode() == FIXED_COLOR)
            {
                EndTaskAutoClick();
                ClickSelectedArea();
            }
            else timer3.Start();
        }

        //private bool IsDiff(string capturedText)
        //{
        //    if (string.IsNullOrEmpty(capturedText) || string.IsNullOrEmpty(lastCapturedText)) return false;
        //    //if (capturedText == "123" || capturedText == "1" || capturedText.StartsWith("21") || capturedText.StartsWith("9") || capturedText.StartsWith("26")) return false;

        //    int parseNum = 0;
        //    bool isInt = int.TryParse(capturedText, out parseNum);
        //    if (!isInt) return false;
        //    if (parseNum <= 0) return false;

        //    if (!string.IsNullOrEmpty(textBox1.Text))
        //    {
        //        var lst = textBox1.Text.Split(";");
        //        if (lst.Contains(capturedText)) return false;
        //    }

        //    if (!string.IsNullOrEmpty(textBox2.Text))
        //    {
        //        int minT = 0;
        //        bool isTextBox2Bool = int.TryParse(textBox2.Text, out minT);
        //        if (!isTextBox2Bool) return false;
        //        else if (parseNum < minT) return false;
        //    }

        //    if (!string.IsNullOrEmpty(textBox3.Text))
        //    {
        //        int maxT = 0;
        //        bool isTextBox3Bool = int.TryParse(textBox3.Text, out maxT);
        //        if (!isTextBox3Bool) return false;
        //        else if (parseNum > maxT) return false;
        //    }

        //    int lengthLast = lastCapturedText.Length;
        //    int lengthCurent = capturedText.Length;
        //    int min = Math.Min(lengthCurent, lengthLast);
        //    for (int i = 0; i < min; i++)
        //    {
        //        if (lastCapturedText[i] != capturedText[i]) return true;
        //    }

        //    return false;

        //}

        private bool IsDiffV2(string captureText)
        {
            if (string.IsNullOrEmpty(captureText) || string.IsNullOrEmpty(lastCapturedText)) return false;
            return captureText != lastCapturedText;
        }

        private void ClickSelectedArea()
        {
            int x = selectedArea.Left + (selectedArea.Width / 2);
            int y = selectedArea.Top + (selectedArea.Height / 2);

            SetForegroundWindow(this.Handle);

            SetCursorPos(x, y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);

            Thread.Sleep(100);

            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_RETURN, 0, 2, UIntPtr.Zero);
        }

        private string GetTextFromClipboard()
        {
            int x = valueArea.Left + (valueArea.Width / 2);
            int y = valueArea.Top + (valueArea.Height / 2);

            SetForegroundWindow(this.Handle);

            SetCursorPos(x, y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);

            SendKeys.Send("^c");

            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                string clipboardText = Clipboard.GetText(TextDataFormat.Text).Replace(",", "").Replace(" ", "").Replace(".", "");
                int res = 0;
                if (!int.TryParse(clipboardText, out res)) return null;

                return clipboardText;
            }
            else return null;
        }

        private void EndTaskAutoClick()
        {
            foreach (var process in Process.GetProcessesByName("MacroRecorder"))
            {
                process.Kill();
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void StartAutoClick(string fileName)
        {
            // Specify the path to the .mcr file
            string macroFilePath = fileName;

            // Create a new process
            Process macroProcess = new Process();

            // Configure the process start info
            macroProcess.StartInfo.FileName = "cmd.exe";
            macroProcess.StartInfo.Arguments = $"/C {macroFilePath}";
            macroProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            macroProcess.StartInfo.UseShellExecute = false;

            try
            {
                // Start the process
                macroProcess.Start();
                // Wait for the process to exit, if needed
                //macroProcess.WaitForExit();
                //Console.WriteLine("Macro executed successfully.");
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //timer.Stop();
            timer = new Timer();
            lastCapturedText = null;
            label1.Text = null;
            label2.Text = null;
            label4.Text = null;
            count = 0;
            button2.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(timer != null) timer.Stop();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (timer != null) timer.Start();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //StartAutoClick();
        }

        //Buy
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            InitBuyLocation();
        }

        private void InitBuyLocation()
        {
            selectedArea = new System.Drawing.Rectangle
            {
                Height = 41,
                Width = 101,
                X = 1416,
                Y = 410,
                Location = new System.Drawing.Point
                {
                    X = 1416,
                    Y = 410
                },
                Size = new System.Drawing.Size
                {
                    Height = 41,
                    Width = 101
                }
            };
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            selectedArea = new System.Drawing.Rectangle
            {
                Height = 42,
                Width = 102,
                X = 1415,
                Y = 410,
                Location = new System.Drawing.Point
                {
                    X = 1415,
                    Y = 410
                },
                Size = new System.Drawing.Size
                {
                    Height = 42,
                    Width = 102
                }
            };
        }

        private void InitCopyValue()
        {
            valueArea = new System.Drawing.Rectangle
            {
                Height = 39,
                Width = 24,
                X = 1286,
                Y = 511,
                Location = new System.Drawing.Point
                {
                    X = 1286,
                    Y = 511
                },
                Size = new System.Drawing.Size
                {
                    Height = 39,
                    Width = 24
                }
            };
        }

        private void InitColorValue()
        {
            colorArea = new System.Drawing.Rectangle
            {
                Height = 10,
                Width = 10,
                X = 1137,
                Y = 797,
                Location = new System.Drawing.Point
                {
                    X = 1137,
                    Y = 797
                },
                Size = new System.Drawing.Size
                {
                    Height = 10,
                    Width = 10
                }
            };
        }

        private string GetColorCode()
        {
            int x = colorArea.Left + (colorArea.Width / 2);
            int y = colorArea.Top + (colorArea.Height / 2);

            Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);

            Color color = screenshot.GetPixel(x, y);

            string hexColor = ColorTranslator.ToHtml(color);
            return hexColor;
        }


    }

    public static class BitmapToPixConverter
    {
        public static Pix Convert(Bitmap bitmap)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                return Pix.LoadFromMemory(ms.ToArray());
            }
        }
    }
}
