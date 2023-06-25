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
using System.Reflection.Emit;
using Microsoft.VisualBasic;
using System.Reflection;
using Label = System.Windows.Forms.Label;

namespace Image_Editing_app
{
    public partial class Form1 : Form
    {

        private string savedFileName;
        private string stringText;
        private Stack<(PictureBox, bool, int)> undoStack;  // true = create, false = delete, int = index
        private Stack<(PictureBox, bool, int)> redoStack;
        private List<PictureBox> layers;
        private ToolStripButton? currentlySelectedButton = null;
        private CheckBox? currentlySelectedCheckBox = null;
        private PictureBox? selectedPictureBox = null;
        readonly Color obicnaBackgroundColor = Color.FromArgb(92, 224, 231); // rgba(92,224,231,255)
        private Bitmap bm;
        private Graphics g;
        private List<Point> polygonPoints;
        private bool polygonCompleted;
        private Pen p = new Pen(Color.Black, 1);
        private Point px, py;
        private bool paint = false;
        private int x, y, sx, sy, cx, cy, i;

        private List<CheckBox> visibilityCheckboxes;  // List to hold the visibility checkboxes

        public Form1()
        {
            InitializeComponent();
            undoStack = new();
            redoStack = new();
            layers = new();
            g = CreateGraphics();
            polygonCompleted = false;
            polygonPoints = new();
            visibilityCheckboxes = new();
            i = 0;
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
            /*if (index == 2)
            {
                polygonPoints.Clear(); // Clear polygon points
            }*/
            /*if (currentlySelectedButton != toolStripButton15)  // move tool selected
                return;*/

            if (e.Button == MouseButtons.Left && currentlySelectedButton == toolStripButton15)
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
            /*if (index == 2)
            {
                if (paint)
                {
                    px = e.Location;
                    polygonPoints.Add(px); // Add the current point to the polygon points list
                }
            }*/

            /*if (currentlySelectedButton != toolStripButton15)
                return;*/

            if (isDragging)
            {
                PictureBox pictureBox = (PictureBox)sender;
                Point currentPoint = pictureBox.Location;
                int deltaX = e.X - startPoint.X;
                int deltaY = e.Y - startPoint.Y;
                currentPoint.Offset(deltaX, deltaY);
                pictureBox.Location = currentPoint;
            }

            //selectedPictureBox.Refresh();

            x = e.X;
            y = e.Y;
            sx = e.X - cx;
            sy = e.Y - cy;
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            paint = false;

            sx = x - cx;
            sy = y - cy;

            if(currentlySelectedButton == toolStripButton11) // ellipse
            {
                g.DrawEllipse(p, cx, cy, sx, sy);
                SelektujIliDeselektuj(toolStripButton11);
                selectedPictureBox.Enabled = false;
            }
            else if(currentlySelectedButton == toolStripButton13) // polygon
            {
                //DrawPolygon();
            }
            else if (currentlySelectedButton == toolStripButton13) // Line
            {
                g.DrawLine(p, cx, cy, x, y);
                SelektujIliDeselektuj(toolStripButton12);
                selectedPictureBox.Enabled = false;
            }

        }

        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (!polygonCompleted && e.Button == MouseButtons.Left)
            {
                polygonPoints.Add(e.Location);
                selectedPictureBox.Invalidate();
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
        }
        private void DrawLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton12);
            addPictureBox();
        }

        private void DrawPolygonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton13);
            addPictureBox();
        }
        private void TextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Brush brush = Brushes.Black;
            SelektujIliDeselektuj(toolStripButton14);
            stringText = addString();
            g.DrawString(stringText, new Font("Poppins", 12), brush, new Point(0, 0));
        }

        private string addString()
        {
            string userInput = Interaction.InputBox("Enter text:", "Text Input");
            PictureBox pictureBox = new PictureBox();
            pictureBox.BackColor = Color.Transparent;
            pictureBox.Size = new Size(200, 100);
            pictureBox.Location = new Point(200, 100);
            pictureBox.BorderStyle = BorderStyle.None;
            bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            g = Graphics.FromImage(bm);
            pictureBox.Image = bm;

            AddPictureBox(pictureBox);

            return userInput;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Environment.Exit(0); }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                i++;
                polygonCompleted = true;
                //selectedPictureBox.Invalidate();
                DrawPolygon();
            }
        }

        private void MoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton15);
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton9);
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton10);
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
            pictureBox.Size = new Size(ClientRectangle.Width, ClientRectangle.Height - 80); // Set the desired size
            pictureBox.Location = new Point(0, 75); // Adjust the location based on the desired positioning

            // Set other properties as desired, e.g., pictureBox.Image = yourImage;
            AddPictureBox(pictureBox);


            bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            g = Graphics.FromImage(bm);
            pictureBox.Image = bm;

            return pictureBox;
        }

        private void DrawPolygon()
        {
            if (polygonPoints.Count >= 3) // Check if there are at least 3 points to form a polygon
            {
                //g.DrawString("hello world", new Font("Poppins", 12), new SolidBrush(Color.Black), new Point(200, 100));
                g.DrawPolygon(p, polygonPoints.ToArray()); // Draw the polygon using the collected points
                SelektujIliDeselektuj(toolStripButton13);
                selectedPictureBox.Enabled = false;
            }
        }

        public void AddPictureBox(PictureBox pictureBox)
        {
            pictureBox.Name = "Layer: " + (i + 1);
            pictureBox.Parent = panel1;
            layers.Add(pictureBox); // Add the PictureBox to the list
            this.Controls.Add(pictureBox); // Add the PictureBox to the form's Controls collection
            pictureBox.BringToFront();

            pictureBox.Click += PictureBox_Click;
            pictureBox.MouseDown += PictureBox_MouseDown; // Renamed the event handler
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;

            undoStack.Push((pictureBox, true, layers.Count - 1));

            // Add item to the TableLayoutPanel
            int row = i;
            i++;
            tableLayoutPanel1.RowStyles.Insert(0, new RowStyle(SizeType.AutoSize));
            tableLayoutPanel1.RowCount++;

            Label layerLabel = new Label()
            {
                Text = pictureBox.Name
            };
            tableLayoutPanel1.Controls.Add(layerLabel, 0, i);

            CheckBox visibilityCheckbox = new CheckBox();
            visibilityCheckbox.Checked = true;
            visibilityCheckbox.CheckedChanged += (sender, e) =>
            {
                CheckBox checkbox = (CheckBox)sender;
                PictureBox pb = (PictureBox)checkbox.Tag;
                pb.Visible = checkbox.Checked;
            };
            visibilityCheckbox.Tag = pictureBox;
            visibilityCheckbox.AutoSize = true;
            tableLayoutPanel1.Controls.Add(visibilityCheckbox, 1, i);

            // Enable row reordering
            layerLabel.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Label label = (Label)sender;
                    label.DoDragDrop(label, DragDropEffects.Move);
                }
            };

            tableLayoutPanel1.DragOver += (sender, e) =>
            {
                e.Effect = DragDropEffects.Move;
            };

            tableLayoutPanel1.DragDrop += (sender, e) =>
            {
                Label sourceLabel = (Label)e.Data.GetData(typeof(Label));
                Label targetLabel = (Label)tableLayoutPanel1.GetChildAtPoint(tableLayoutPanel1.PointToClient(new Point(e.X, e.Y)));

                if (sourceLabel != null && targetLabel != null)
                {
                    int sourceRow = tableLayoutPanel1.GetRow(sourceLabel);
                    int targetRow = tableLayoutPanel1.GetRow(targetLabel);

                    if (sourceRow != targetRow)
                    {
                        tableLayoutPanel1.SuspendLayout();
                        tableLayoutPanel1.Controls.SetChildIndex(sourceLabel, targetRow * tableLayoutPanel1.ColumnCount);
                        tableLayoutPanel1.Controls.SetChildIndex(targetLabel, sourceRow * tableLayoutPanel1.ColumnCount);
                        tableLayoutPanel1.ResumeLayout();

                        // Update the order of layers in the 'layers' list
                        layers.RemoveAt(sourceRow);
                        layers.Insert(targetRow, layers[sourceRow]);

                        // Update the layer indices in the undoStack
                        var undoStackCopy = new Stack<(PictureBox, bool, int)>(undoStack);
                        undoStack.Clear();

                        for (int index = 0; index < tableLayoutPanel1.RowCount; index++)
                        {
                            undoStack.Push((undoStackCopy.ElementAt(index).Item1, undoStackCopy.ElementAt(index).Item2, index));
                        }
                    }
                }
            };

            tableLayoutPanel1.BringToFront();
        }
    }
}
