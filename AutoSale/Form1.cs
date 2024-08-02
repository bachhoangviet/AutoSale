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
        private Timer timer;
        private Timer timer2;
        private Timer timer3;
        private string? lastCapturedText;
        private string? secondText;
        private string? thirdText;

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


        public Form1()
        {
            InitializeComponent();
           
            this.TopMost = true;
            this.TopLevel = true;
            selectedArea = Rectangle.Empty;
            checkBox1.Checked = true;
          
            radioButton1.Checked = true;
            InitBuyLocation();

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
                    StartAutoClick("Auto.mcr");
                    Thread.Sleep(1000);
                    SendKeys.Send("^p");

                    timer = new Timer();
                    timer.Interval = 100;
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

            var capturedText = CaptureAndReadText();

            label4.Text = $"Run {count} times with value {capturedText}";

            if (string.IsNullOrEmpty(capturedText))
            {
                timer.Start();
                return;
            }

            if (!string.IsNullOrEmpty(capturedText) && string.IsNullOrEmpty(lastCapturedText))
            {
                //var temp = capturedText.Split(".")[0];
                int res = 0;
                bool isTmpInt = int.TryParse(capturedText, out res);
                if (isTmpInt && res > 1) lastCapturedText = capturedText;
                timer.Start();
                return;
            }

            //var convertedCapture = capturedText.Split(".")[0];
            var convertedCapture = capturedText;

            //label5.Text = $"Converted: {convertedCapture}";

            //IsDiff(convertedCapture)
            if (IsDiff(convertedCapture))
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
                    timer2.Interval = 100;
                    timer2.Tick += Timer_Tick_2;

                    timer3 = new Timer();
                    timer3.Interval = 100;
                    timer3.Tick += Timer_Tick_3;

                    RunNextAuto("Auto2.mcr");
                    timer2.Start();
                }
                
            } else
            {
                timer.Start();
            }
        }

        private void RunNextAuto(string fileName)
        {
            Thread.Sleep(1000);
            StartAutoClick(fileName);
            Thread.Sleep(700);
            SendKeys.Send("^p");
            Thread.Sleep(100);
            SendKeys.Send("^p");
        }

        private void Timer_Tick_2(object sender, EventArgs e)
        {
            timer2.Stop();
            var capturedText = CaptureAndReadText();
            if(!string.IsNullOrEmpty(capturedText))
            {
                EndTaskAutoClick();
                ClickSelectedArea();

                RunNextAuto("Auto3.mcr");
                timer3.Start();
            }
            else timer2.Start();
        }

        private void Timer_Tick_3(object sender, EventArgs e)
        {
            timer3.Stop();
            var capturedText = CaptureAndReadText();
            if (!string.IsNullOrEmpty(capturedText))
            {
                EndTaskAutoClick();
                ClickSelectedArea();
            }
            else timer3.Start();
        }

        private bool IsDiff(string capturedText)
        {
            if (string.IsNullOrEmpty(capturedText) || string.IsNullOrEmpty(lastCapturedText)) return false;
            //if (capturedText == "123" || capturedText == "1" || capturedText.StartsWith("21") || capturedText.StartsWith("9") || capturedText.StartsWith("26")) return false;

            int parseNum = 0;
            bool isInt = int.TryParse(capturedText, out parseNum);
            if (!isInt) return false;
            if (parseNum <= 0) return false;

            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                var lst = textBox1.Text.Split(";");
                if (lst.Contains(capturedText)) return false;
            }

            if (!string.IsNullOrEmpty(textBox2.Text))
            {
                int minT = 0;
                bool isTextBox2Bool = int.TryParse(textBox2.Text, out minT);
                if (!isTextBox2Bool) return false;
                else if (parseNum < minT) return false;
            }

            if (!string.IsNullOrEmpty(textBox3.Text))
            {
                int maxT = 0;
                bool isTextBox3Bool = int.TryParse(textBox3.Text, out maxT);
                if (!isTextBox3Bool) return false;
                else if (parseNum > maxT) return false;
            }

            int lengthLast = lastCapturedText.Length;
            int lengthCurent = capturedText.Length;
            int min = Math.Min(lengthCurent, lengthLast);
            for (int i = 0; i < min; i++)
            {
                if (lastCapturedText[i] != capturedText[i]) return true;
            }

            return false;

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

            //mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            //mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);

            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            keybd_event(VK_RETURN, 0, 2, UIntPtr.Zero);
        }

        private string CaptureAndReadText()
        {
            using (var bitmap = new Bitmap(selectedArea.Width, selectedArea.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(selectedArea.Location, Point.Empty, selectedArea.Size);
                }
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = BitmapToPixConverter.Convert(bitmap))
                    {
                        using (var page = engine.Process(img))
                        {
                            var temp = page.GetText().Trim().Replace(",", "").Replace(" ", "").Replace(".", "");
                            if (!string.IsNullOrEmpty(temp)) return temp.Substring(0, temp.Length - 1);
                            else return temp;

                        }
                    }
                }
            }
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
