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
using System.Collections.ObjectModel;
using System.Reflection;
using System.Drawing.Drawing2D;

namespace Image_Editing_app
{
    public partial class Form1 : Form
    {
        private string savedFileName = "";
        private Stack<(Layer, bool, int)> undoStack;  // true = create, false = delete, int = index
        private Stack<(Layer, bool, int)> redoStack;
        private BindingList<Layer> layers;
        private ToolStripButton? currentlySelectedButton = null;
        private Layer? selectedLayer = null;
        readonly Color obicnaBackgroundColor = Color.FromArgb(226, 227, 226); // control light color
        private Graphics g;
        private bool isDragging = false;
        private Point startPoint;
        private bool isDrawingEllipse = false;
        private Point initialMousePosition;
        private bool isDrawingPolygon = false;
        private List<Point> polygonPoints = new List<Point>();
        private Point point1;
        private Point point2;
        private Tuple<PictureBox, Image> originalPbImage;
        int i, x, y, lx, ly = 0;
        private Color selectedColor = Color.White;
        private int strokeValue = 1, opacityValue = 100; // opacity is in percentage
        private int transparencyValue = 100, unitIndex = 1;
        public Form1()
        {
            InitializeComponent();
            undoStack = new();
            redoStack = new();
            layers = new();
            i = 0;
            g = CreateGraphics();
            panel1.MouseClick += panel1_MouseClick;
            unitComboBox.SelectedIndex = unitIndex;
        }

        private void Form_Load(object sender, EventArgs e)
        {
            // Here are the properties of the datagrid view. It is bound to List<Layer> layers

            // Set up the DataGridView
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowDrop = true;

            // Define the columns
            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn();
            nameColumn.DataPropertyName = "Name";
            nameColumn.HeaderText = "Layer name";

            IconDataGridViewCheckBoxColumn visibleColumn = new IconDataGridViewCheckBoxColumn();

            visibleColumn.DataPropertyName = "Visible";
            visibleColumn.HeaderText = "View";

            // Add the columns to the DataGridView
            dataGridView1.Columns.Add(nameColumn);
            dataGridView1.Columns.Add(visibleColumn);
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            dataGridView1.CurrentCellDirtyStateChanged += dataGridView1_CurrentCellDirtyStateChanged;


            // Set the DataSource to the BindingList
            dataGridView1.DataSource = layers;

            dataGridView1.MouseMove += dataGridView1_MouseMove;
            dataGridView1.MouseDown += dataGridView1_MouseDown;
            dataGridView1.DragOver += dataGridView1_DragOver;
            dataGridView1.DragDrop += dataGridView1_DragDrop;

            unitComboBox.SelectedItem = 0;
            unitComboBox.SelectedText = "mm";
        }
        // Event handler for UserDeletingRow

        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;

        private void dataGridView1_MouseMove(object? sender, MouseEventArgs e)
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

        private void dataGridView1_MouseDown(object? sender, MouseEventArgs e)
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

        private void dataGridView1_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dataGridView1_DragDrop(object? sender, DragEventArgs e)
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
                DataGridViewRow? rowToMove = e.Data!.GetData(
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

        private void dataGridView1_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
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

        private void dataGridView1_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            // Commit the changes when the checkbox cell is clicked
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void importImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportImage importPopup = new();
            importPopup.ImportRequest += Import; // Subscribe to the event
            importPopup.ShowDialog();
        }
        private void Import(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                openFileDialog.Title = "Import Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    PictureBox pictureBox = new PictureBox();
                    Image importedImage;
                    try
                    {
                        importedImage = Image.FromFile(openFileDialog.FileName);
                        // Rest of the code to work with the imported image if the image is to big
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading the image: " + ex.Message);
                        return;
                    }

                    // Apply transparency
                    Bitmap bmp = new Bitmap(importedImage);
                    if (transparencyValue != 100)
                    {
                        int transparencyAlphaValue = (int)((transparencyValue / 100.0) * 255); // Assuming transparencyValue is from 0 to 100
                        for (int y = 0; y < bmp.Height; y++)
                        {
                            for (int x = 0; x < bmp.Width; x++)
                            {
                                Color c = bmp.GetPixel(x, y);
                                bmp.SetPixel(x, y, Color.FromArgb(transparencyAlphaValue, c.R, c.G, c.B));
                            }
                        }
                    }


                    pictureBox.Size = bmp.Size; // set size of PictureBox to the size of the imported image
                    pictureBox.Image = bmp;
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox.Location = new Point(200, 100);
                    pictureBox.BackColor = Color.Transparent;

                    AddPictureBox(pictureBox, false);
                }
            }
        }
        private void PictureBox_Click(object? sender, EventArgs e)
        {
            if (selectedLayer != null)
            {
                // Reset the border style of the previously selected Layer's PictureBox
                selectedLayer.PictureBox.BorderStyle = BorderStyle.None;
            }

            selectedLayer = layers.FirstOrDefault(layer => layer.PictureBox == (PictureBox)sender!);

            // Set the border style of the selected Layer's PictureBox
            selectedLayer!.PictureBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && currentlySelectedButton == toolStripButton15)
            {
                isDragging = true;
                startPoint = e.Location;
            }

            if (isDrawingEllipse)
            {
                initialMousePosition = e.Location;
            }

            if (isDrawingPolygon)
            {
                polygonPoints.Add(e.Location);
            }

            x = e.X;
            y = e.Y;
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                PictureBox pictureBox = (PictureBox)sender!;
                Point currentPoint = pictureBox.Location;
                int deltaX = e.X - startPoint.X;
                int deltaY = e.Y - startPoint.Y;
                currentPoint.Offset(deltaX, deltaY);
                pictureBox.Location = currentPoint;
            }

            if (isDrawingEllipse && e.Button == MouseButtons.Left)
            {
                //using (Graphics g = CreateGraphics())
                //{
                    int width = e.X - initialMousePosition.X;
                    int height = e.Y - initialMousePosition.Y;
                    Rectangle rect = new Rectangle(initialMousePosition.X, initialMousePosition.Y, width, height);
                    g.Clear(Color.Transparent);
                    g.DrawEllipse(Pens.White, rect);
                    g.FillEllipse(new SolidBrush(selectedColor), rect);
                    SelektujIliDeselektuj(toolStripButton13);
                //}
            }
            if (showCoordinates)
            {
                Point mousePositionInScreen = this.PointToClient(MousePosition);
                // subtracting the panel coordinates 
                string coordinates = "X: " + (mousePositionInScreen.X - panel1.Location.X) + ", Y: " + (mousePositionInScreen.Y - panel1.Location.Y);
                Point newLocation = new Point(MousePosition.X + 15, MousePosition.Y - 15);
                toolTip.Show(coordinates, this, newLocation.X - this.Location.X, newLocation.Y - this.Location.Y, 5000);
            }
            else
            {
                toolTip.Hide(this);
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            isDragging = false;
            isDrawingEllipse = false;

            lx = e.X;
            ly = e.Y;

            if (currentlySelectedButton == toolStripButton12)
            {
                if (selectedLayer is not null)
                    g.DrawLine(new Pen(new SolidBrush(selectedColor), strokeValue), new Point(x - selectedLayer.PictureBox.Left, y - selectedLayer.PictureBox.Top), new Point(lx - selectedLayer.PictureBox.Left, ly - selectedLayer.PictureBox.Top));
                else
                    g.DrawLine(new Pen(new SolidBrush(selectedColor), strokeValue), new Point(x, y), new Point(lx, ly));

                g.Dispose();
                SelektujIliDeselektuj(toolStripButton12);
            }
        }

        private void PictureBox_MouseClick(object? sender, MouseEventArgs e)
        {
            if (currentlySelectedButton == toolStripButton13)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (isDrawingPolygon)
                    {
                        polygonPoints.Add(e.Location);
                        this.Invalidate();
                    }

                    else
                    {
                        isDrawingPolygon = true;
                        polygonPoints.Clear();
                        polygonPoints.Add(e.Location);
                    }
                }

                else if (e.Button == MouseButtons.Right && isDrawingPolygon)
                {
                    isDrawingPolygon = false;
                    SelektujIliDeselektuj(toolStripButton13);
                    this.Invalidate();

                    if (polygonPoints.Count >= 2)
                    {
                        g.DrawPolygon(Pens.White, polygonPoints.ToArray());
                        g.FillPolygon(new SolidBrush(selectedColor), polygonPoints.ToArray());
                        polygonPoints.Clear();
                    }
                }
            }
            else if (currentlySelectedButton == toolStripButton16)
            {
                if (selectedLayer == null || selectedLayer.PictureBox == null)
                    return;

                if (originalPbImage is not null && point1.IsEmpty)
                    originalPbImage.Item1.Image = originalPbImage.Item2;

                PictureBox pictureBox = selectedLayer.PictureBox;

                if (pictureBox.Image == null)
                    bitmap = new Bitmap(pictureBox.Width, pictureBox.Height);
                else
                    bitmap = new Bitmap(pictureBox.Image);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    if (point1.IsEmpty)
                    {
                        originalPbImage = new Tuple<PictureBox, Image>(pictureBox, pictureBox.Image!);

                        panel1.Refresh();
                        label1.Text = "";
                        point1 = e.Location;
                        g.FillEllipse(Brushes.White, e.X - 3, e.Y - 3, 6, 6);
                    }
                    else if (point2.IsEmpty)
                    {
                        point2 = e.Location;
                        g.FillEllipse(Brushes.White, e.X - 3, e.Y - 3, 6, 6);

                        // Calculate distance
                        double distance = CalculateDistance(point1, point2);

                        Pen pen = new Pen(selectedColor, 2);
                        g.DrawLine(pen, point1, point2);

                        // Display distance
                        label1.ForeColor = selectedColor;
                        label1.Text = $"Distance: {distance:F2}";

                        // Reset points for the next calculation
                        point1 = Point.Empty;
                        point2 = Point.Empty;
                    }
                }

                pictureBox.Image = bitmap;
            }

        }

        // Add this field to your Form1 class
        private bool undoWasLastOperation = false;

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during the undo operation: " + ex.Message);
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during the redo operation: " + ex.Message);
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
                    Layer? previousLayer = null;
                    foreach (Layer layer in layers)
                    {
                        PictureBox pb = layer.PictureBox;
                        // If the PictureBox is visible and intersects with the panel's bounds, draw it on the bitmap
                        if (pb.Visible && pb.Bounds.IntersectsWith(panel1.Bounds))
                        {
                            if (previousLayer is not null && layer.PictureBox.Parent == previousLayer.PictureBox)
                            {
                                // Get the intersection rectangle between the current and previous layers
                                Rectangle intersection = Rectangle.Intersect(previousLayer.PictureBox.Bounds, pb.Bounds);

                                // Check if there is any intersection
                                if (!intersection.IsEmpty)
                                {
                                    // Calculate the location of the intersection on the current layer
                                    Point intersectionLocation = new Point(
                                        intersection.Left - pb.Left,
                                        intersection.Top - pb.Top
                                    );

                                    // Calculate the destination location on the bitmap
                                    PointF[] destinationLocations = new PointF[]
                                    {
                            new PointF(intersection.Left, intersection.Top),
                            new PointF(intersection.Right, intersection.Top),
                            new PointF(intersection.Left, intersection.Bottom)
                                    };

                                    // Draw the intersection part of the layer on the bitmap
                                    g.DrawImage(pb.Image, destinationLocations, new RectangleF(intersectionLocation, intersection.Size), GraphicsUnit.Pixel);
                                }
                            }
                            else
                            {
                                g.DrawImage(pb.Image, pb.Location);
                            }
                        }
                        previousLayer = layer;
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

            DeleteRow(selectedLayer);

            // Reset selected Layer
            selectedLayer = null;

            if (!undoToolStripMenuItem.Enabled)
                undoToolStripMenuItem.Enabled = true;
        }
        private void DeleteRow(Layer layerForDeletion)
        {
            foreach (Layer layer in layers)
                if (layer.PictureBox.Parent == layerForDeletion.PictureBox)
                    layer.PictureBox.Parent = layerForDeletion.PictureBox.Parent;

            layers.Remove(layerForDeletion);

            // Hide the PictureBox
            layerForDeletion.Visible = false;

            if (!undoToolStripMenuItem.Enabled)
                undoToolStripMenuItem.Enabled = true;
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) { SaveAs(); }
        private void SaveAs()
        {
            g.Dispose();
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
            g = CreateGraphics();
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

                    if (stateData != null && stateData.Layers != null)
                    {
                        // Create Layers from the deserialized StateData object
                        foreach (var layerDTO in stateData.Layers!)
                        {
                            Layer layer = new Layer(layerDTO.ToPictureBox(), layerDTO.IsDrawing);
                            layer.PictureBox.Location = layerDTO.Location;
                            AddPictureBox(layer.PictureBox, layerDTO.IsDrawing);
                        }
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
                layer?.PictureBox.Dispose();
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
            SelektujIliDeselektuj(toolStripButton18);
            addPictureBox();
            isDrawingEllipse = true;
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
            isDrawingPolygon = true;
        }

        private void TextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create a brush using the selectedColor variable
            Brush brush = new SolidBrush(selectedColor);

            string stringText = addString();

            // Draw the string using the brush with the selected color
            g.DrawString(stringText, new Font("Poppins", 12), brush, new Point(0, 0));

            // Dispose of the brush when done to free resources
            brush.Dispose();
        }

        private string addString()
        {
            string userInput = Interaction.InputBox("Enter text:", "Text Input");
            PictureBox pictureBox = new PictureBox();
            pictureBox.BackColor = Color.Transparent;
            pictureBox.Size = new Size(200, 100);
            pictureBox.Location = new Point(200, 100);
            pictureBox.BorderStyle = BorderStyle.None;
            Bitmap bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            g = Graphics.FromImage(bm);
            pictureBox.Image = bm;

            AddPictureBox(pictureBox, true);

            return userInput;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Environment.Exit(0); }

        public void BackToOrigin(object sender, EventArgs e)
        {
            if(selectedLayer is not null)
            {
                selectedLayer.PictureBox.Location = new Point(0, 0);
            }
        }
        private void MoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj((sender as ToolStripButton)!);
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton9);

            if (selectedLayer != null)
            {
                PictureBox pictureBox = selectedLayer.PictureBox;
                pictureBox.Width = (int)(pictureBox.Width * 1.2);
                pictureBox.Height = (int)(pictureBox.Height * 1.2);
            }
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton10);

            if (selectedLayer != null)
            {
                PictureBox pictureBox = selectedLayer.PictureBox;
                pictureBox.Width = (int)(pictureBox.Width / 1.2);
                pictureBox.Height = (int)(pictureBox.Height / 1.2);
            }
        }

        private void deselectToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (selectedLayer != null)
                selectedLayer.PictureBox.BorderStyle = BorderStyle.None;
            selectedLayer = null;
        }

        private ToolTip toolTip = new ToolTip();
        private bool showCoordinates = false;
        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj((sender as ToolStripButton)!);
            showCoordinates = !showCoordinates; // Toggle the state

        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (showCoordinates)
            {
                Point mousePositionInScreen = this.PointToClient(MousePosition);
                // subtracting the panel coordinates 
                string coordinates = "X: " + (mousePositionInScreen.X - panel1.Location.X) + ", Y: " + (mousePositionInScreen.Y - panel1.Location.Y);
                Point newLocation = new Point(MousePosition.X + 15, MousePosition.Y - 15);
                toolTip.Show(coordinates, this, newLocation.X - this.Location.X, newLocation.Y - this.Location.Y, 5000);
            }
            else
            {
                toolTip.Hide(this);
            }
        }

        private double CalculateConversionFactorToMillimeters()
        {
            using (Graphics g = this.CreateGraphics())
            {
                float dpiX = g.DpiX;
                // 1 inch is 25.4 millimeters
                // so 1 pixel is 25.4 / dpiX millimeters
                return 25.4 / dpiX;
            }
        }


        double conversionFactorToMillimeters = 0;
        private double CalculateDistance(Point p1, Point p2)
        {
            if (conversionFactorToMillimeters == 0)
                conversionFactorToMillimeters = CalculateConversionFactorToMillimeters();

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double distanceInPixels = Math.Sqrt(dx * dx + dy * dy);

            // Convert distance from pixels to the selected unit
            return unitIndex switch
            {
                0 => distanceInPixels * conversionFactorToMillimeters, // millimeters (mm)
                1 => distanceInPixels * conversionFactorToMillimeters * 1000, // micrometers (�m)
                2 => distanceInPixels * conversionFactorToMillimeters * 1000 * 1000, // nanometers (nm)
                _ => distanceInPixels, // Default to pixels if no valid unitIndex is provided
            };
        }
        private Bitmap bitmap;

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (currentlySelectedButton == toolStripButton16)
            {
                if (originalPbImage is not null && point1.IsEmpty)
                    originalPbImage.Item1.Image = originalPbImage.Item2;

                if (point1.IsEmpty)
                {
                    panel1.Refresh();
                    label1.Text = "";
                    point1 = e.Location;
                    panel1.CreateGraphics().FillEllipse(Brushes.White, e.X - 3, e.Y - 3, 6, 6);
                }
                else if (point2.IsEmpty)
                {
                    point2 = e.Location;
                    panel1.CreateGraphics().FillEllipse(Brushes.White, e.X - 3, e.Y - 3, 6, 6);

                    // Calculate distance
                    double distance = CalculateDistance(point1, point2);

                    string unitLabel = unitIndex switch
                    {
                        0 => "mm",
                        1 => "�m",
                        2 => "nm",
                        _ => "pixels",
                    };

                    // Display distance
                    label1.ForeColor = selectedColor;
                    label1.Text = $"Distance: {distance:F2} {unitLabel}";

                    Pen pen = new Pen(selectedColor, 2);
                    panel1.CreateGraphics().DrawLine(pen, point1, point2);

                    // Reset points for the next calculation
                    point1 = Point.Empty;
                    point2 = Point.Empty;
                }
            }
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void bottomMenuStripControl1_Load(object sender, EventArgs e)
        {

        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem1_Click_1(object sender, EventArgs e)
        {

        }

        private void toolStripButton60_Paint(object sender, PaintEventArgs e)
        {
            Color startColor = SystemColors.ControlDarkDark; // Replace with your desired start color.
            Color endColor = SystemColors.ActiveBorder; // Replace with your desired end color.

            // Create a linear gradient brush for the background.
            Rectangle rect = new Rectangle(Point.Empty, toolStripButton60.Size);
            LinearGradientBrush brush = new LinearGradientBrush(rect, startColor, endColor, LinearGradientMode.Vertical);

            // Draw the gradient background on the button.
            e.Graphics.FillRectangle(brush, rect);
        }

        private void strokeComboBox_Click(object sender, EventArgs e)
        {
            if (strokeComboBox.SelectedItem != null)
            {
                string selectedStroke = strokeComboBox.SelectedItem.ToString(); // Get the selected item as a string
                selectedStroke = selectedStroke.Replace("pt", "").Trim(); // Remove "pt" from the string

                // Try to convert the remaining string to an integer and assign it to strokeValue
                if (int.TryParse(selectedStroke, out int value))
                {
                    strokeValue = value;
                }
                else
                {
                    // Handle the error if the conversion fails
                    MessageBox.Show("Invalid stroke value selected!");
                }
            }
        }

        private void unitComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ToolStripComboBox comboBox = sender as ToolStripComboBox;
            if (comboBox != null)
            {
                unitIndex = comboBox.SelectedIndex;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            int value = trackBar1.Value; // Gets the current value of the track bar
            transparencyTextBox.Text = value * 10 + "%";
            transparencyValue = value * 10;
        }

        private void machineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Machine machinePopup = new();

            machinePopup.ShowDialog();
        }

        private void opacityComboBox_Click(object sender, EventArgs e)
        {
            if (opacityComboBox.SelectedItem != null)
            {
                string selectedopacity = opacityComboBox.SelectedItem!.ToString()!; // Get the selected item as a string

                // Check if the selected string contains the percentage symbol
                if (selectedopacity!.Contains('%'))
                {
                    selectedopacity = selectedopacity.Replace("%", "").Trim(); // Remove "%" from the string

                    if (int.TryParse(selectedopacity, out int value))
                    {
                        if (value >= 0 && value <= 100) // Check if the value is in the correct range
                        {
                            opacityValue = value;
                            int alphaValue = (int)(255 * (value / 100.0)); // Convert percentage to alpha value between 0 and 255
                            selectedColor = Color.FromArgb(alphaValue, selectedColor.R, selectedColor.G, selectedColor.B);
                        }
                        else
                        {
                            // The value is outside the expected range
                            MessageBox.Show("Opacity value must be between 0% and 100%!");
                        }
                    }
                    else
                    {
                        // Handle the error if the conversion fails
                        MessageBox.Show("Invalid opacity value selected!");
                    }
                }
                else
                {
                    // The selected string does not contain a percentage symbol
                    MessageBox.Show("Opacity value must be a percentage!");
                }
            }
        }


        private void RotateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelektujIliDeselektuj(toolStripButton5);

            if (selectedLayer != null)
            {
                selectedLayer.PictureBox.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                selectedLayer.PictureBox.Invalidate();
            }
        }
        
        private void SelektujIliDeselektujBoje(object sender, EventArgs e)
        {
            if (sender is ToolStripButton btn)
            {
                selectedColor = btn.BackColor;
                
                if(opacityValue != 100)
                {
                    int alphaValue = (int)(255 * (opacityValue / 100.0)); // Convert percentage to alpha value between 0 and 255
                    selectedColor = Color.FromArgb(alphaValue, selectedColor.R, selectedColor.G, selectedColor.B);
                }
            }
        }

        private void SelektujIliDeselektuj(ToolStripButton stripButton)
        {
            if (currentlySelectedButton == stripButton) // double selected, which means deselect
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
            pictureBox.BackColor = Color.Transparent;

            if (layers.Count == 0 || selectedLayer == null)
            {
                pictureBox.Size = new Size(panel1.Width, panel1.Height); // Set the desired size
                pictureBox.Location = new Point(0, 0);
            }
            
            else if (!selectedLayer.isDrawing)
            {
                pictureBox.Size = new Size(selectedLayer.PictureBox.Width, selectedLayer.PictureBox.Height); // Set the desired size
                pictureBox.Location = new Point(selectedLayer.PictureBox.Location.X - selectedLayer.PictureBox.Left, selectedLayer.PictureBox.Location.Y - selectedLayer.PictureBox.Top);
            }

            else
            {
                pictureBox.Size = new Size(selectedLayer.PictureBox.Width, selectedLayer.PictureBox.Height); // Set the desired size
                pictureBox.Location = selectedLayer.PictureBox.Location;
            }

            AddPictureBox(pictureBox, true);

            Bitmap bm = new Bitmap(pictureBox.Width, pictureBox.Height);
            g = Graphics.FromImage(bm);
            pictureBox.Image = bm;

            return pictureBox;
        }

        public void AddPictureBox(PictureBox pictureBox, bool isDrawing)
        {
            pictureBox.Name = "Layer: " + (i + 1);
            pictureBox.Parent = panel1;

            // If the new layer is added onto the panel when it has already a drawing, it is added to the drawing to still display it
            if (layers.Count != 0 && selectedLayer is null)
                selectedLayer = layers.FirstOrDefault(x => x.PictureBox.Parent == panel1 && x.isDrawing);

            if (layers.Count == 0 || selectedLayer is null)
                panel1.Controls.Add(pictureBox);
            else
            {
                // going to the the deepest drawing to add to the image so the drawings are visible
                while (selectedLayer.PictureBox.HasChildren)
                {
                    var newSelectedLayer = layers.FirstOrDefault(x => x.PictureBox.Parent == selectedLayer.PictureBox);
                    if (newSelectedLayer != null)
                        selectedLayer = newSelectedLayer;
                    else
                        break; // Exit the loop if no layers are found
                }

                if (pictureBox != selectedLayer.PictureBox)
                    selectedLayer.PictureBox.Controls.Add(pictureBox);
                else
                    panel1.Controls.Add(pictureBox);
            }

            Layer layer = new(pictureBox, isDrawing);
            layers.Add(layer);

            pictureBox.BringToFront();

            layer.PictureBox.Click += PictureBox_Click;
            layer.PictureBox.MouseDown += PictureBox_MouseDown;
            layer.PictureBox.MouseMove += PictureBox_MouseMove;
            layer.PictureBox.MouseUp += PictureBox_MouseUp;
            layer.PictureBox.MouseClick += PictureBox_MouseClick;

            undoStack.Push((layer, true, layers.Count - 1));

            i++;
        }

    }
}
