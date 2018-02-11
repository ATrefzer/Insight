using System;
using System.Collections.Generic;

namespace Visualization.Controls.CirclePackaging
{
    public class Node
    {
        public Node Next;
        public Node Previous;
        public CircularLayoutInfo Value;

        public Node(CircularLayoutInfo value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Front chain implementation for the circle packaging algorithm
    /// </summary>
    public sealed class FrontChain
    {
        public Node Head { get; set; }

        public Node Add(CircularLayoutInfo layout)
        {
            var newNode = new Node(layout);

            if (Head == null)
            {
                Head = newNode;
                newNode.Next = Head;
                newNode.Previous = Head;
            }
            else
            {
                // Add to the end (before head)
                var tail = Head.Previous;
                tail.Next = newNode;
                newNode.Previous = tail;

                newNode.Next = Head;
                Head.Previous = newNode;
            }

            return newNode;
        }

        public int Count()
        {
            var count = 0;
            var iter = Head;
            while (iter != null)
            {
                count++;

                iter = iter.Next;
                if (iter == Head)
                {
                    iter = null;
                }
            }

            return count;
        }

        public void Delete(Node node)
        {
            if (node == Head && Head.Next == Head)
            {
                // Delete last node
                Head = null;
                return;
            }

            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;

            if (node == Head)
            {
                Head = node.Next;
            }
        }

        public void Delete(Node fromExclusive, Node toExclusive)
        {
            // Check if head is to be deleted
            var current = fromExclusive.Next;
            while (current != toExclusive)
            {
                if (current == Head)
                {
                    Head = toExclusive; // Could also be toExclusive.
                    break;
                }

                current = current.Next;
            }

            fromExclusive.Next = toExclusive;
            toExclusive.Previous = fromExclusive;
        }

        public Node Find(Func<Node, bool> pred)
        {
            if (Head == null)
            {
                return null;
            }

            var currentNode = Head;

            do
            {
                if (pred(currentNode))
                {
                    return currentNode;
                }

                currentNode = currentNode.Next;
            } while (currentNode != Head);

            return null;
        }

        /// <summary>
        /// ((Vector)currentNode.Value.Center).Length;
        /// </summary>
        public Node FindMinValue(Func<Node, double> toDouble)
        {
            if (Head == null)
            {
                return null;
            }

            var currentNode = Head;
            var smallestValue = double.MaxValue;
            Node smallestNode = null;
            do
            {
                var currentValue = toDouble(currentNode);
                if (currentValue < smallestValue)
                {
                    smallestValue = currentValue;
                    smallestNode = currentNode;
                }

                currentNode = currentNode.Next;
            } while (currentNode != Head);

            return smallestNode;
        }

        /// <summary>
        ///  Expensive!
        /// </summary>
        public int IndexOf(Node node)
        {
            var index = -1;

            var i = 0;
            var iter = Head;
            while (iter != null) // If not found
            {
                if (iter == node)
                {
                    index = i;
                    break;
                }

                i++;
                iter = iter.Next;
                if (iter == Head)
                {
                    iter = null;
                }
            }

            return index;
        }

        public Node InsertAfter(Node node, CircularLayoutInfo layout)
        {
            var newNode = new Node(layout);

            // Fix new node
            newNode.Next = node.Next;
            newNode.Previous = node;

            // Fix successor
            var successor = node.Next;
            successor.Previous = newNode;

            // Fix node
            node.Next = newNode;

            return newNode;
        }

        /// <summary>
        /// Is the shortest distance to node from reference by going forward (after) or backward.
        /// </summary>
        public bool IsAfter(Node reference, Node node)
        {
            if (reference == node)
            {
                return false;
            }

            var forwardIter = reference.Next;
            var backwardIter = reference.Previous;

            var isAfter = false;
            while (forwardIter != reference || backwardIter != reference)
            {
                if (forwardIter == node)
                {
                    // If distance is same forward iter wins
                    isAfter = true;
                    break;
                }

                if (backwardIter == node)
                {
                    //isAfter = false;
                    break;
                }

                if (forwardIter != reference)
                {
                    forwardIter = forwardIter.Next;
                }

                if (backwardIter != reference)
                {
                    backwardIter = backwardIter.Previous;
                }
            }

            return isAfter;
        }

        /// <summary>
        /// Reference pattern for iteration.
        /// </summary>
        public List<CircularLayoutInfo> ToList()
        {
            var result = new List<CircularLayoutInfo>();
            var iter = Head;
            while (iter != null)
            {
                result.Add(iter.Value);

                iter = iter.Next;
                if (iter == Head)
                {
                    iter = null;
                }
            }

            return result;
        }
    }
}