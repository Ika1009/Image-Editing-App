using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Image_Editing_app
{
    public class DrawingController
    {
        private PictureBox _pictureBox;
        private Drawing _drawing;

        public DrawingController(PictureBox pictureBox)
        {
            _pictureBox = pictureBox;
            _drawing = new Drawing();
        }

        public void DrawShape(Shape shape)
        {
            _drawing.AddShape(shape);
            RefreshPictureBox();
        }

        private void RefreshPictureBox()
        {
            var bitmap = new Bitmap(_pictureBox.Width, _pictureBox.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                foreach (var shape in _drawing.Shapes)
                {
                    shape.Draw(g);
                }
            }
            _pictureBox.Image = bitmap;
        }
    }

    public class Drawing
    {
        private List<Shape> _shapes;

        public Drawing()
        {
            _shapes = new List<Shape>();
        }

        public void AddShape(Shape shape)
        {
            _shapes.Add(shape);
        }

        public IReadOnlyList<Shape> Shapes => _shapes.AsReadOnly();
    }

    public abstract class Shape
    {
        public abstract void Draw(Graphics graphics);
    }

    public class Line : Shape
    {
        public Point Start { get; }
        public Point End { get; }
        public Pen Pen { get; }

        public Line(Point start, Point end, Pen pen)
        {
            Start = start;
            End = end;
            Pen = pen;
        }

        public override void Draw(Graphics graphics)
        {
            graphics.DrawLine(Pen, Start, End);
        }
    }

    public class Rectangle : Shape
    {
        public System.Drawing.Rectangle Rect { get; }
        public Pen Pen { get; }

        public Rectangle(System.Drawing.Rectangle rect, Pen pen)
        {
            Rect = rect;
            Pen = pen;
        }

        public override void Draw(Graphics graphics)
        {
            graphics.DrawRectangle(Pen, Rect);
        }
    }

    public class Circle : Shape
    {
        public Point Center { get; }
        public int Radius { get; }
        public Pen Pen { get; }

        public Circle(Point center, int radius, Pen pen)
        {
            Center = center;
            Radius = radius;
            Pen = pen;
        }

        public override void Draw(Graphics graphics)
        {
            graphics.DrawEllipse(Pen, Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);
        }
    }
}