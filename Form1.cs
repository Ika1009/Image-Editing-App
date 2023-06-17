using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Image_Editing_app
{
    public partial class Form1 : Form
    {
        PictureBox org;
        private Stack<Image> undoStack;
        int brojac;

        public Form1()
        {
            InitializeComponent();
            undoStack = new Stack<Image>();
            brojac = 1;
            this.DoubleBuffered = true;
        }

        private void importImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModifyImage();

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                openFileDialog.Title = "Import Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    /*string fileName = openFileDialog.FileName;
                    Image image = Image.FromFile(fileName);
                    pictureBox1.Image = image;
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;*/
                    org = new PictureBox();
                    org.Load(openFileDialog.FileName);
                    pictureBox1.Load(openFileDialog.FileName);
                }
            }
        }

        private void exportImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png|Bitmap Image|*.bmp";
                saveFileDialog.Title = "Export Image";
                saveFileDialog.FileName = "image";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFileDialog.FileName;
                    Image image = pictureBox1.Image;
                    image.Save(fileName);
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                Clipboard.SetImage(pictureBox1.Image);
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModifyImage();

            if (Clipboard.ContainsImage())
            {
                Image image = Clipboard.GetImage();
                pictureBox1.Image = image;
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModifyImage();
            pictureBox1.Image = null;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png|Bitmap Image|*.bmp";
                saveFileDialog.Title = "Save Image As";
                saveFileDialog.FileName = "image";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFileDialog.FileName;
                    Image image = pictureBox1.Image;
                    image.Save(fileName);
                }
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                Image previousState = undoStack.Pop();
                pictureBox1.Image = previousState;
            }
        }

        private void ModifyImage()
        {
            Image currentState = pictureBox1.Image;
            undoStack.Push(currentState);
            provera();
        }

        private void provera()
        {
            if (undoStack.Count > 0)
            {
                undoToolStripMenuItem.Enabled = true;
            }

            else
            {
                undoToolStripMenuItem.Enabled = false;
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        Image ZoomPicture(Image img, Size size)
        {
            Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * size.Width),
                Convert.ToInt32(img.Height * size.Height));
            Graphics gpu = Graphics.FromImage(bm);
            gpu.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            return bm;
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            brojac++;
            pictureBox1.Image = null;
            pictureBox1.Image = ZoomPicture(org.Image, new Size(brojac, brojac));
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {   
            if (brojac > 1)
            {
                brojac--;
                pictureBox1.Image = null;
                pictureBox1.Image = ZoomPicture(org.Image, new Size(brojac, brojac));
            }
        }
    }
}
