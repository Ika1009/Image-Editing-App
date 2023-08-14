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
        public delegate void ImportDelegate(object sender, EventArgs e);
        public event ImportDelegate ImportRequest;
        public ImportImage()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
        }
        private void Import(object sender, EventArgs e)
        {
            ImportRequest?.Invoke(this, new EventArgs());
            this.Close();
        }
    }
}
