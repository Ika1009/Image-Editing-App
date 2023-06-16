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
        private Image originalImage;
        private float zoomFactor = 1.0f;

        public Form1()
        {
            InitializeComponent();
            //originalImage = Image.FromFile("image.jpg");
            //pictureBox1.Image = originalImage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png|Bitmap Image|*.bmp";
                saveFileDialog.Title = "Save Image As";
                saveFileDialog.FileName = "image"; // Default file name

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFileDialog.FileName;

                    // Get the image you want to save (assuming it's stored in a PictureBox control)
                    Image image = pictureBox1.Image;

                    // Save the image to the selected file
                    image.Save(fileName);

                    // MessageBox.Show("Image saved successfully!", "Save Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png|Bitmap Image|*.bmp";
                saveFileDialog.Title = "Export Image";
                saveFileDialog.FileName = "image"; // Default file name

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFileDialog.FileName;

                    // Get the image you want to export (assuming it's stored in a PictureBox control)
                    Image image = pictureBox1.Image;

                    // Save the image to the selected file
                    image.Save(fileName);

                    // MessageBox.Show("Image exported successfully!", "Export Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Create a new Bitmap object to serve as the canvas
            Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            // Create a Graphics object from the Bitmap
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Set the pen properties for drawing the rectangle
                Pen pen = new Pen(Color.Red, 2); // Red color with 2-pixel width

                // Define the rectangle coordinates and dimensions
                int x = 50; // X-coordinate of the top-left corner
                int y = 50; // Y-coordinate of the top-left corner
                int width = 200; // Width of the rectangle
                int height = 150; // Height of the rectangle

                // Draw the rectangle on the Graphics object
                graphics.DrawRectangle(pen, x, y, width, height);
            }

            // Display the Bitmap on a PictureBox control
            pictureBox1.Image = bitmap;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                openFileDialog.Title = "Import Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;

                    // Load the image from the selected file
                    Image image = Image.FromFile(fileName);

                    // Display the image in a PictureBox control (assuming pictureBox1 is the PictureBox control)
                    pictureBox1.Image = image;

                    // Optionally, you can resize the PictureBox to fit the image
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

                    // MessageBox.Show("Image imported successfully!", "Import Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            // Zoom in or out based on the mouse wheel delta
            if (e.Delta > 0)
            {
                // Zoom in
                zoomFactor += 0.1f;
            }
            else
            {
                // Zoom out
                zoomFactor -= 0.1f;
                if (zoomFactor < 0.1f)
                    zoomFactor = 0.1f;
            }

            // Apply the zoom level to the image
            Image zoomedImage = ScaleImage(originalImage, zoomFactor);

            // Update the PictureBox with the zoomed image
            pictureBox1.Image = zoomedImage;
        }

        private Image ScaleImage(Image image, float scaleFactor)
        {
            int newWidth = (int)(image.Width * scaleFactor);
            int newHeight = (int)(image.Height * scaleFactor);

            // Create a new Bitmap with the scaled dimensions
            Bitmap scaledBitmap = new Bitmap(newWidth, newHeight);

            // Create a Graphics object from the scaled Bitmap
            using (Graphics graphics = Graphics.FromImage(scaledBitmap))
            {
                // Set the interpolation mode to achieve smoother scaling
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                // Draw the scaled image onto the Graphics object
                // graphics.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight));
            }

            return scaledBitmap;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                // Copy the image to the clipboard
                Clipboard.SetImage(pictureBox1.Image);

                // MessageBox.Show("Image copied to clipboard!", "Copy Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No image available to copy!", "Copy Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                // Retrieve the image from the clipboard
                Image image = Clipboard.GetImage();

                // Assign the image to the PictureBox control
                pictureBox1.Image = image;

                // MessageBox.Show("Image pasted successfully!", "Paste Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No image available on the clipboard!", "Paste Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;

            //MessageBox.Show("Image deleted!", "Delete Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
