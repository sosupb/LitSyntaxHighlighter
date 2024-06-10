using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace LitSyntaxHighlighter
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "LitClassifier" classification type.
    /// </summary>
    internal class LitClassifier : IClassifier
    {
        private readonly IDictionary<string, IClassificationType> classificationTypes;
        readonly private IClassifier classifier;

        private static Dictionary<string, Regex> regexes = new Dictionary<string, Regex>()
        {
            { "attributeValue", new Regex("\"[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*\"", RegexOptions.IgnoreCase) },
            { "openTag", new Regex("<([A-Za-z0-9]+(-[A-Za-z0-9]+)*)(\\/)?>?", RegexOptions.IgnoreCase) },
            { "closingTag", new Regex("</([A-Za-z0-9]+(-[A-Za-z0-9]+)*)>", RegexOptions.IgnoreCase) },
            { "property", new Regex("\\.[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "boolean", new Regex("\\?[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "eventListener", new Regex("@[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "attributeName", new Regex("[A-Za-z0-9]+=", RegexOptions.IgnoreCase) },
            { "selfClosingTag", new Regex("(\\/)?>", RegexOptions.IgnoreCase) },
            { "text", new Regex("^[^\"]*$", RegexOptions.IgnoreCase) }

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
                { FormatNames.AttributeDelimiter, registry.GetClassificationType(FormatNames.AttributeDelimiter) },
                { FormatNames.EventName, registry.GetClassificationType(FormatNames.EventName) },
                { FormatNames.Quote, registry.GetClassificationType(FormatNames.Quote) },
                { FormatNames.AttributeValue, registry.GetClassificationType(FormatNames.AttributeValue) },
                { FormatNames.Text, registry.GetClassificationType(FormatNames.Text) }
            };

            this.classifier = classifier;
        }

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
            List<ClassificationSpan> result = new List<ClassificationSpan>();

            foreach (ClassificationSpan cs in classifier.GetClassificationSpans(span))
            {
                string csClass = cs.ClassificationType.Classification.ToLower();
                var literal = cs.Span.GetText().Trim();
                if (csClass == "string" && !(literal.StartsWith("\"") && literal.EndsWith("\"")))
                {
                    var subSpan = new SnapshotSpan(cs.Span.Start, cs.Span.End);
                    if (subSpan.Length > 0)
                    {
                        var classification = ScanLiteral(subSpan);

                        if (classification.Count > 0)
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


        private List<ClassificationSpan> ScanLiteral(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();
            string literal = span.GetText();

            foreach (var keyValuePair in regexes)
            {
                AddMatchingHighlighting(keyValuePair, literal, span, result);
            }

            return result;
        }

        private bool IsNameChar(char c)
        {
            return c == '_' || c == '-' || char.IsLetterOrDigit(c);
        }

        private void AddMatchingHighlighting(KeyValuePair<string, Regex> regex, string text, SnapshotSpan span, IList<ClassificationSpan> list)
        {
            foreach (Match match in regex.Value.Matches(text))
            {
                var str = new SnapshotSpan(span.Snapshot, span.Start.Position + match.Index, match.Length);

                if (list.Any(s => s.Span.IntersectsWith(str) && s.ClassificationType == classificationTypes[FormatNames.AttributeValue]))
                {
                    return;
                }

                switch (regex.Key)
                {
                    case "openTag":
                        {
                            ProcessOpenTag(str, list);
                            break;
                        }
                    case "closingTag":
                        {
                            ProcessClosingTag(str, list); 
                            break;
                        }
                    case "property":
                    case "boolean":
                    case "eventListener":
                    case "attributeName":
                        {
                            ProcessAttributeName(str, list);
                            break;
                        }
                    case "attributeValue":
                        {
                            ProcessAttributeValue(str, list);
                            break;
                        }
                    case "selfClosingTag":
                        {
                            ProcessSelfClosingTag(str, list);
                            break;
                        }
                    case "text":
                        {
                            ProcessText(str, list);
                            break;
                        }
                }
            }
        }

        #region Tag processing

        private void ProcessOpenTag(SnapshotSpan span, IList<ClassificationSpan> list)
        {
            string literal = span.GetText();
            int currentCharIndex = 0;
            int? nameStartIndex = null;

            while ( currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                if(c == '<') // found beginning of tag
                {
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                }
                else if(IsNameChar(c) && !nameStartIndex.HasValue) // start of element name
                {
                    nameStartIndex = currentCharIndex;
                } 
                else if (c == '>' || c == '/') // found end of tag
                {
                    if (nameStartIndex.HasValue)
                    {
                        int length = currentCharIndex - nameStartIndex.Value;
                        list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.Element]));
                        nameStartIndex = null;
                    }
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                }
                else if(char.IsWhiteSpace(c) && nameStartIndex.HasValue) // end of element name without closing 
                {
                    int length = currentCharIndex - nameStartIndex.Value;
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.Element]));
                    nameStartIndex = null;
                }

                currentCharIndex += 1;
            }

            // cleanup end of element name if there is one
            if (nameStartIndex != null)
            {
                int length = currentCharIndex - nameStartIndex.Value;
                list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.Element]));
            }
        }

        private void ProcessClosingTag(SnapshotSpan span, IList<ClassificationSpan> list)
        {
            string literal = span.GetText();
            int currentCharIndex = 0;
            int? nameStartIndex = null;

            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                if (c == '<' || c == '/') // found beginning of tag
                {
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                }
                else if (IsNameChar(c) && !nameStartIndex.HasValue) // start of element name
                {
                    nameStartIndex = currentCharIndex;
                }
                else if (c == '>') // found end of tag
                {
                    if (nameStartIndex.HasValue)
                    {
                        int length = currentCharIndex - nameStartIndex.Value;
                        list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.Element]));
                        nameStartIndex = null;
                    }
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                }
                else if (char.IsWhiteSpace(c) && nameStartIndex.HasValue) // end of element name without closing 
                {
                    int length = currentCharIndex - nameStartIndex.Value;
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.Element]));
                    nameStartIndex = null;
                }

                currentCharIndex += 1;
            }
        }

        private void ProcessAttributeName(SnapshotSpan span, IList<ClassificationSpan> list)
        {
            string literal = span.GetText();
            int currentCharIndex = 0;
            int? nameStartIndex = null;

            bool isEventAttribute = false;
            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                if (c == '.' || c == '?' || c == '@') // found start of attribute
                {
                    if(c == '@')
                    {
                        isEventAttribute = true;
                    }
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.AttributeDelimiter]));
                }
                else if (IsNameChar(c) && !nameStartIndex.HasValue) // start of attribute name
                {
                    nameStartIndex = currentCharIndex;
                }
                else if (c == '=') // found end of attribute name
                {
                    if (nameStartIndex.HasValue)
                    {
                        int length = currentCharIndex - nameStartIndex.Value;
                        list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[isEventAttribute ? FormatNames.EventName : FormatNames.AttributeName]));
                        nameStartIndex = null;
                    }
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                }
                currentCharIndex += 1;
            }
        }

        private void ProcessAttributeValue(SnapshotSpan span, IList<ClassificationSpan> list)
        {
            string literal = span.GetText();
            int currentCharIndex = 0;
            int? nameStartIndex = null;

            if(list.Any(s => s.Span.IntersectsWith(span)))
            {
                return;
            }

            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                if (c == '\"') // found start or end of attribute value
                {
                    if(nameStartIndex.HasValue)
                    {
                        int length = currentCharIndex - nameStartIndex.Value;
                        list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.AttributeValue]));
                        nameStartIndex = null;
                    }
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Quote]));
                }
                else if (IsNameChar(c) && !nameStartIndex.HasValue) // start of attribute name
                {
                    nameStartIndex = currentCharIndex;
                }
                currentCharIndex += 1;
            }
        }

        private void ProcessSelfClosingTag(SnapshotSpan span, IList<ClassificationSpan> list)
        {
            string literal = span.GetText();
            int currentCharIndex = 0;

            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                if (c == '>' || c == '/') // found end open tag or self closing tag
                {
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                }

                currentCharIndex += 1;
            }
        }

        private void ProcessText(SnapshotSpan span, IList<ClassificationSpan> list)
        {
            string literal = span.GetText();
            int currentCharIndex = 0;
            int? nameStartIndex = null;

            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                if (!nameStartIndex.HasValue && (c != '>' || c != '<')) // skip tag start and end
                {
                    nameStartIndex = currentCharIndex;
                }
                else if (nameStartIndex.HasValue && c == '<')
                {
                    int length = currentCharIndex - nameStartIndex.Value;
                    list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.Text]));
                    nameStartIndex = null;
                }
                currentCharIndex += 1;
            }

            // final check if we are still tracking a word
            if(nameStartIndex.HasValue)
            {
                int length = currentCharIndex - nameStartIndex.Value;
                list.Add(new ClassificationSpan(new SnapshotSpan(span.Start + nameStartIndex.Value, length), classificationTypes[FormatNames.Text]));
                nameStartIndex = null;
            }
        }

        #endregion
    }
}
