using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace KinectOutput
{
    public class Coordinator
    {
        private static Dictionary<string, Transformer> results = new Dictionary<string, Transformer>();

        private class Transformer
        {
            public CalibrationResult Data { get; set; }
            public Func<double[], double[]> Transform { get; set; }
            public Func<double[], double[]> Inverse { get; set; }
        }

        private static string IdToFileName(string id)
        {
            return id.Replace("\\", "-") + ".xml";
        }

        private static void SaveResult(string id, CalibrationResult result)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CalibrationResult));
            using (var file = new StreamWriter(File.Open(IdToFileName(id), FileMode.Create)))
            {
                serializer.Serialize(file, result);
            }
        }

        public static CalibrationResult? GetCalibration(string id)
        {
            if (results.ContainsKey(id))
                return Coordinator.results[id].Data;
            return null;
        }

        public static void LoadCalibration(string id)
        {
            try
            {
                if (!File.Exists(IdToFileName(id)))
                    return;
                using (var file = new StreamReader(IdToFileName(id)))
                {
                    XDocument doc = XDocument.Load(file);
                    var ns = doc.Elements().First().Name.Namespace;
                    var result = doc.Elements().Select(row => new CalibrationResult()
                    {
                        P0 = DenseVector.OfEnumerable(row.Element(ns + "P0").Elements().Select(d => double.Parse(d.Value, CultureInfo.InvariantCulture))),
                        F1 = DenseVector.OfEnumerable(row.Element(ns + "F1").Elements().Select(d => double.Parse(d.Value, CultureInfo.InvariantCulture))),
                        F2 = DenseVector.OfEnumerable(row.Element(ns + "F2").Elements().Select(d => double.Parse(d.Value, CultureInfo.InvariantCulture))),
                        F3 = DenseVector.OfEnumerable(row.Element(ns + "F3").Elements().Select(d => double.Parse(d.Value, CultureInfo.InvariantCulture))),
                    }).First();
                    _setResult(id, result);
                }
            }
            catch (Exception) { }
        }

        public static void SetResult(KinectSensor sensor, CalibrationResult result)
        {
            SetResult(sensor.UniqueKinectId, result);
        }

        public static void SetResult(string id, CalibrationResult result)
        {
            SaveResult(id, result);
            _setResult(id, result);
        }
        public static void _setResult(string id, CalibrationResult result)
        {
            if (result.P0 == null || result.F1 == null || result.F2 == null || result.F3 == null)
                return;
            var mat = DenseMatrix.OfColumns(4, 4, new double[][] { result.F1.Concat(new double[] {0}).ToArray(),
                result.F2.Concat(new double[] {0}).ToArray(),
                result.F3.Concat(new double[] {0}).ToArray(),
                result.P0.Concat(new double[] {1}).ToArray() });

            if (result.A1 != null && result.A2 != null && result.A3 != null)
            {
                var matA = DenseMatrix.OfColumns(4,4, new double[][] { result.A1.ToArray(), result.A2.ToArray(), result.A3.ToArray(), new double[4] { 0, 0, 0, 1} });
                mat = matA * mat;
            }
            var inv = mat.Inverse();
            results[id] = new Transformer()
            {
                Data = result,
                Transform = (v) =>
                {
                    if (v.Count() == 3)
                        return inv.Multiply(DenseVector.OfEnumerable(v.Concat(new double[] { 1 }))).ToArray();
                    else
                        return inv.Multiply(DenseVector.OfEnumerable(v)).ToArray();
                },
                Inverse = (v) =>
                {
                    if (v.Count() == 3)
                        return mat.Multiply(DenseVector.OfEnumerable(v.Concat(new double[] { 1 }))).ToArray();
                    else
                        return mat.Multiply(DenseVector.OfEnumerable(v)).ToArray();
                }
            };
        }

        private static Func<double[], double[]> Identity = (v) => v;

        public static Func<double[], double[]> GetInverse(string id)
        {
            if (results.ContainsKey(id))
                return results[id].Inverse;
            return Identity;
        }

        public static Func<double[], double[]> GetTransform(string id)
        {
            if (results.ContainsKey(id))
                return results[id].Transform;
            return Identity;
        }

        public static Func<double[], double[]> GetTransform(KinectSensor sensor)
        {
            return GetTransform(sensor.UniqueKinectId);
        }
    }
}
