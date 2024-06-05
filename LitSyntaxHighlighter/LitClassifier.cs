using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LitSyntaxHighlighter
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "LitClassifier" classification type.
    /// </summary>
    internal class LitClassifier : IClassifier
    {
        private IDictionary<string, IClassificationType> classificationTypes;

        private IClassifier classifier;

        private static Dictionary<string, Regex> regexes = new Dictionary<string, Regex>()
        {
            { "text", new Regex("[A-Za-z0-9]+", RegexOptions.IgnoreCase) },
            { "attribute", new Regex("[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "property", new Regex("\\.[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "boolean", new Regex("\\?[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "eventListener", new Regex("@[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "openTag", new Regex("<([A-Za-z0-9]+(-[A-Za-z0-9]+)+)>", RegexOptions.IgnoreCase) },
            { "closingTag", new Regex("</([A-Za-z0-9]+(-[A-Za-z0-9]+)+)>", RegexOptions.IgnoreCase) }

        };

        /// <summary>
        /// Initializes a new instance of the <see cref="LitClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal LitClassifier(IClassificationTypeRegistryService registry, IClassifier classifier)
        {
            this.classificationTypes = new Dictionary<string, IClassificationType>()
            {
                { FormatNames.Delimiter, registry.GetClassificationType(FormatNames.Delimiter) },
                { FormatNames.Element, registry.GetClassificationType(FormatNames.Element) },
                { FormatNames.AttributeName, registry.GetClassificationType(FormatNames.AttributeName) },
                { FormatNames.Quote, registry.GetClassificationType(FormatNames.Quote) },
                { FormatNames.AttributeValue, registry.GetClassificationType(FormatNames.AttributeValue) },
                { FormatNames.Text, registry.GetClassificationType(FormatNames.Text) }
            };

            this.classifier = classifier;
        }

        #region IClassifier

#pragma warning disable 67

        /// <summary>
        /// An event that occurs when the classification of a span of text has changed.
        /// </summary>
        /// <remarks>
        /// This event gets raised if a non-text change would affect the classification in some way,
        /// for example typing /* would cause the classification to change in C# without directly
        /// affecting the span.
        /// </remarks>
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
        /// </summary>
        /// <remarks>
        /// This method scans the given SnapshotSpan for potential matches for this classification.
        /// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
        /// </remarks>
        /// <param name="span">The span currently being classified.</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();
            var temp = span.Snapshot.TextBuffer.CurrentSnapshot;
            foreach(ClassificationSpan cs in classifier.GetClassificationSpans(span))
            { 
                string csClass = cs.ClassificationType.Classification.ToLower();
                if (csClass == "string") // Only apply our rules if we found a string literal
                {
                    if (cs.Span.Length > 2)
                    {
                        var sspan = new SnapshotSpan(cs.Span.Start.Add(1), cs.Span.End.Subtract(1)); // exclude quote

                        var classification = ScanLiteral(sspan);

                        if (classification != null)
                        {
                            result.AddRange(classification);
                            continue;
                        }
                    }
                }
                result.Add(cs);
            }

            return result;
        }

        #endregion

        private void AddMatchingHighlighting(Regex regex, string text, ITextSnapshotLine line, IList<ClassificationSpan> list, string type)
        {
            foreach (Match match in regex.Matches(text))
            {
                var str = new SnapshotSpan(line.Snapshot, line.Start.Position + match.Index, match.Length);

                if (list.Any(cs => cs.Span.IntersectsWith(str)))
                    continue;

                // list.Add(new ClassificationSpan(str, classificationType));
            }
        }

        private List<ClassificationSpan> ScanLiteral(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();
            ITextSnapshotLine line = span.Start.GetContainingLine();
            string literal = line.GetText();

            foreach (var keyValuePair in regexes)
            {
                AddMatchingHighlighting(keyValuePair.Value, literal, line, result, keyValuePair.Key);
            }

            return result;
        }
    }
}
