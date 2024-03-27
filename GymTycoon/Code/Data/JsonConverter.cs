using GymTycoon.Code.Common;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace GymTycoon.Code.Data
{
    public class Point3Converter : JsonConverter
    {
        private readonly Type[] _types;

        public Point3Converter()
        {
            _types = [];
        }

        public Point3Converter(params Type[] types)
        {
            _types = types;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point3) || objectType == typeof(Point3[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Point3[]))
            {
                int[][] arr = serializer.Deserialize<int[][]>(reader);
                Point3[] points = new Point3[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    points[i] = new Point3(arr[i]);
                }

                return points;
            }

            int[] obj = (int[])reader.Value;
            return new Point3(obj);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value.GetType() == typeof(Point3[]))
            {
                Point3[] points = (Point3[])value;
                writer.WriteStartArray();

                for (int i = 0; i < points.Length; i++)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(points[i].X);
                    writer.WriteValue(points[i].Y);
                    writer.WriteValue(points[i].Z);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
                return;
            }

            Point3 p = (Point3)value;
            writer.WriteStartArray();
            writer.WriteValue(p.X);
            writer.WriteValue(p.Y);
            writer.WriteValue(p.Z);
            writer.WriteEndArray();
        }
    }

    public class PointConverter : JsonConverter
    {
        private readonly Type[] _types;

        public PointConverter()
        {
            _types = [];
        }

        public PointConverter(params Type[] types)
        {
            _types = types;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point) || objectType == typeof(Point[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Point[]))
            {
                int[][] arr = serializer.Deserialize<int[][]>(reader);
                Point[] points = new Point[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    points[i] = new Point(arr[i][0], arr[i][1]);
                }

                return points;
            }

            int[] obj = (int[])reader.Value;
            return new Point(obj[0], obj[1]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value.GetType() == typeof(Point[]))
            {
                Point[] points = (Point[])value;
                writer.WriteStartArray();

                for (int i = 0; i < points.Length; i++)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(points[i].X);
                    writer.WriteValue(points[i].Y);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
                return;
            }

            Point p = (Point)value;
            writer.WriteStartArray();
            writer.WriteValue(p.X);
            writer.WriteValue(p.Y);
            writer.WriteEndArray();
        }
    }

}
