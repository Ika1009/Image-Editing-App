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
        private Stack<(Layer, bool, int)> undoStack;  // true = create, false = delete, int = index
        private Stack<(Layer, bool, int)> redoStack;
        private BindingList<Layer> layers;
        private ToolStripButton? currentlySelectedButton = null;
        private CheckBox? currentlySelectedCheckBox = null;
        private Layer? selectedLayer = null;
        readonly Color obicnaBackgroundColor = Color.FromArgb(92, 224, 231); // rgba(92,224,231,255)
        private Bitmap bm;
        private Graphics g;
        private List<Point> polygonPoints;
        private bool polygonCompleted;
        private Pen p = new Pen(Color.Black, 5);
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

        private void Form_Load(object sender, EventArgs e)
        {
            // Set up the DataGridView
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowDrop = true;

            // Define the columns
            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.DataPropertyName = "Name";
            nameColumn.HeaderText = "Name";

            DataGridViewCheckBoxColumn visibleColumn = new DataGridViewCheckBoxColumn();
            visibleColumn.DataPropertyName = "Visible";
            visibleColumn.HeaderText = "Visible";

            // Add the columns to the DataGridView
            dataGridView1.Columns.Add(nameColumn);
            dataGridView1.Columns.Add(visibleColumn);
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            dataGridView1.CurrentCellDirtyStateChanged += dataGridView1_CurrentCellDirtyStateChanged;
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;


            // Set the DataSource to the BindingList
            dataGridView1.DataSource = layers;

            dataGridView1.MouseMove += dataGridView1_MouseMove;
            dataGridView1.MouseDown += dataGridView1_MouseDown;
            dataGridView1.DragOver += dataGridView1_DragOver;
            dataGridView1.DragDrop += dataGridView1_DragDrop;

        }

        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;
        private void dataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (dragBoxFromMouseDown != Rectangle.Empty &&
                    !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {

                    // Proceed with the drag and drop, passing in the list item.                    
                    DragDropEffects dropEffect = dataGridView1.DoDragDrop(
                    dataGridView1.Rows[rowIndexFromMouseDown],
                    DragDropEffects.Move);
                }
            }
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the index of the item the mouse is below.
            rowIndexFromMouseDown = dataGridView1.HitTest(e.X, e.Y).RowIndex;
            if (rowIndexFromMouseDown != -1)
            {
                // Remember the point where the mouse down occurred. 
                // The DragSize indicates the size that the mouse can move 
                // before a drag event should be started.                
                Size dragSize = SystemInformation.DragSize;

                // Create a rectangle using the DragSize, with the mouse position being
                // at the center of the rectangle.
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                               e.Y - (dragSize.Height / 2)),
                                    dragSize);
            }
            else
                // Reset the rectangle if the mouse is not over an item in the ListBox.
                dragBoxFromMouseDown = Rectangle.Empty;
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            // The mouse locations are relative to the screen, so they must be 
            // converted to client coordinates.
            Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));

            // Get the row index of the item the mouse is below. 
            rowIndexOfItemUnderMouseToDrop =
                dataGridView1.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            // If the drag operation was a move then remove and insert the row.
            if (e.Effect == DragDropEffects.Move)
            {
                DataGridViewRow? rowToMove = e.Data.GetData(
                    typeof(DataGridViewRow)) as DataGridViewRow;

                Layer layerToMove = layers[rowIndexFromMouseDown];
                layers.RemoveAt(rowIndexFromMouseDown);

                // Adjust the row index if it's out of bounds
                if (rowIndexOfItemUnderMouseToDrop < 0 || rowIndexOfItemUnderMouseToDrop >= layers.Count)
                {
                    rowIndexOfItemUnderMouseToDrop = layers.Count;
                }

                // Insert the layerToMove at the rowIndexOfItemUnderMouseToDrop index
                layers.Insert(rowIndexOfItemUnderMouseToDrop, layerToMove);

                // Iterate through the layers list to change display order
                foreach (Layer layer in layers)
                {
                    // Find the PictureBox associated with the current layer
                    PictureBox pictureBox = layer.PictureBox;

                    if (pictureBox != null)
                    {
                        // Bring the PictureBox to the front
                        pictureBox.BringToFront();
                    }
                }
            }
        }


        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Check if the current cell is in the selected row
            if (e.RowIndex >= 0 && dataGridView1.Rows[e.RowIndex].Selected)
            {
                // Set custom selection colors
                e.CellStyle.SelectionBackColor = Color.Red; // Change to the desired background color
                e.CellStyle.SelectionForeColor = Color.White; // Change to the desired foreground color
            }
            else
            {
                // Reset the cell style for non-selected cells
                e.CellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.SelectionBackColor;
                e.CellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.SelectionForeColor;
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Check if the modified cell belongs to the visibleColumn
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                // Get the Layer object bound to the current row
                Layer layer = (Layer)dataGridView1.Rows[e.RowIndex].DataBoundItem;

                // Update the Visible property of the Layer object based on the checkbox value
                layer.Visible = (bool)dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            }
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Commit the changes when the checkbox cell is clicked
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
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

                    AddPictureBox(pictureBox, true);
                }
            }
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            if (selectedLayer != null)
            {
                // Reset the border style of the previously selected Layer's PictureBox
                selectedLayer.PictureBox.BorderStyle = BorderStyle.None;
            }

            selectedLayer = layers.FirstOrDefault(layer => layer.PictureBox == (PictureBox)sender);

            // Set the border style of the selected Layer's PictureBox
            selectedLayer.PictureBox.BorderStyle = BorderStyle.FixedSingle;
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
            Random random = new Random();
            sx = x - cx;
            sy = y - cy;

            if (currentlySelectedButton == toolStripButton11) // ellipse
            {
                selectedLayer.PictureBox.CreateGraphics().DrawEllipse(p, cx, cy, sx, sy);
                SelektujIliDeselektuj(toolStripButton11);
                selectedLayer.PictureBox.Enabled = false;
            }
            else if (currentlySelectedButton == toolStripButton13) // polygon
            {
                // DrawPolygon();
            }
            else if (currentlySelectedButton == toolStripButton12) // Line
            {
                selectedLayer.PictureBox.CreateGraphics().DrawLine(p, cx, cy, x, y);
                SelektujIliDeselektuj(toolStripButton12);
                selectedLayer.PictureBox.Enabled = false;
            }
        }

        private void PictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (!polygonCompleted && e.Button == MouseButtons.Left)
            {
                polygonPoints.Add(e.Location);
                selectedLayer.PictureBox.Invalidate();
            }
        }

        // Add this field to your Form1 class
        private bool undoWasLastOperation = false;

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                (Layer lastLayer, bool wasCreated, int index) = undoStack.Pop();
                if (wasCreated)
                {
                    redoStack.Push((lastLayer, true, index));
                    lastLayer.Visible = false;
                    if (layers.Contains(lastLayer))
                        layers.Remove(lastLayer);
                }
                else
                {
                    redoStack.Push((lastLayer, false, index));
                    lastLayer.Visible = true;
                    if (!layers.Contains(lastLayer))
                        layers.Insert(index, lastLayer);
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
                (Layer layer, bool wasCreated, int index) = redoStack.Pop();

                if (wasCreated)
                {
                    layer.Visible = true;
                    if (!layers.Contains(layer))
                        layers.Insert(index, layer);
                    undoStack.Push((layer, true, index));
                }
                else
                {
                    layer.Visible = false;
                    if (layers.Contains(layer))
                        layers.Remove(layer);
                    undoStack.Push((layer, false, index));
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
                    foreach (Layer layer in layers)
                    {
                        PictureBox pb = layer.PictureBox;
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
            if (selectedLayer?.PictureBox.Image != null)
            {
                Clipboard.SetImage(selectedLayer.PictureBox.Image);
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
            if (selectedLayer == null)
                return;

            // Remove from collections
            int index = layers.IndexOf(selectedLayer);
            foreach (Layer layer in layers)
                if (layer.PictureBox.Parent == selectedLayer.PictureBox)
                    layer.PictureBox.Parent = selectedLayer.PictureBox.Parent;

            layers.RemoveAt(index);

            // Hide the PictureBox
            selectedLayer.Visible = false;

            // Reset selected Layer
            selectedLayer = null;

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
                        Layers = layers.Select(layer => StateData.LayerDTO.FromLayer(layer)).ToList(),
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
                    StateData.LayerDTO layerDTO = StateData.LayerDTO.FromLayer(layer);
                    layerDTO.Location = layer.PictureBox.Location;
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

                    // Create Layers from the deserialized StateData object
                    foreach (var layerDTO in stateData.Layers)
                    {
                        Layer layer = new Layer(layerDTO.ToPictureBox());
                        layer.PictureBox.Location = layerDTO.Location;
                        layers.Add(layer);
                        AddPictureBox(layer.PictureBox, false);
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
            foreach (Layer layer in layers)
            {
                PictureBox pictureBox = layer.PictureBox;
                Control parentContainer = pictureBox.Parent;

                parentContainer.Controls.Remove(pictureBox);
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

            AddPictureBox(pictureBox, false);

            return userInput;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Environment.Exit(0); }

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
            pictureBox.Size = new Size(layers[layers.Count - 1].PictureBox.Width, layers[layers.Count - 1].PictureBox.Height); // Set the desired size
            pictureBox.Location = layers[layers.Count - 1].PictureBox.Location;

            // Set other properties as desired, e.g., pictureBox.Image = yourImage;
            AddPictureBox(pictureBox, false);


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
                selectedLayer.PictureBox.Enabled = false;
            }
        }

        public void AddPictureBox(PictureBox pictureBox, bool onPanel)
        {
            pictureBox.Name = "Layer: " + (i + 1);
            pictureBox.Parent = panel1;

            if (layers.Count == 0 || onPanel || selectedLayer is null)
                panel1.Controls.Add(pictureBox); // Add the PictureBox to the panel's Controls collection
            else
            {
                if (pictureBox != selectedLayer.PictureBox)
                    selectedLayer.PictureBox.Controls.Add(pictureBox);
                else
                    panel1.Controls.Add(pictureBox); // Add the PictureBox to the panel's Controls collection
            }

            Layer layer = new Layer(pictureBox);
            layers.Add(layer); // Add the Layer to the list

            pictureBox.BringToFront();

            layer.PictureBox.Click += PictureBox_Click;
            layer.PictureBox.MouseDown += PictureBox_MouseDown; // Renamed the event handler
            layer.PictureBox.MouseMove += PictureBox_MouseMove;
            layer.PictureBox.MouseUp += PictureBox_MouseUp;

            undoStack.Push((layer, true, layers.Count - 1));

            i++;
        }

    }
}
