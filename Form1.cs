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
using System.Text.Json;

namespace Image_Editing_app
{
    public partial class Form1 : Form
    {
        private string savedFileName;

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
            g = CreateGraphics();
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
                    pictureBox.Location = new Point(200, 100);
                    pictureBox.BackColor = Color.Transparent;

                    AddPictureBox(pictureBox);
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
            if (currentlySelectedButton != toolStripButton15)  // move tool selected
                return;

            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                startPoint = e.Location;
            }

            else
            {
                paint = true;
                py = e.Location;

                cx = e.X;
                cy = e.Y;
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentlySelectedButton != toolStripButton15)
                return;

            if (isDragging)
            {
                PictureBox pictureBox = (PictureBox)sender;
                Point currentPoint = pictureBox.Location;
                int deltaX = e.X - startPoint.X;
                int deltaY = e.Y - startPoint.Y;
                currentPoint.Offset(deltaX, deltaY);
                pictureBox.Location = currentPoint;
            }

            if (paint)
            {
                if (index == 1)
                {
                    px = e.Location;
                    g.DrawLine(p, px, py);
                    py = px;
                }
            }

            //selectedPictureBox.Refresh();

            x = e.X;
            y = e.Y;
            sx = e.X - cx;
            sy = e.Y - cy;
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            Brush brush = Brushes.Black;
            isDragging = false;
            paint = false;

            sx = x - cx;
            sy = y - cy;

            switch (index)
            {
                case 1:
                    g.DrawEllipse(p, cx, cy, sx, sy);
                    break;
                case 2:
                    g.DrawRectangle(p, cx, cy, sx, sy);
                    break;
                case 3:
                    g.DrawLine(p, cx, cy, x, y);
                    break;
                case 4:
                    g.DrawString("Hello, world!", new Font("Poppins", 12), brush, new Point(50, 50));
                    break;
            }
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
            if (selectedPictureBox?.Image != null)
            {
                Clipboard.SetImage(selectedPictureBox.Image);
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                Image image = Clipboard.GetImage();
                var picturebox = addPictureBox();
                picturebox.Image = image;
            }
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
            SaveAs();
        }
        private void SaveAs()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Work File|*.wrk";
                saveFileDialog.Title = "Save As";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFileDialog.FileName;

                    // Create a StateData object to hold the necessary data
                    StateData stateData = new StateData
                    {
                        Layers = layers.Select(layer => StateData.LayerDTO.FromPictureBox(layer)).ToList(),
                    };

                    // Serialize the StateData object to JSON
                    string json = stateData.SerializeToJson();

                    // Save the JSON data to the file
                    File.WriteAllText(fileName, json);
                }
            }
        }

        private void Save(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(savedFileName))
            {
                SaveAs();
                return;
            }

            StateData stateData = new StateData
            {
                Layers = layers.Select(layer =>
                {
                    StateData.LayerDTO layerDTO = StateData.LayerDTO.FromPictureBox(layer);
                    layerDTO.Location = layer.Location;
                    return layerDTO;
                }).ToList(),
            };

            string json = stateData.SerializeToJson();
            File.WriteAllText(savedFileName, json);
        }

        private void Open(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Work File|*.wrk";
                openFileDialog.Title = "Open";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;

                    // Read the JSON data from the file
                    string json = File.ReadAllText(fileName);

                    // Deserialize the JSON data to StateData object
                    StateData stateData = StateData.DeserializeFromJson(json);

                    // Clear existing layers
                    ClearLayers();

                    // Create PictureBoxes from the deserialized StateData object
                    foreach (var layerDTO in stateData.Layers)
                    {
                        PictureBox pictureBox = layerDTO.ToPictureBox();
                        pictureBox.Location = layerDTO.Location;
                        AddPictureBox(pictureBox);
                    }

                    // Update the savedFileName
                    savedFileName = fileName;

                    // Enable necessary controls
                    undoToolStripMenuItem.Enabled = false;
                    redoToolStripMenuItem.Enabled = false;
                    deleteToolStripMenuItem.Enabled = false;
                }
            }
        }
        private void ClearLayers()
        {
            foreach (PictureBox pictureBox in layers)
            {
                panel1.Controls.Remove(pictureBox);
                pictureBox.Dispose();
            }

            layers.Clear();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Draw click
        private void DrawCircleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton11);
            addPictureBox();
            index = 1;
        }
        private void DrawLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton12);
            addPictureBox();
            index = 3;
        }

        private void DrawPolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton13);
            addPictureBox();
            index = 2;
        }
        private void TextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton14);
            addPictureBox();
            index = 4;
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Environment.Exit(0); }

        private void MoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton15);
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

        private PictureBox addPictureBox()
        {
            PictureBox pictureBox = new PictureBox();
            pictureBox.BackColor = Color.AliceBlue;
            pictureBox.Name = "pictureBox" + (i + 1);
            pictureBox.Size = new Size(570, 324); // Set the desired size
            pictureBox.Location = new Point(183, 99); // Adjust the location based on the desired positioning

            // Set other properties as desired, e.g., pictureBox.Image = yourImage;
            AddPictureBox(pictureBox);


            bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            g = Graphics.FromImage(bm);
            g.Clear(Color.White);
            //pomeranje = new List<bool>();
            pictureBox.Image = bm;

            return pictureBox;
        }

        public void AddPictureBox(PictureBox pictureBox)
        {
            pictureBox.Parent = panel1;
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
