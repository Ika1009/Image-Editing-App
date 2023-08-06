using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Image_Editing_app
{
    public class IconDataGridViewCheckBoxColumn : DataGridViewCheckBoxColumn
    {
        public IconDataGridViewCheckBoxColumn()
        {
            // Set the cell style to use custom painting
            this.CellTemplate = new IconDataGridViewCheckBoxCell();
        }
    }

    public class IconDataGridViewCheckBoxCell : DataGridViewCheckBoxCell
    {
        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates elementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            // Call the base Paint method to draw the checkbox
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);

            // Check if the checkbox is checked
            bool isChecked = (bool)value;

            // Load your custom icon image
            Image icon = Properties.Resources.eyeBlack;
            Image icon2 = Properties.Resources.eyeBlackClosed;
            int newWidth = 30; // Specify the desired width
            int newHeight = 20; // Specify the desired height


            Image resizedImage = icon.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero);
            Image resizedImage2 = icon2.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero);

            // Calculate the position to draw the icon in the cell
            int iconX = cellBounds.Left + (cellBounds.Width - resizedImage.Width) / 2;
            int iconY = cellBounds.Top + (cellBounds.Height - resizedImage.Height) / 2;

            int iconX2 = cellBounds.Left + (cellBounds.Width - resizedImage2.Width) / 2;
            int iconY2 = cellBounds.Top + (cellBounds.Height - resizedImage2.Height) / 2;

            // Draw the icon in the cell if the checkbox is checked
            if (isChecked)
            {
                graphics.DrawImage(resizedImage, iconX, iconY, resizedImage.Width, resizedImage.Height);
            }

            else
            {
                graphics.DrawImage(resizedImage2, iconX2, iconY2, resizedImage2.Width, resizedImage2.Height);
            }
        }
    }
}
