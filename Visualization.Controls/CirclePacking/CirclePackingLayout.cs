using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using Visualization.Controls.Interfaces;
using Visualization.Controls.Utility;

namespace Visualization.Controls.CirclePacking
{
    /*
    Algorithm:
    Implementation based on the paper:  Visualization of large Hierarchical Data by Circle Packing
    
    Basic idea
    1. On each level we start with arranging three circles. This gives the original front chain.
       The front chain is basically a convex polygon to keep the circles together.
    2. Find the circle on the front chain closest to the origin (m)
    3. Try set the next circle tangent between the circles m, n = m+1

    There are two solutions to finding a tangend circle with two others.
    (Unless the circles are too far apart). I chose the circle with center outside the polygon.
    See SelectCircle.

    4. If there is no collision place the circle on the front chain between m and n.
    5. If the circle overlaps with another circle on the front chain at position j:
        a. If j is before m: j m n
           Remove interval (j,n) from the front chain (exclusive)
           Try setting the circle again but now tangent between m=j and n
        b. If j is after n: m n j
           Remove interval (m,j) from the front chain (exclusive)
           Try setting the circle again but now tangent between m and n=j

    Extensions:
    
    Circles are drawn from the leaf nodes to the root. I do this because I want the
    leaf node area metric comparable between all leaf nodes.
    Once a level is finished I calculate a circle around all its children.   

    I found that it is very sensitive to the order of traversing the front chain.
    How the front chain is initialized and how to proceed in case of inconclusive situations.
    I initialize the front chain clockwise. If the distance j..m is the same as n..j I also chose the
    clockwise direction as default.

    Note that if we just fixed a circle overlapping from the front chain we may get 
    directly the next overlapping circle. That is ok.
    See Examples\why_we_end_up_with_a_collision_again.html
    */
    internal sealed class CirclePackingLayout
    {
        /// When one layer is finished the output files are deleted.
        public bool DebugEnabled { get; } = true;

        public void Layout(IHierarchicalData root, double actualWidth, double actualHeight)
        {
            // actualWidth and actualHeight are not used. We render the circles independent of the sizes and scale them later.

            // Counters over all steps
            DebugHelper.ResetDbgCounter();

            // Cleanup previous layouting
            root.TraverseTopDown(x => x.Layout = null);
            Layout(root);

            // Center root node to screen. Move to (0,0)
            var rootLayout = GetLayout(root);
            MoveCircles(root, -(Vector) rootLayout.Center);

            ApplyPendingMovements(root);
        }


        /// <summary>
        /// For performance optimization we did not apply movements to children until
        /// all positions are fixed.
        /// </summary>
        private void ApplyPendingMovements(IHierarchicalData data)
        {
            var layout = GetLayout(data);
            var pending = layout.PendingChildrenMovement;

            foreach (var child in data.Children)
            {
                var childLayout = GetLayout(child);
                childLayout.Move(pending);
                ApplyPendingMovements(child);
            }
        }

        private double AreaToRadius(double weight)
        {
            return Math.Sqrt(weight / (2 * Math.PI));
        }

        private CircularLayoutInfo CalculateEnclosingCircle(IHierarchicalData root, FrontChain frontChain)
        {
            // Get extreme points by extending the vector from origin to center by the radius
            var layouts = frontChain.ToList();
            var points = layouts.Select(x => MathHelper.MovePointAlongLine(new Point(), x.Center, x.Radius)).ToList();

            var layout = GetLayout(root);
            Debug.Assert(layout.Center == new Point()); // Since we process bottom up this node was not considered yet.

            double radius;
            Point center;

            if (frontChain.Head.Next == frontChain.Head) // Single element
            {
                center = frontChain.Head.Value.Center;
                radius = frontChain.Head.Value.Radius;
            }
            else
            {
                // 2 and 3 points seem to be handled correctly.
                MathHelper.FindMinimalBoundingCircle(points, out center, out radius);
            }

            layout.Center = center;
            Debug.Assert(Math.Abs(radius) > 0.00001);
            layout.Radius = radius * 1.03; // Just a little larger 3%. 
            return layout;

            // Note that this is not exact. It is a difficult problem.
            // We may have a little overlap. The larger the increment in radius
            // the smaller this problem gets.
        }

        private double DistanceToOrigin(Node node)
        {
            return ((Vector) node.Value.Center).Length;
        }

        private Node FindOverlappingCircleOnFrontChain(FrontChain frontChain, CircularLayoutInfo tmpCircle)
        {
            return frontChain.Find(node => IsOverlapping(node.Value, tmpCircle));
        }

        /// <summary>
        /// Find the circle tangent with the two others.
        /// </summary>
        private Tuple<CircularLayoutInfo, CircularLayoutInfo> FindTangentCircle(CircularLayoutInfo circle1, CircularLayoutInfo circle2, double radiusOfTangentCircle)
        {
            var solutions = MathHelper.FindCircleCircleIntersections(circle1.Center, circle1.Radius + radiusOfTangentCircle,
                                                                     circle2.Center, circle2.Radius + radiusOfTangentCircle, out var solution1
                                                                     , out var solution2);

            Debug.Assert(solutions >= 1);

            return new Tuple<CircularLayoutInfo, CircularLayoutInfo>(
                                                                     new CircularLayoutInfo
                                                                             { Center = solution1, Radius = radiusOfTangentCircle },
                                                                     new CircularLayoutInfo
                                                                     {
                                                                             Center = solution2,
                                                                             Radius = radiusOfTangentCircle
                                                                     });
        }

        private CircularLayoutInfo GetLayout(IHierarchicalData item)
        {
            if (item.Layout == null)
            {
                var layout = new CircularLayoutInfo();
                item.Layout = layout;
            }

            return item.Layout as CircularLayoutInfo;
        }

        /// <summary>
        /// Layouting of items starts with arranging the first three items around the center 0,0
        /// Note that we do not set the radius. The radius was determined for the leaf nodes before.
        /// The non leaf nodes don't reflect the sum of the children (area metric!).
        /// </summary>
        private FrontChain InitializeFrontChain(List<IHierarchicalData> children)
        {
            var frontChain = new FrontChain();

            var left = new CircularLayoutInfo();
            var right = new CircularLayoutInfo();
            var top = new CircularLayoutInfo();

            IHierarchicalData child;
            if (children.Count >= 1)
            {
                // Left
                child = children[0];
                left = GetLayout(child);

                // If child has children its origin is not (0,0) any more. So move this node to origin first.               
                var displacement = -(Vector) left.Center + new Vector(-left.Radius, 0);
                left.Move(displacement);
            }

            if (children.Count >= 2)
            {
                // Right
                child = children[1];
                right = GetLayout(child);

                // If child has children its origin is not (0,0) any more. So move this node to origin first.          
                var displacement = -(Vector) right.Center + new Vector(right.Radius, 0);
                right.Move(displacement);
            }

            if (children.Count >= 3)
            {
                // Top
                child = children[2];
                top = GetLayout(child);

                var solutions = MathHelper.FindCircleCircleIntersections(
                                                                         left.Center, left.Radius + top.Radius,
                                                                         right.Center, right.Radius + top.Radius,
                                                                         out var solution1, out var solution2);

                // If not maybe you did not remove zero weights?
                Debug.Assert(solutions == 2);
                var solution = solution1.Y > solution2.Y ? solution1 : solution2;

                var displacement = -(Vector) top.Center + (Vector) solution;
                top.Move(displacement);
            }

            // This order makes a huge difference. 
            // left -> right -> top did not work and leads to overlapping circles!
            frontChain.Add(left);
            frontChain.Add(top);
            frontChain.Add(right);
            return frontChain;
        }

        /// <summary>
        /// At the beginning any leaf node is simply centered at (0,0)
        /// </summary>
        private void InitLayoutForLeafNode(IHierarchicalData data)
        {
            // Not intersted in non leaf nodes, so we can ask for area metric.
            Debug.Assert(data.AreaMetric > 0);
            var layout = GetLayout(data);
            layout.Radius = AreaToRadius(data.AreaMetric);
            layout.Center = new Point(0, 0);
            layout.PendingChildrenMovement = new Vector(0, 0);
        }

        private bool IsOverlapping(CircularLayoutInfo layout1, CircularLayoutInfo layout2)
        {
            if (layout1 == layout2)
            {
                return true;
            }

            var intersections = MathHelper.FindCircleCircleIntersections(layout1.Center, layout1.Radius,
                                                                         layout2.Center, layout2.Radius, out var intersection1, out var intersection2);

            // Two solutions or one circle inside the other.
            return intersections == 2 || intersections == -1;
        }

        private bool IsPointValid(Point pt)
        {
            return !double.IsNaN(pt.X) && !double.IsNaN(pt.Y);
        }

        /// <summary>
        /// m, n, j and i refer to the paper.
        /// </summary>
        private void Layout(IHierarchicalData data)
        {
            if (DebugEnabled)
            {
                DebugHelper.DeleteDebugOutput();
            }

            if (data.IsLeafNode)
            {
                // That's the most simple layout. A leaf node is centered around (0,0)
                InitLayoutForLeafNode(data);
                return;
            }

            foreach (var child in data.Children)
            {
                // First layout all levels below.
                Layout(child);
            }

            // The radii of all children are known here.
            // We start again at origin (0,0) and move the circles around using the front chain. 
            // To initialize the front chain we use the first three circles.
            // Note that we have to sort by the radius and not(!) by the AreaMetric because
            // the area metric is used only for the leaf nodes. This was a subtle bug.
            // Non leaf nodes have a radius depending on the circle enclosing all children.
            var children = data.Children.OrderByDescending(x => GetLayout(x).Radius).ToList();

            var frontChain = InitializeFrontChain(children);

            var adjustingFrontChain = false;
            Node mNode = null;
            Node nNode = null;

            // Arrange the remaining circles            
            var childIdx = 3;
            while (childIdx < children.Count)
            {
                DebugHelper.IncDbgCounter();

                var iItem = children.ElementAt(childIdx);
                var iLayout = GetLayout(iItem);

                if (!adjustingFrontChain)
                {
                    // Find circle with shortest distance to origin
                    mNode = frontChain.FindMinValue(DistanceToOrigin);
                    nNode = mNode.Next;
                }
                else
                {
                    // Use the specified indicies  from the last front chain update.
                }

                // Calc solutions

                // Test front chain Both violate-> deltete and try to fix whole.

                var solutions = FindTangentCircle(mNode.Value, nNode.Value, iLayout.Radius);

                
                var tmpCircle2 = SelectCircleExperimential(frontChain, mNode.Value, nNode.Value, solutions.Item1, solutions.Item2);

                // TODO remove old implementation
                var tmpCircle = SelectCircle(frontChain, solutions.Item1, solutions.Item2);

                Debug.Assert(tmpCircle == tmpCircle2);
                Debug.Assert(tmpCircle != null);

                // Find overlappings with circles in the front chain.Does one intersect with the just calculated circle?
                // Exclude mIndex and nIndex Overlapping with itself is detected due to rounding erros.
                var jNode = FindOverlappingCircleOnFrontChain(frontChain, tmpCircle);
                if (jNode != null && adjustingFrontChain)
                {
                    // This is a valid case: 
                    // See Examples\Why_we_end_up_with_a_collision_again.html
                }

                adjustingFrontChain = false;
                if (jNode != null && frontChain.IsAfter(jNode, mNode))
                {
                    // This is also the default case if distance j...m and n...j is the same.
                    // IsAfter is true in both cases then.
                    // This way always the m node is removed. m is the node closest to the origin. This seems to be the
                    // best heuristic if we want to grow outwards. Otherwise it happened that the front
                    // chain is reduced to less than three circles.

                    adjustingFrontChain = true;

                    // j is before m
                    // Remove (j, n) from front chain
                    // i should be placed between j and n in the next loop.

                    // Update front chain
                    frontChain.Delete(jNode, nNode);
                    Debug.Assert(frontChain.Count() >= 3);

                    // Adjust the indicies
                    mNode = jNode;

                    // nNode stays the same
                }
                else if (jNode != null && frontChain.IsAfter(nNode, jNode))
                {
                    adjustingFrontChain = true;

                    // j is after n
                    // Remove (m,j) from the front chain
                    // i should be placed between m and j in the next loop.

                    //DebugHelper.BreakOn(13025);

                    // Update front chain
                    frontChain.Delete(mNode, jNode);
                    Debug.Assert(frontChain.Count() >= 3);

                    // mNode stays the same
                    nNode = jNode;
                }

                else
                {
                    Debug.Assert(jNode == null);

                    // No overlap with any other circle
                    var disp = tmpCircle.Center - iLayout.Center;
                    MoveCircles(iItem, disp);
                    frontChain.InsertAfter(mNode, tmpCircle);
                    childIdx++;

                    if (DebugEnabled)
                    {
                        // Template for debug output
                        DebugHelper.WriteDebugOutput(frontChain, $"dbgOut_m_{frontChain.IndexOf(mNode)}_n_${frontChain.IndexOf(nNode)}", solutions.Item1, solutions.Item2);
                        DebugHelper.WriteDebugOutput(frontChain, $"dbgOut_m_{frontChain.IndexOf(mNode)}_n_{frontChain.IndexOf(nNode)}", tmpCircle);
                    }
                }
            }

            var enclosing = CalculateEnclosingCircle(data, frontChain);
        }

        /// <summary>
        /// Usually we have two solutions for tangent circles. We have to decide which to use.
        /// I assume the polygon goes always in direction m to n.
        /// We chose the solution that is outside the polygon. I want to grow outside.
        /// 1. Calculate segment m-n
        /// 2. Calculate normal vector to this form
        /// 3. Chose m as vector to the segment
        /// 4. Use vector normal form to check if solution1 or solution2 is outside or inside.
        /// Both scalars may be positive or negative. So I chose the larger on. Not quite sure if this is ok.
        /// </summary>
        private CircularLayoutInfo SelectCircleExperimential(CircularLayoutInfo m, CircularLayoutInfo n, CircularLayoutInfo circle1, CircularLayoutInfo circle2)
        {
            Debug.Assert(IsPointValid(circle1.Center) || IsPointValid(circle2.Center));

            // If only one point is valid, take it      
            if (IsPointValid(circle1.Center) && !IsPointValid(circle2.Center))
            {
                return circle1;
            }

            if (!IsPointValid(circle1.Center) && IsPointValid(circle2.Center))
            {
                return circle2;
            }

            // We have two solutions. Which point to chose?
          
            var segment_m_n = m.Center - n.Center;
            var anyNormal = new Vector(-segment_m_n.Y, segment_m_n.X)
                - new Vector(segment_m_n.Y, -segment_m_n.X);
            var value1 = Vector.Multiply(circle1.Center - m.Center, (Vector)anyNormal);
            var value2 = Vector.Multiply(circle2.Center - m.Center, (Vector)anyNormal);

            if (value1 > 0 && value2 < 0)
                return circle2;

            if (value2 > 0 && value1 < 0)
                return circle1;

            return value1 > value2 ? circle2 : circle1;
        }

        /// <summary>
        /// Moves the circle and records that this movement is pending for the children.
        /// </summary>
        private void MoveCircles(IHierarchicalData data, Vector offset)
        {
            var layout = GetLayout(data);
            layout.Move(offset);
        }

        /// <summary>
        /// Usually we have two solutions for tangent circles. We have to decide which to use.
        /// Theoretically that is the one that is outside the front chain polygon.
        /// However there are many edge cases. So I do not have an exact mathematical solution to this problem.
        /// Therefore I use different heuristics.
        /// </summary>
        private CircularLayoutInfo SelectCircle(FrontChain frontChain, CircularLayoutInfo circle1, CircularLayoutInfo circle2)
        {
            var poly = frontChain.ToList().Select(x => x.Center).ToList();

            Debug.Assert(IsPointValid(circle1.Center) || IsPointValid(circle2.Center));

            // ----------------------------------------------------------------------------
            // If exactly one of the two points is valid that is the solution.
            // ----------------------------------------------------------------------------

            if (IsPointValid(circle1.Center) && !IsPointValid(circle2.Center))
            {
                return circle1;
            }

            if (!IsPointValid(circle1.Center) && IsPointValid(circle2.Center))
            {
                return circle2;
            }

            // We have two solutions. Which point to chose?

            // ----------------------------------------------------------------------------
            // If one center is inside and one outside the polygon take the outside.
            // ----------------------------------------------------------------------------

            var center1Inside = MathHelper.PointInPolygon(poly, circle1.Center.X, circle1.Center.Y);
            var center2Inside = MathHelper.PointInPolygon(poly, circle2.Center.X, circle2.Center.Y);

            if (center1Inside && !center2Inside)
            {
                return circle2;
            }

            if (!center1Inside && center2Inside)
            {
                return circle1;
            }

            // Both centers outside: Examples\Both_centers_outside_polygon.html.
            // Note that the purple circle (center) may also be inside the polygon if it was a little bit smaller.
            // Debug.Assert(center2Inside && center2Inside);

            // Both centers inside: Examples\Both_centers_inside_polygon.html
            // Happens when centers are on the polygon edges.
            // Debug.Assert(!center2Inside && !center2Inside);

            // ----------------------------------------------------------------------------
            // If one circle is crossed by an polygon edge and the other is not, 
            // take the other.
            // ----------------------------------------------------------------------------
            var circle1HitByEdge = false;
            var circle2HitByEdge = false;
            var iter = frontChain.Head;
            while (iter != null)
            {
                var first = iter.Value;
                var second = iter.Next.Value;

                Point intersect1;
                Point intersect2;

                // Note we consider a line here, not a line segment
                var solutions = MathHelper.FindLineCircleIntersections(circle1.Center.X,
                                                                       circle1.Center.Y, circle1.Radius, first.Center, second.Center, out intersect1, out intersect2);

                if (solutions > 0)
                {
                    circle1HitByEdge |= MathHelper.PointOnLineSegment(first.Center, second.Center, intersect1);
                }

                if (solutions > 1)
                {
                    circle1HitByEdge |= MathHelper.PointOnLineSegment(first.Center, second.Center, intersect2);
                }

                solutions = MathHelper.FindLineCircleIntersections(circle2.Center.X,
                                                                   circle2.Center.Y, circle2.Radius, first.Center, second.Center, out intersect1, out intersect2);

                if (solutions > 0)
                {
                    circle2HitByEdge |= MathHelper.PointOnLineSegment(first.Center, second.Center, intersect1);
                }

                if (solutions > 1)
                {
                    circle2HitByEdge |= MathHelper.PointOnLineSegment(first.Center, second.Center, intersect2);
                }

                iter = iter.Next;
                if (iter == frontChain.Head)
                {
                    // Ensure that the segment from tail to head is also processed.
                    iter = null;
                }
            }

            if (circle1HitByEdge && !circle2HitByEdge)
            {
                return circle2;
            }

            if (!circle1HitByEdge && circle2HitByEdge)
            {
                return circle1;
            }

            if (DebugEnabled)
            {
                DebugHelper.WriteDebugOutput(frontChain, "not_sure_which_circle", circle1, circle2);
            }

            // Still inconclusive which solution to take.
            // I choose the one with the largst distance from the origin.
            // Background is following example: Inconclusive_solution.html
            // I need to get rid of the inner (green) circle If I want to grow outwards.

            // In my understanding this inner node is always m. We chose it because it had the smallest distance to the origin.
            // My hope is that the selected solution leads to removal of m. Cannot prove it, but I think so.
            return SelectCircleWithLargerDistanceFromOrigin(circle1, circle2);
        }

        private CircularLayoutInfo SelectCircleWithLargerDistanceFromOrigin(CircularLayoutInfo circle1, CircularLayoutInfo circle2)
        {
            Debug.Assert(IsPointValid(circle1.Center) && IsPointValid(circle2.Center));
            Debug.Assert(circle1.Radius.Equals(circle2.Radius));

            if (((Vector) circle1.Center).Length > ((Vector) circle2.Center).Length)
            {
                return circle1;
            }

            return circle2;
        }
    }
}