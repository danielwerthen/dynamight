﻿using MathNet.Numerics.LinearAlgebra.Double;
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
            var mat = DenseMatrix.OfColumns(4, 4, new double[][] { result.F1.Concat(new double[] {0}).ToArray(),
                result.F2.Concat(new double[] {0}).ToArray(),
                result.F3.Concat(new double[] {0}).ToArray(),
                result.P0.Concat(new double[] {1}).ToArray() }).Inverse();
            results[id] = new Transformer()
            {
                Data = result,
                Transform = (v) =>
                {
                    return mat.Multiply(DenseVector.OfEnumerable(v.Concat(new double[] { 1 }))).ToArray();
                }
            };
        }

        private static Func<double[], double[]> Identity = (v) => v;

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
