using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoSale
{
    public partial class SelectAreaForm : Form
    {
        public Rectangle SelectedArea { get; private set; }

        private bool isSelecting;
        private Point startPoint;

        public SelectAreaForm()
        {
            InitializeComponent();
            this.Opacity = 0.5;
            this.BackColor = Color.Gray;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.MouseDown += SelectAreaForm_MouseDown;
            this.MouseMove += SelectAreaForm_MouseMove;
            this.MouseUp += SelectAreaForm_MouseUp;
        }

        private void SelectAreaForm_MouseDown(object sender, MouseEventArgs e)
        {
            isSelecting = true;
            startPoint = e.Location;
            SelectedArea = Rectangle.Empty;
            this.Invalidate();
        }

        private void SelectAreaForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                SelectedArea = new Rectangle(
                    Math.Min(startPoint.X, e.X),
                    Math.Min(startPoint.Y, e.Y),
                    Math.Abs(startPoint.X - e.X),
                    Math.Abs(startPoint.Y - e.Y));
                this.Invalidate();
            }
        }

        private void SelectAreaForm_MouseUp(object sender, MouseEventArgs e)
        {
            isSelecting = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (SelectedArea != Rectangle.Empty)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, SelectedArea);
                }
            }
        }
    }
}
