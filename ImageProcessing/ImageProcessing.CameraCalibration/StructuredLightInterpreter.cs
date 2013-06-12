using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    public class StructuredLightInterpreter
    {
        private StructuredLightInterpreter()
        {
        }
        public class MapRecord
        {
            public Bitmap Map;
            public int Step;
            public int Pixels;
            public bool Row;
        }
        private Bitmap Offmap;
        private List<MapRecord> records = new List<MapRecord>();

        public static Action<System.Drawing.Bitmap> Build(Action ProjOff, Func<MapRecord> ProjStep, out Func<List<Point>, List<Point>> transformPoints)
        {
            var interp = new StructuredLightInterpreter();
            //Turn projector off;
            ProjOff();
            Action<Bitmap> current;
            Action<Bitmap> registerMap = null;
            MapRecord currentRecord = null;
            Action<Bitmap> registerOffmap = (bitmap) =>
            {
                interp = new CameraCalibration.StructuredLightInterpreter();
                interp.Offmap = bitmap;
                current = registerMap;
                currentRecord = ProjStep();
            };
            registerMap = (bitmap) =>
            {
                currentRecord.Map = bitmap;
                interp.records.Add(currentRecord);
                currentRecord = ProjStep();
                if (currentRecord == null)
                    current = registerOffmap;
            };
            current = registerOffmap;
            transformPoints = (camera) =>
            {
                var result = new List<Point>();
                return result;
            };
            return (bitmap) =>
            {
                current(bitmap);
            };
        }
    }
}

