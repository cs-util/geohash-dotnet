﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NetTopologySuite.Geometries;
using System.Threading.Tasks;
using NetTopologySuite.Geometries.Prepared;

namespace Geohash
{
    /// <summary>
    /// Geohasher 
    /// </summary>
    public static class Geohasher
    {
        private static readonly char[] base32Chars = "0123456789bcdefghjkmnpqrstuvwxyz".ToCharArray();

        private static readonly int[] bits= { 16, 8, 4, 2, 1 };

        /// <summary>
        /// Encodes coordinates to a geohash string.
        /// </summary>
        /// <param name="latitude">latitude</param>
        /// <param name="longitude">longitude</param>
        /// <param name="precision">Length of the geohash. Must be between 1 and 12. Defaults to 6.</param>
        /// <returns>The created geoash for the given coordinates.</returns>
        public static string Encode(double latitude, double longitude, int precision = 6)
        {
            Validate(latitude, longitude);

            if (precision < 1 || precision > 12)
            {
                throw new ArgumentException("precision must be between 1 and 12");
            }

            double[] latInterval = { -90.0, 90.0 };
            double[] lonInterval = { -180.0, 180.0 };

            var geohash = new StringBuilder();
            bool isEven = true;
            int bit = 0;
            int ch = 0;

            while (geohash.Length < precision)
            {
                double mid;

                if (isEven)
                {
                    mid = (lonInterval[0] + lonInterval[1]) / 2;

                    if (longitude > mid)
                    {
                        ch |= bits[bit];
                        lonInterval[0] = mid;
                    }
                    else
                    {
                        lonInterval[1] = mid;
                    }

                }
                else
                {
                    mid = (latInterval[0] + latInterval[1]) / 2;

                    if (latitude > mid)
                    {
                        ch |= bits[bit];
                        latInterval[0] = mid;
                    }
                    else
                    {
                        latInterval[1] = mid;
                    }
                }

                isEven = !isEven;

                if (bit < 4)
                {
                    bit++;
                }
                else
                {
                    geohash.Append(base32Chars[ch]);
                    bit = 0;
                    ch = 0;
                }
            }

            return geohash.ToString();
        }

        /// <summary>
        /// Return the 32 subhashes for the given geohash string.
        /// </summary>
        /// <param name="geohash">geohash for which to get the subhashes.</param>
        /// <returns>subhashes</returns>
        public static string[] GetSubhashes(string geohash)
        {
            if (String.IsNullOrEmpty(geohash)) throw new ArgumentNullException("geohash");
            if (geohash.Length > 11) throw new ArgumentException("geohash length must be < 12");

            return base32Chars.Select(x => $"{geohash}{x}").ToArray();
        }

        /// <summary>
        /// Decodes a geohash to the corresponding coordinates.
        /// </summary>
        /// <param name="geohash">geohash for which to get the coordinates</param>
        /// <returns>Tuple with latitude and longitude</returns>
        public static Tuple<double, double> Decode(string geohash)
        {
            if (String.IsNullOrEmpty(geohash)) throw new ArgumentNullException("geohash");
            if (geohash.Length > 12) throw new ArgumentException("geohash length > 12");

            double[] bbox = GetBoundingBox(geohash);

            double latitude = (bbox[0] + bbox[1]) / 2;
            double longitude = (bbox[2] + bbox[3]) / 2;

            return Tuple.Create(latitude, longitude);
        }


        /// <summary>
        /// Returns the neighbor for a given geohash and directions.
        /// </summary>
        /// <param name="geohash">geohash for which to find the neighbor</param>
        /// <param name="direction">direction of the neighbor</param>
        /// <returns>geohash</returns>
        public static string GetNeighbor(string geohash, Direction direction)
        {
            if (String.IsNullOrEmpty(geohash)) throw new ArgumentNullException("geohash");
            if (geohash.Length > 12) throw new ArgumentException("geohash length > 12");
            var neighbors = CreateNeighbors(geohash);
            return neighbors[direction];
        }

        /// <summary>
        /// Returns all neighbors for a given geohash.
        /// </summary>
        /// <param name="geohash">geohash for which to find the neighbors</param>
        /// <returns>Dictionary with direction and geohash</returns>
        public static Dictionary<Direction,string> GetNeighbors(string geohash)
        {
            if (String.IsNullOrEmpty(geohash)) throw new ArgumentNullException("geohash");
            if (geohash.Length > 12) throw new ArgumentException("geohash length > 12");
            return CreateNeighbors(geohash);
        }

        /// <summary>
        /// Returns the parent of the given geohash.
        /// </summary>
        /// <param name="geohash">geohash for which to get the parent.</param>
        /// <returns>parent geohash</returns>
        public static string GetParent(string geohash)
        {
            ValidateGeohash(geohash);
            return geohash.Substring(0, geohash.Length - 1);
        }

        /// <summary>
        /// returns the bounding box for the given geoash
        /// </summary>
        /// <param name="geohash">geohash for which to get the bounding box</param>
        /// <returns>bounding box as double[] containing latInterval[0], latInterval[1], lonInterval[0], lonInterval[1]</returns>
        public static double[] GetBoundingBox(string geohash)
        {
            ValidateGeohash(geohash);

            double[] latInterval = { -90.0, 90.0 };
            double[] lonInterval = { -180.0, 180.0 };

            bool isEven = true;
            for (int i = 0; i < geohash.Length; i++)
            {

                int currentCharacter = Array.IndexOf(base32Chars, geohash[i]);

                for (int z = 0; z < bits.Length; z++)
                {
                    int mask = bits[z];
                    if (isEven)
                    {
                        if ((currentCharacter & mask) != 0)
                        {
                            lonInterval[0] = (lonInterval[0] + lonInterval[1]) / 2;
                        }
                        else
                        {
                            lonInterval[1] = (lonInterval[0] + lonInterval[1]) / 2;
                        }

                    }
                    else
                    {

                        if ((currentCharacter & mask) != 0)
                        {
                            latInterval[0] = (latInterval[0] + latInterval[1]) / 2;
                        }
                        else
                        {
                            latInterval[1] = (latInterval[0] + latInterval[1]) / 2;
                        }
                    }
                    isEven = !isEven;
                }
            }

            return new double[] { latInterval[0], latInterval[1], lonInterval[0], lonInterval[1] };
        }

        /// <summary>
        /// Return Hashes for a given polygon
        /// </summary>
        /// <param name="startingHash">Starting Position, e.g use centroid.x and centroid.y</param>
        /// <param name="polygon">Polygon for which to create hashes</param>
        /// <param name="precision">Precision of the hashes, defaults to 6</param>
        /// <param name="mode">Fill Mode for the hashes</param>
        /// <param name="progress">Allows reporting progress</param>
        /// <returns></returns>
        [Obsolete("Dont use until https://github.com/Postlagerkarte/geohash-dotnet/issues/47 is clarified")]
        public static List<string> GetHashes(string startingHash, IPreparedGeometry polygon, int precision = 6, Mode mode = Mode.Contains, IProgress<HashingProgress> progress = null)
        {
            return new PolygonHasher().GetHashes(startingHash, polygon, precision, mode, progress);
        }

        private static void ValidateGeohash(string geohash)
        {
            if (String.IsNullOrEmpty(geohash)) throw new ArgumentNullException("geohash");
            if (geohash.Length > 12) throw new ArgumentException("geohash length > 12");
        }

        private static Dictionary<Direction, string> CreateNeighbors(string geohash)
        {
            var result = new Dictionary<Direction, string>();
            result.Add(Direction.North, North(geohash));
            result.Add(Direction.NorthWest, West(result[Direction.North]));
            result.Add(Direction.NorthEast, East(result[Direction.North]));
            result.Add(Direction.East, East(geohash));
            result.Add(Direction.South, South(geohash));
            result.Add(Direction.SouthWest, West(result[Direction.South]));
            result.Add(Direction.SouthEast, East(result[Direction.South]));
            result.Add(Direction.West, West(geohash));
            return result;
        }

        private static void Validate(double latitude, double longitude)
        {
            if (latitude < -90.0 || latitude > 90.0)
            {
                throw new ArgumentException("Latitude " + latitude + " is outside valid range of [-90,90]");
            }
            if (longitude < -180.0 || longitude > 180.0)
            {
                throw new ArgumentException("Longitude " + longitude + " is outside valid range of [-180,180]");
            }
        }



        private static string South(string geoHash)
        {
            double[] bbox = GetBoundingBox(geoHash);
            double latDiff = bbox[1] - bbox[0];
            double lat = bbox[0] - latDiff / 2;
            double lon = (bbox[2] + bbox[3]) / 2;

            if (lat < -90)
            {
                lat = (-90 + (-90 - lat)) * -1;
            }


            return Encode(lat, lon, geoHash.Length);
        }

        private static string North(string geoHash)
        {
            double[] bbox = GetBoundingBox(geoHash);
            double latDiff = bbox[1] - bbox[0];
            double lat = bbox[1] + latDiff / 2;

            if (lat > 90)
            {
                lat = (90 - (lat - 90)) * -1; 
            }

            double lon = (bbox[2] + bbox[3]) / 2;
            return Encode(lat, lon, geoHash.Length);
        }

        private static string West(string geoHash)
        {
            double[] bbox = GetBoundingBox(geoHash);
            double lonDiff = bbox[3] - bbox[2];
            double lat = (bbox[0] + bbox[1]) / 2;
            double lon = bbox[2] - lonDiff / 2;
            if (lon < -180)
            {
                lon = 180 - (lon + 180);
            }
            if (lon > 180)
            {
                lon = 180;
            }

            return Encode(lat, lon, geoHash.Length);
        }

        private static string East(string geoHash)
        {
            double[] bbox = GetBoundingBox(geoHash);
            double lonDiff = bbox[3] - bbox[2];
            double lat = (bbox[0] + bbox[1]) / 2;
            double lon = bbox[3] + lonDiff / 2;

            if (lon > 180)
            {
                lon = -180 + (lon - 180);
            }
            if (lon < -180)
            {
                lon = -180;
            }

            return Encode(lat, lon, geoHash.Length);
        }
    }
}
