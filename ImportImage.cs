using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Image_Editing_app
{
    public partial class ImportImage : Form
    {
        public ImportImage()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
        }
        public void Import()
        {
            //using (OpenFileDialog openFileDialog = new OpenFileDialog())
            //{
            //    openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            //    openFileDialog.Title = "Import Image";

            //    if (openFileDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        undoToolStripMenuItem.Enabled = true;
            //        PictureBox pictureBox = new PictureBox();
            //        Image importedImage;
            //        try
            //        {
            //            importedImage = Image.FromFile(openFileDialog.FileName);
            //            // Rest of the code to work with the imported image if the image is to big
            //        }
            //        catch (Exception ex)
            //        {
            //            MessageBox.Show("Error loading the image: " + ex.Message);
            //            return;
            //        }

            //        // Apply transparency
            //        Bitmap bmp = new Bitmap(importedImage);
            //        if (transparencyValue != 100)
            //        {
            //            int transparencyAlphaValue = (int)((transparencyValue / 100.0) * 255); // Assuming transparencyValue is from 0 to 100
            //            for (int y = 0; y < bmp.Height; y++)
            //            {
            //                for (int x = 0; x < bmp.Width; x++)
            //                {
            //                    Color c = bmp.GetPixel(x, y);
            //                    bmp.SetPixel(x, y, Color.FromArgb(transparencyAlphaValue, c.R, c.G, c.B));
            //                }
            //            }
            //        }


            //        pictureBox.Size = bmp.Size; // set size of PictureBox to the size of the imported image
            //        pictureBox.Image = bmp;
            //        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            //        pictureBox.Location = new Point(200, 100);
            //        pictureBox.BackColor = Color.Transparent;

            //        AddPictureBox(pictureBox, false);
            //    }
            //}
        }
    }
}
