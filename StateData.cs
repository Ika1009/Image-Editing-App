using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Image_Editing_app
{
    [Serializable]
    public class StateData
    {
        public List<LayerDTO> Layers { get; set; }

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

            public PictureBox ToPictureBox()
            {
                PictureBox pictureBox = new PictureBox();
                pictureBox.Width = Width;
                pictureBox.Height = Height;
                pictureBox.Image = ByteArrayToImage(ImageBytes);
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
                    ImageBytes = imageBytes
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
}
