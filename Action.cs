using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Image_Editing_app
{
    public class Action
    {
        public PictureBox PictureBox { get; set; }
        public string ActionType { get; set; }
        public Point PreviousLocation { get; set; }
        public Image DeletedImage { get; set; }
    }
}
