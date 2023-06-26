using Image_Editing_app;
using System.Text.Json;

public class StateData
{
    public List<LayerDTO> Layers { get; set; }

    public int Brojac { get; set; }

    public string SerializeToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public static StateData DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<StateData>(json);
    }

    public class LayerDTO
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] ImageBytes { get; set; }
        public Point Location { get; set; }

        public PictureBox ToPictureBox()
        {
            PictureBox pictureBox = new PictureBox();
            pictureBox.Width = Width;
            pictureBox.Height = Height;
            pictureBox.Image = ByteArrayToImage(ImageBytes);
            pictureBox.Location = Location;
            return pictureBox;
        }

        public static LayerDTO FromPictureBox(PictureBox pictureBox)
        {
            Image image = pictureBox.Image;
            byte[] imageBytes = ImageToByteArray(image);

            return new LayerDTO
            {
                Width = pictureBox.Width,
                Height = pictureBox.Height,
                ImageBytes = imageBytes,
                Location = pictureBox.Location
            };
        }
        public Layer ToLayer()
        {
            Layer layer = new Layer(ToPictureBox());
            layer.PictureBox.Location = Location;
            return layer;
        }

        public static LayerDTO FromLayer(Layer layer)
        {
            PictureBox pictureBox = layer.PictureBox;
            Image image = pictureBox.Image;
            byte[] imageBytes = ImageToByteArray(image);

            return new LayerDTO
            {
                Width = pictureBox.Width,
                Height = pictureBox.Height,
                ImageBytes = imageBytes,
                Location = pictureBox.Location
            };
        }
        private static byte[] ImageToByteArray(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat);
                return ms.ToArray();
            }
        }

        private static Image ByteArrayToImage(byte[] byteArray)
        {
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                return Image.FromStream(ms);
            }
        }
    }
}