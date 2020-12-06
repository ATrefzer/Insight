using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using OuelletConvexHull;

namespace Visualization.Controls.Utility
{
    // http://csharphelper.com/blog/2014/07/determine-whether-a-point-is-inside-a-polygon-in-c/
    // http://csharphelper.com/blog/2014/08/find-a-minimal-bounding-circle-of-a-set-of-points-in-c/

    internal static class Geometry
    {
        public static double Epsilon = 0.0000001;

        // Return the cross product AB x BC.
        // The cross product is a vector perpendicular to AB
        // and BC having length |AB| * |BC| * Sin(theta) and
        // with direction given by the right-hand rule.
        // For two vectors in the X-Y plane, the result is a
        // vector with X and Y components 0 so the Z component
        // gives the vector's length and direction.
        public static double CrossProductLength(double Ax, double Ay,
                                                double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            var BAx = Ax - Bx;
            var BAy = Ay - By;
            var BCx = Cx - Bx;
            var BCy = Cy - By;

            // Calculate the Z coordinate of the cross product.
            return BAx * BCy - BAy * BCx;
        }

        // Find the polygon's centroid.
        public static Point FindCentroid(List<Point> points)
        {
            // Add the first point at the end of the array.
            var num_points = points.Count;
            var pts = new Point[num_points + 1];
            points.CopyTo(pts, 0);
            pts[num_points] = points[0];

            // Find the centroid.
            double X = 0;
            double Y = 0;
            double second_factor;
            for (var i = 0; i < num_points; i++)
            {
                second_factor =
                        pts[i].X * pts[i + 1].Y -
                        pts[i + 1].X * pts[i].Y;
                X += (pts[i].X + pts[i + 1].X) * second_factor;
                Y += (pts[i].Y + pts[i + 1].Y) * second_factor;
            }

            // Divide by 6 times the polygon's area.
            var polygon_area = PolygonArea(points);
            X /= 6 * polygon_area;
            Y /= 6 * polygon_area;

            // If the values are negative, the polygon is
            // oriented counterclockwise so reverse the signs.
            if (X < 0)
            {
                X = -X;
                Y = -Y;
            }

            return new Point(X, Y);
        }

        public static int FindCircleCircleIntersections(Point center0, double radius0, Point center1, double radius1,
                                                        out Point intersection1, out Point intersection2)
        {
            return FindCircleCircleIntersections(center0.X, center0.Y, radius0, center1.X, center1.Y, radius1, out intersection1, out intersection2);
        }

        // Find the points where the two circles intersect.
        public static int FindCircleCircleIntersections(
                double cx0, double cy0, double radius0,
                double cx1, double cy1, double radius1,
                out Point intersection1, out Point intersection2)
        {
            // Find the distance between the centers.
            var dx = cx0 - cx1;
            var dy = cy0 - cy1;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            // See how many solutions there are.
            if (dist > radius0 + radius1 + Epsilon) // TODO atr: Fixed rounding errors
            {
                // No solutions, the circles are too far apart.
                intersection1 = new Point(double.NaN, double.NaN);
                intersection2 = new Point(double.NaN, double.NaN);
                return 0;
            }
            else if (dist + Epsilon < Math.Abs(radius0 - radius1)) // TODO atr: Fixed rounding errors
            {
                // No solutions, one circle contains the other.
                intersection1 = new Point(double.NaN, double.NaN);
                intersection2 = new Point(double.NaN, double.NaN);
                return -1;
            }
            else if (dist == 0 && radius0 == radius1)
            {
                // No solutions, the circles coincide.
                intersection1 = new Point(double.NaN, double.NaN);
                intersection2 = new Point(double.NaN, double.NaN);
                return -1;
            }
            else
            {
                // Find a and h.
                var a = (radius0 * radius0 -
                         radius1 * radius1 + dist * dist) / (2 * dist);

                // TODO atr Fixed rounding issues. I already know here that a valid h must exist.
                var radicand = radius0 * radius0 - a * a;
                if (radicand < 0)
                {
                    radicand = 0.0;
                }

                var h = Math.Sqrt(radicand);

                // Find P2.
                var cx2 = cx0 + a * (cx1 - cx0) / dist;
                var cy2 = cy0 + a * (cy1 - cy0) / dist;

                // Get the points P3.
                intersection1 = new Point(
                                          cx2 + h * (cy1 - cy0) / dist,
                                          cy2 - h * (cx1 - cx0) / dist);
                intersection2 = new Point(
                                          cx2 - h * (cy1 - cy0) / dist,
                                          cy2 + h * (cx1 - cx0) / dist);

                // See if we have 1 or 2 solutions.
                if (Math.Abs(dist - (radius0 + radius1)) < Epsilon)
                {
                    return 1;
                }

                return 2;
            }
        }


        // Find the points of intersection.
        public static int FindLineCircleIntersections(
                double cx, double cy, double radius,
                Point point1, Point point2,
                out Point intersection1, out Point intersection2)
        {
            double dx, dy, A, B, C, det, t;

            dx = point2.X - point1.X;
            dy = point2.Y - point1.Y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.X - cx) + dy * (point1.Y - cy));
            C = (point1.X - cx) * (point1.X - cx) +
                (point1.Y - cy) * (point1.Y - cy) -
                radius * radius;

            det = B * B - 4 * A * C;
            if (A <= 0.0000001 || det < 0)
            {
                // No real solutions.
                intersection1 = new Point(double.NaN, double.NaN);
                intersection2 = new Point(double.NaN, double.NaN);
                return 0;
            }
            else if (det == 0)
            {
                // One solution. // TODO atr do we ever have exactly zero.
                t = -B / (2 * A);
                intersection1 =
                        new Point(point1.X + t * dx, point1.Y + t * dy);
                intersection2 = new Point(double.NaN, double.NaN);
                return 1;
            }
            else
            {
                // Two solutions.
                t = (-B + Math.Sqrt(det)) / (2 * A);
                intersection1 =
                        new Point(point1.X + t * dx, point1.Y + t * dy);
                t = (-B - Math.Sqrt(det)) / (2 * A);
                intersection2 =
                        new Point(point1.X + t * dx, point1.Y + t * dy);
                return 2;
            }
        }

        // Find a minimal bounding circle.
        public static void FindMinimalBoundingCircle(List<Point> points, out Point center, out double radius)
        {
            // Find the convex hull.
            var hull = MakeConvexHull(points);

            // The best solution so far.
            var best_center = points[0];
            var best_radius2 = double.MaxValue;

            // Look at pairs of hull points.
            for (var i = 0; i < hull.Count - 1; i++)
            {
                for (var j = i + 1; j < hull.Count; j++)
                {
                    // Find the circle through these two points.
                    var test_center = new Point(
                                                (hull[i].X + hull[j].X) / 2f,
                                                (hull[i].Y + hull[j].Y) / 2f);
                    var dx = test_center.X - hull[i].X;
                    var dy = test_center.Y - hull[i].Y;
                    var test_radius2 = dx * dx + dy * dy;

                    // See if this circle would be an improvement.
                    if (test_radius2 < best_radius2)
                    {
                        // See if this circle encloses all of the points.
                        if (CircleEnclosesPoints(test_center, test_radius2, hull, i, j, -1))
                        {
                            // Save this solution.
                            best_center = test_center;
                            best_radius2 = test_radius2;
                        }
                    }
                } // for i
            } // for j

            // Look at triples of hull points.
            for (var i = 0; i < hull.Count - 2; i++)
            {
                for (var j = i + 1; j < hull.Count - 1; j++)
                {
                    for (var k = j + 1; k < hull.Count; k++)
                    {
                        // Find the circle through these three points.
                        Point test_center;
                        double test_radius2;
                        FindCircle(hull[i], hull[j], hull[k], out test_center, out test_radius2);

                        // See if this circle would be an improvement.
                        if (test_radius2 < best_radius2)
                        {
                            // See if this circle encloses all of the points.
                            if (CircleEnclosesPoints(test_center, test_radius2, hull, i, j, k))
                            {
                                // Save this solution.
                                best_center = test_center;
                                best_radius2 = test_radius2;
                            }
                        }
                    } // for k
                } // for i
            } // for j

            center = best_center;
            if (best_radius2 == double.MaxValue)
            {
                radius = 0;
            }
            else
            {
                radius = Math.Sqrt(best_radius2);
            }
        }

        // Return the angle ABC.
        // Return a value between PI and -PI.
        // Note that the value is the opposite of what you might
        // expect because Y coordinates increase downward.
        public static double GetAngle(double Ax, double Ay,
                                      double Bx, double By, double Cx, double Cy)
        {
            // Get the dot product.
            var dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);

            // Get the cross product.
            var cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

            // Calculate the angle.
            return Math.Atan2(cross_product, dot_product);
        }

        // Return the points that make up a polygon's convex hull.
        // This method leaves the points list unchanged.
        public static List<Point> MakeConvexHull(List<Point> points)
        {
            var convexHull = new ConvexHull(points);
            convexHull.CalcConvexHull(ConvexHullThreadUsage.OnlyOne);
            return convexHull.GetResultsAsArrayOfPoint().ToList();
        }

        // Moves the point along the line "length" units way from from the center.
        public static Point MovePointAlongLine(Point origin, Point pt, double length)
        {
            if (origin == pt)
            {
                return pt;
            }

            var distOriginToPt = (pt - origin).Length;
            var distOriginToNewPoint = distOriginToPt + length;

            var vecOriginToPt = pt - origin;

            var prop = distOriginToNewPoint / distOriginToPt;
            var vecOriginNewPoint = vecOriginToPt * prop;

            Debug.Assert(!(double.IsNaN(vecOriginNewPoint.X) || double.IsNaN(vecOriginNewPoint.Y)));
            return origin + vecOriginNewPoint;
        }

        public static bool PointInCircle(Point center, double radius, Point pt)
        {
            var disp = center - pt;
            return disp.Length <= radius;
        }


        public static bool PointInPolygon(List<Point> points, Point pt)
        {
            return PointInPolygon(points, pt.X, pt.Y);
        }

        // Return True if the point is in the polygon.
        public static bool PointInPolygon(List<Point> Points, double X, double Y)
        {
            // Get the angle between the point and the
            // first and last vertices.
            var max_point = Points.Count - 1;
            var total_angle = GetAngle(
                                       Points[max_point].X, Points[max_point].Y,
                                       X, Y,
                                       Points[0].X, Points[0].Y);

            // Add the angles from the point
            // to each other pair of vertices.
            for (var i = 0; i < max_point; i++)
            {
                total_angle += GetAngle(
                                        Points[i].X, Points[i].Y,
                                        X, Y,
                                        Points[i + 1].X, Points[i + 1].Y);
            }

            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            return Math.Abs(total_angle) > 0.000001;
        }

        public static bool PointOnLineSegment(Point pt1, Point pt2, Point pt)
        {
            if (pt.X - Math.Max(pt1.X, pt2.X) > Epsilon ||
                Math.Min(pt1.X, pt2.X) - pt.X > Epsilon ||
                pt.Y - Math.Max(pt1.Y, pt2.Y) > Epsilon ||
                Math.Min(pt1.Y, pt2.Y) - pt.Y > Epsilon)
            {
                return false;
            }

            if (Math.Abs(pt2.X - pt1.X) < Epsilon)
            {
                return Math.Abs(pt1.X - pt.X) < Epsilon || Math.Abs(pt2.X - pt.X) < Epsilon;
            }

            if (Math.Abs(pt2.Y - pt1.Y) < Epsilon)
            {
                return Math.Abs(pt1.Y - pt.Y) < Epsilon || Math.Abs(pt2.Y - pt.Y) < Epsilon;
            }

            var x = pt1.X + (pt.Y - pt1.Y) * (pt2.X - pt1.X) / (pt2.Y - pt1.Y);
            var y = pt1.Y + (pt.X - pt1.X) * (pt2.Y - pt1.Y) / (pt2.X - pt1.X);

            return Math.Abs(pt.X - x) < Epsilon || Math.Abs(pt.Y - y) < Epsilon;
        }

        // Return the polygon's area in "square units."
        public static double PolygonArea(List<Point> points)
        {
            // Return the absolute value of the signed area.
            // The signed area is negative if the polygon is
            // oriented clockwise.
            return Math.Abs(SignedPolygonArea(points));
        }

        // Return True if the polygon is convex.
        public static bool PolygonIsConvex(Point[] points)
        {
            // For each set of three adjacent points A, B, C,
            // find the cross product AB · BC. If the sign of
            // all the cross products is the same, the angles
            // are all positive or negative (depending on the
            // order in which we visit them) so the polygon
            // is convex.
            var got_negative = false;
            var got_positive = false;
            var num_points = points.Length;
            int B, C;
            for (var A = 0; A < num_points; A++)
            {
                B = (A + 1) % num_points;
                C = (B + 1) % num_points;

                var cross_product =
                        CrossProductLength(
                                           points[A].X, points[A].Y,
                                           points[B].X, points[B].Y,
                                           points[C].X, points[C].Y);
                if (cross_product < 0)
                {
                    got_negative = true;
                }
                else if (cross_product > 0)
                {
                    got_positive = true;
                }

                if (got_negative && got_positive)
                {
                    return false;
                }
            }

            // If we got this far, the polygon is convex.
            return true;
        }

        // Return true if the indicated circle encloses all of the points.
        private static bool CircleEnclosesPoints(Point center,
                                                 double radius2, List<Point> points, int skip1, int skip2, int skip3)
        {
            for (var i = 0; i < points.Count; i++)
            {
                if (i != skip1 && i != skip2 && i != skip3)
                {
                    var point = points[i];
                    var dx = center.X - point.X;
                    var dy = center.Y - point.Y;
                    var test_radius2 = dx * dx + dy * dy;
                    if (test_radius2 > radius2)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Return the dot product AB · BC.
        // Note that AB · BC = |AB| * |BC| * Cos(theta).
        private static double DotProduct(double Ax, double Ay,
                                         double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            var BAx = Ax - Bx;
            var BAy = Ay - By;
            var BCx = Cx - Bx;
            var BCy = Cy - By;

            // Calculate the dot product.
            return BAx * BCx + BAy * BCy;
        }

        // Find a circle through the three points.
        private static void FindCircle(Point a, Point b, Point c, out Point center, out double radius2)
        {
            // Get the perpendicular bisector of (x1, y1) and (x2, y2).
            var x1 = (b.X + a.X) / 2;
            var y1 = (b.Y + a.Y) / 2;
            var dy1 = b.X - a.X;
            var dx1 = -(b.Y - a.Y);

            // Get the perpendicular bisector of (x2, y2) and (x3, y3).
            var x2 = (c.X + b.X) / 2;
            var y2 = (c.Y + b.Y) / 2;
            var dy2 = c.X - b.X;
            var dx2 = -(c.Y - b.Y);

            // See where the lines intersect.
            FindIntersection(
                             new Point(x1, y1),
                             new Point(x1 + dx1, y1 + dy1),
                             new Point(x2, y2),
                             new Point(x2 + dx2, y2 + dy2),
                             out var lines_intersect,
                             out var segments_intersect,
                             out var intersection,
                             out var close_p1,
                             out var close_p2);

            center = intersection;
            var dx = center.X - a.X;
            var dy = center.Y - a.Y;
            radius2 = dx * dx + dy * dy;
        }


        // Find the point of intersection between
        // the lines p1 --> p2 and p3 --> p4.
        private static void FindIntersection(Point p1, Point p2, Point p3, Point p4,
                                             out bool lines_intersect, out bool segments_intersect,
                                             out Point intersection, out Point close_p1, out Point close_p2)
        {
            // Get the segments' parameters.
            var dx12 = p2.X - p1.X;
            var dy12 = p2.Y - p1.Y;
            var dx34 = p4.X - p3.X;
            var dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            var denominator = dy12 * dx34 - dx12 * dy34;

            double t1;
            try
            {
                t1 = ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34) / denominator;
            }
            catch
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Point(double.NaN, double.NaN);
                close_p1 = new Point(double.NaN, double.NaN);
                close_p2 = new Point(double.NaN, double.NaN);
                return;
            }

            lines_intersect = true;

            var t2 = ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12) / -denominator;

            // Find the point of intersection.
            intersection = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect = t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1;

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Point(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }

        // Return the polygon's area in "square units."
        // The value will be negative if the polygon is
        // oriented clockwise.
        private static double SignedPolygonArea(List<Point> points)
        {
            // Add the first point to the end.
            var num_points = points.Count;
            var pts = new Point[num_points + 1];
            points.CopyTo(pts, 0);
            pts[num_points] = points[0];

            // Get the areas.
            double area = 0;
            for (var i = 0; i < num_points; i++)
            {
                area +=
                        (pts[i + 1].X - pts[i].X) *
                        (pts[i + 1].Y + pts[i].Y) / 2;
            }

            // Return the result.
            return area;
        }
    }
}