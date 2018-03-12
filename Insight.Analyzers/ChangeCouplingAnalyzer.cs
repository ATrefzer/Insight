using System;
using System.Collections.Generic;
using System.Linq;

using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;

namespace Insight.Analyzers
{
    /// <summary>
    ///     Change coupling based on commits! Alternative would be logical work items.
    /// </summary>
    public sealed class ChangeCouplingAnalyzer
    {
        /// <summary>
        ///     Changesets with more modifications are ignored.
        /// </summary>
        private const int MaxChangesInChangeset = 300;

        // Reduce output
        private const int MinCoupling = 2;


        private readonly Dictionary<string, uint> _count = new Dictionary<string, uint>();


        private readonly Dictionary<string, Coupling> _couplings = new Dictionary<string, Coupling>();

        public List<Coupling> CalculateChangeCouplings(ChangeSetHistory history, IFilter filter)
        {
            _couplings.Clear();
            _count.Clear();

            foreach (var cs in history.ChangeSets)
            {
                if (cs.Items.Count > MaxChangesInChangeset)
                {
                    continue;
                }

                // Only accepted files
                var reducedItems =
                        cs.Items.Where(item => filter.IsAccepted(item.LocalPath)).Select(item => item.LocalPath).ToList();


                IncrementCommitCount(reducedItems);

                for (var i = 0; i < reducedItems.Count - 1; i++) // Keep one for the last pair
                {
                    // Make pairs of files.
                    for (var j = i + 1; j < reducedItems.Count; j++)
                    {
                        var path1 = reducedItems[i];
                        var path2 = reducedItems[j];

                        IncrementCoupling(path1, path2);
                    }
                }
            }

            CalculateDegree();

            return _couplings.Values
                             .Where(coupling => coupling.Couplings > MinCoupling)
                             .OrderByDescending(coupling => coupling.Degree).ToList();
        }

        /// <summary>
        ///     If the classifier returns string.EMPTY the according file is not used.
        /// </summary>
        public List<Coupling> CalculateChangeCouplings(ChangeSetHistory history, Func<string, string> classifier)
        {
            _couplings.Clear();
            _count.Clear();

            foreach (var cs in history.ChangeSets)
            {
                var classifications = ClassifyItems(cs, classifier);
                IncrementCommitCount(classifications);

                for (var i = 0; i < classifications.Count - 1; i++) // Keep one for the last pair
                {
                    // Make pairs of code classifiers.
                    for (var j = i + 1; j < classifications.Count; j++)
                    {
                        var class1 = classifications[i];
                        var class2 = classifications[j];

                        IncrementCoupling(class1, class2);
                    }
                }
            }

            CalculateDegree();

            return _couplings.Values
                             .Where(coupling => coupling.Couplings > MinCoupling)
                             .OrderByDescending(coupling => coupling.Degree).ToList();
        }

        private void CalculateDegree()
        {
            foreach (var coupling in _couplings.Values)
            {
                coupling.Degree = 100.0 * coupling.Couplings /
                                  (GetCount(coupling.Item1) + GetCount(coupling.Item2) - coupling.Couplings);

                coupling.Degree = Math.Round(coupling.Degree, 2);
            }
        }

        private List<string> ClassifyItems(ChangeSet cs, Func<string, string> classifier)
        {
            // Get classifiers for changeset
            var set = new HashSet<string>();
            foreach (var item in cs.Items)
            {
                var classification = classifier(item.LocalPath);
                if (!string.IsNullOrEmpty(classification))
                {
                    set.Add(classification);
                }
            }

            return set.ToList();
        }

        /// <summary>
        /// Returns number of commits on this item
        /// </summary>
        private uint GetCount(string artifact)
        {
            uint value;
            _count.TryGetValue(artifact, out value);
            return value;
        }

        /// <summary>
        ///     File or classification. The given keys are from one change set. We increment each file (or class)
        ///     that occurs in the changeset.
        /// </summary>
        private void IncrementCommitCount(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                _count.AddToValue(key, 1);
            }
        }

        /// <summary>
        ///     File or classification. The given items occur together in a changeset.
        /// </summary>
        private void IncrementCoupling(string item1, string item2)
        {
            var pairKey = OrderedPair.Key(item1, item2);

            Coupling coupling;
            if (_couplings.TryGetValue(pairKey, out coupling))
            {
                coupling.Couplings = coupling.Couplings + 1;
            }
            else
            {
                _couplings.Add(pairKey, new Coupling(item1, item2));
            }
        }
    }
}