using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Forms;

namespace Image_Editing_app
{
    public partial class Form1 : Form
    {
        private Stack<(PictureBox, bool, int)> undoStack;  // true = create, false = delete, int = index
        private Stack<(PictureBox, bool, int)> redoStack;
        private List<PictureBox> layers;
        private int brojac;

        private ToolStripButton? currentlySelectedButton = null;
        private PictureBox? selectedPictureBox = null;

        readonly Color obicnaBackgroundColor = Color.FromArgb(92, 224, 231); // rgba(92,224,231,255)

        Bitmap bm;
        Graphics g;
        bool paint = false;
        Point px, py;
        Pen p = new Pen(Color.Black, 1);
        int index;
        int x, y, sx, sy, cx, cy;
        int i;

        public Form1()
        {
            InitializeComponent();
            undoStack = new Stack<(PictureBox, bool, int)>();
            redoStack = new Stack<(PictureBox, bool, int)>();
            layers = new List<PictureBox>();
        }

        private void importImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                openFileDialog.Title = "Import Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    undoToolStripMenuItem.Enabled = true;
                    PictureBox pictureBox = new PictureBox();
                    Image importedImage = Image.FromFile(openFileDialog.FileName);
                    pictureBox.Size = importedImage.Size; // set size of PictureBox to the size of the imported image
                    pictureBox.Image = importedImage;
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox.BackColor = Color.Transparent;
                    pictureBox.Parent = panel1;

                    pictureBox.Click += PictureBox_Click;
                    pictureBox.MouseDown += PictureBox_MouseDown;
                    pictureBox.MouseMove += PictureBox_MouseMove;
                    pictureBox.MouseUp += PictureBox_MouseUp;

                    pictureBox.BringToFront();
                    layers.Add(pictureBox);
                    undoStack.Push((pictureBox, true, layers.Count - 1));
                }
            }

            listView1.Items.Add($"Layer {brojac}: vidljiv");
            brojac++;
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            if (selectedPictureBox != null)
            {
                // Reset the border style of the previously selected PictureBox
                selectedPictureBox.BorderStyle = BorderStyle.None;
            }

            selectedPictureBox = (PictureBox)sender;

            // Set the border style of the selected PictureBox
            selectedPictureBox.BorderStyle = BorderStyle.FixedSingle;
        }
        private bool isDragging = false;
        private Point startPoint;

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                startPoint = e.Location;
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                PictureBox pictureBox = (PictureBox)sender;
                Point currentPoint = pictureBox.Location;
                int deltaX = e.X - startPoint.X;
                int deltaY = e.Y - startPoint.Y;
                currentPoint.Offset(deltaX, deltaY);
                pictureBox.Location = currentPoint;
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        // Add this field to your Form1 class
        private bool undoWasLastOperation = false;

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                (PictureBox lastPictureBox, bool wasCreated, int index) = undoStack.Pop();
                if (wasCreated)
                {
                    redoStack.Push((lastPictureBox, true, index));
                    lastPictureBox.Visible = false;
                    if (layers.Contains(lastPictureBox))
                        layers.Remove(lastPictureBox);
                }
                else
                {
                    redoStack.Push((lastPictureBox, false, index));
                    lastPictureBox.Visible = true;
                    if (!layers.Contains(lastPictureBox))
                        layers.Insert(index, lastPictureBox);
                }

                if (undoStack.Count == 0)
                    undoToolStripMenuItem.Enabled = false;

                if (!redoToolStripMenuItem.Enabled)
                    redoToolStripMenuItem.Enabled = true;

                undoWasLastOperation = true; // Set the flag here
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // If undo was not the last operation, return early
            if (!undoWasLastOperation) return;

            if (redoStack.Count > 0)
            {
                (PictureBox pictureBox, bool wasCreated, int index) = redoStack.Pop();

                if (wasCreated)
                {
                    pictureBox.Visible = true;
                    if (!layers.Contains(pictureBox))
                        layers.Insert(index, pictureBox);
                    undoStack.Push((pictureBox, true, index));
                }
                else
                {
                    pictureBox.Visible = false;
                    if (layers.Contains(pictureBox))
                        layers.Remove(pictureBox);
                    undoStack.Push((pictureBox, false, index));
                }

                if (redoStack.Count == 0)
                    redoToolStripMenuItem.Enabled = false;

                if (!undoToolStripMenuItem.Enabled)
                    undoToolStripMenuItem.Enabled = true;

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

                    // Calculate the bounding rectangle based on the panel's bounds
                    int minX = panel1.Left;
                    int minY = panel1.Top;
                    int maxX = panel1.Right;
                    int maxY = panel1.Bottom;

                    int width = panel1.Width;
                    int height = panel1.Height;

                    // Create a new bitmap the size of the panel
                    Bitmap bmp = new Bitmap(width, height);

                    // Create a new graphics object from the bitmap
                    Graphics g = Graphics.FromImage(bmp);

                    // Adjust the graphics object to the panel's position
                    g.TranslateTransform(-minX, -minY);

                    // Loop through the PictureBoxes in your layers list
                    foreach (PictureBox pb in layers)
                    {
                        // If the PictureBox is visible and intersects with the panel's bounds, draw it on the bitmap
                        if (pb.Visible && pb.Bounds.IntersectsWith(panel1.Bounds))
                        {
                            g.DrawImage(pb.Image, pb.Location);
                        }
                    }

                    // Dispose of the Graphics object now we're done with it
                    g.Dispose();

                    // Save the bitmap as an image
                    bmp.Save(fileName);

                    // Dispose of the bitmap to free up memory
                    bmp.Dispose();
                }
            }
        }


        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (panel1.Image != null)
            //{
            //    Clipboard.SetImage(panel1.Image);
            //}
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // ModifyImage();

            //if (Clipboard.ContainsImage())
            //{
            //    Image image = Clipboard.GetImage();
            //    panel1.Image = image;
            //}
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedPictureBox == null)
                return;

            // Remove from collections
            int index = layers.IndexOf(selectedPictureBox);
            layers.RemoveAt(index);
            undoStack.Push((selectedPictureBox, false, index));

            // Hide the PictureBox
            selectedPictureBox.Visible = false;

            // Reset selected PictureBox
            selectedPictureBox = null;

            if (!undoToolStripMenuItem.Enabled)
                undoToolStripMenuItem.Enabled = true;
        }


        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            //{
            //    saveFileDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png|Bitmap Image|*.bmp";
            //    saveFileDialog.Title = "Save Image As";
            //    saveFileDialog.FileName = "image";

            //    if (saveFileDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        string fileName = saveFileDialog.FileName;
            //        Image image = panel1.Image;
            //        image.Save(fileName);
            //    }
            //}
        }

        //private void ModifyImage()
        //{
        //    Image currentState = panel1.Image;
        //    undoStack.Push(currentState);
        //    provera();
        //}

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Draw click
        private void DrawCircleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton11);
            addPictureBox();
            index = 2;
        }
        private void DrawLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton12);
            addPictureBox();
            index = 4;
        }

        private void DrawPolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton13);
            addPictureBox();
            index = 3;
        }
        private void TextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton14);
            addPictureBox();
            index = 5;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Environment.Exit(0); }

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
            SelektujIliDeselektuj(toolStripButton9);
            //brojac++;
            //panel1.Image = null;
            //panel1.Image = ZoomPicture(org.Image, new Size(brojac, brojac));
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton10);
            //if (brojac > 1)
            //{
            //    brojac--;
            //    panel1.Image = null;
            //    panel1.Image = ZoomPicture(org.Image, new Size(brojac, brojac));
            //}
        }
        private void RotateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton5);
        }


        private void SelektujIliDeselektuj(ToolStripButton stripButton) // selektuje ako treba ili deselektuje
        {
            if (currentlySelectedButton == stripButton) // duplo je selektovana
            {
                stripButton.BackColor = obicnaBackgroundColor;
                currentlySelectedButton = null;
                return;
            }
            stripButton.BackColor = SystemColors.Highlight;
            if (currentlySelectedButton is not null) currentlySelectedButton.BackColor = obicnaBackgroundColor;
            currentlySelectedButton = stripButton;
        }

        private void addPictureBox()
        {
            PictureBox pictureBox = new PictureBox();
            pictureBox.BackColor = Color.AliceBlue;
            pictureBox.Name = "pictureBox" + (i + 1);
            pictureBox.Size = new Size(570, 324); // Set the desired size
            pictureBox.Location = new Point(183, 99); // Adjust the location based on the desired positioning

            // Set other properties as desired, e.g., pictureBox.Image = yourImage;

            layers.Add(pictureBox); // Add the PictureBox to the list
            this.Controls.Add(pictureBox); // Add the PictureBox to the form's Controls collection
            pictureBox.BringToFront();

            pictureBox.Click += PictureBox_Click;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;

            undoStack.Push((pictureBox, true, layers.Count - 1));
        }
    }
}
