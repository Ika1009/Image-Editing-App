using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Image_Editing_app
{
    public class Layer
    {
        public static int counter = 1;
        private bool visible;
        public bool isDrawing;
        public PictureBox PictureBox { get; set; }
        public string Name { get; set; }
        public bool Visible 
        { 
            get => visible;
            set
            {
                if(value == false)
                {
                    visible = false;
                    PictureBox.Visible = false;
                }
                else if (value == true)
                {
                    visible = true;
                    PictureBox.Visible = true;
                }
            } 
        }
        public DataGridViewCheckBoxCell CheckboxCell { get; set; }
        public Layer(PictureBox pictureBox, bool isDrawing)
        {
            PictureBox = pictureBox;
            Name = "Layer " + counter;
            Visible = true;

            CheckboxCell = new DataGridViewCheckBoxCell();
            CheckboxCell.Value = Visible;
            counter++;
            this.isDrawing = isDrawing;
        }
    }
}
