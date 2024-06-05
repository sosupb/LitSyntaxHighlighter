using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LitSyntaxHighlighter
{
    /// <summary>
    /// Classifier that classifies all text as an instance of the "LitClassifier" classification type.
    /// </summary>
    internal class LitClassifier : IClassifier
    {
        IDictionary<string, IClassificationType> classificationTypes;

        IClassifier classifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="LitClassifier"/> class.
        /// </summary>
        /// <param name="registry">Classification registry.</param>
        internal LitClassifier(IClassificationTypeRegistryService registry, IClassifier classifier)
        {
            this.classificationTypes = new Dictionary<string, IClassificationType>();

            this.classificationTypes.Add(FormatNames.Delimiter, registry.GetClassificationType(FormatNames.Delimiter));
            this.classificationTypes.Add(FormatNames.Element, registry.GetClassificationType(FormatNames.Element));
            this.classificationTypes.Add(FormatNames.AttributeName, registry.GetClassificationType(FormatNames.AttributeName));
            this.classificationTypes.Add(FormatNames.Quote, registry.GetClassificationType(FormatNames.Quote));
            this.classificationTypes.Add(FormatNames.AttributeValue, registry.GetClassificationType(FormatNames.AttributeValue));
            this.classificationTypes.Add(FormatNames.Text, registry.GetClassificationType(FormatNames.Text));

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

        private enum State
        {
            Default,
            AfterOpenAngleBracket,
            ElementName,
            InsideAttributeList,
            AttributeName,
            AfterAttributeName,
            AfterAttributeEqualSign,
            AfterOpenDoubleQuote,
            AfterOpenSingleQuote,
            AttributeValue,
            InsideElement,
            AfterCloseAngleBracket,
            AfterOpenTagSlash,
            AfterCloseTagSlash,
        }

        private bool IsNameChar(char c)
        {
            return c == '_' || char.IsLetterOrDigit(c) || c == '-';
        }

        private List<ClassificationSpan> ScanLiteral(SnapshotSpan span)
        {
            State state = State.Default;

            var result = new List<ClassificationSpan>();

            string literal = span.GetText();
            int currentCharIndex = 0;

            int? continuousMark = null;
            bool insideSingleQuote = false;
            bool insideDoubleQuote = false;

            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];

                switch (state)
                {
                    case State.Default:
                        {
                            if (c != '<')
                            {
                                return null;
                            }
                            else
                            {
                                state = State.AfterOpenAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            break;
                        }
                    case State.AfterOpenAngleBracket:
                        {
                            if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.ElementName;
                            }
                            else if (c == '/')
                            {
                                state = State.AfterCloseTagSlash;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.ElementName:
                        {
                            if (IsNameChar(c))
                            {

                            }
                            else if (char.IsWhiteSpace(c))
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), classificationTypes[FormatNames.Element]));
                                    continuousMark = null;
                                }
                                state = State.InsideAttributeList;
                            }
                            else if (c == '>')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), classificationTypes[FormatNames.Element]));
                                    continuousMark = null;
                                }

                                state = State.AfterCloseAngleBracket;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else if (c == '/')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), classificationTypes[FormatNames.Element]));
                                    continuousMark = null;
                                }

                                state = State.AfterOpenTagSlash;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.InsideAttributeList:
                        {
                            if (char.IsWhiteSpace(c))
                            {

                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeName;
                            }
                            else if (c == '>')
                            {
                                state = State.AfterCloseAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else if (c == '/')
                            {
                                state = State.AfterOpenTagSlash;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AttributeName:
                        {
                            if (char.IsWhiteSpace(c))
                            {
                                if (continuousMark.HasValue)
                                {
                                    int length = currentCharIndex - continuousMark.Value;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + continuousMark.Value, length), classificationTypes[FormatNames.AttributeName]));
                                    continuousMark = null;
                                }
                                state = State.AfterAttributeName;
                            }
                            else if (IsNameChar(c))
                            {

                            }
                            else if (c == '=')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int attrNameStart = continuousMark.Value;
                                    int attrNameLength = currentCharIndex - attrNameStart;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), classificationTypes[FormatNames.AttributeName]));
                                }

                                state = State.AfterAttributeEqualSign;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else if (c == '>')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int attrNameStart = continuousMark.Value;
                                    int attrNameLength = currentCharIndex - attrNameStart;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), classificationTypes[FormatNames.AttributeName]));
                                }

                                state = State.AfterCloseAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else if (c == '/')
                            {
                                if (continuousMark.HasValue)
                                {
                                    int attrNameStart = continuousMark.Value;
                                    int attrNameLength = currentCharIndex - attrNameStart;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), classificationTypes[FormatNames.AttributeName]));
                                }

                                state = State.AfterOpenTagSlash;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterAttributeName:
                        {
                            if (char.IsWhiteSpace(c))
                            {

                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeName;
                            }
                            else if (c == '=')
                            {
                                state = State.AfterAttributeEqualSign;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else if (c == '/')
                            {
                                state = State.AfterOpenTagSlash;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else if (c == '>')
                            {
                                state = State.AfterCloseAngleBracket;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterAttributeEqualSign:
                        {
                            if (char.IsWhiteSpace(c))
                            {

                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeValue;
                            }
                            else if (c == '\"')
                            {
                                state = State.AfterOpenDoubleQuote;
                                insideDoubleQuote = true;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Quote]));
                            }
                            else if (c == '\'')
                            {
                                state = State.AfterOpenSingleQuote;
                                insideSingleQuote = true;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Quote]));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterOpenDoubleQuote:
                        {
                            if (c == '\"')
                            {
                                state = State.InsideAttributeList;
                                insideDoubleQuote = false;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Quote]));
                            }
                            else
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeValue;
                            }
                            break;
                        }
                    case State.AfterOpenSingleQuote:
                        {
                            if (c == '\'')
                            {
                                state = State.InsideAttributeList;
                                insideSingleQuote = false;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Quote]));
                            }
                            else
                            {
                                continuousMark = currentCharIndex;
                                state = State.AttributeValue;
                            }
                            break;
                        }
                    case State.AttributeValue:
                        {
                            if (c == '\'')
                            {
                                if (insideSingleQuote)
                                {
                                    state = State.InsideAttributeList;
                                    insideSingleQuote = false;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Quote]));

                                    if (continuousMark.HasValue)
                                    {
                                        int start = continuousMark.Value;
                                        int length = currentCharIndex - start;
                                        continuousMark = null;

                                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), classificationTypes[FormatNames.AttributeValue]));
                                    }
                                }
                            }
                            else if (c == '\"')
                            {
                                if (insideDoubleQuote)
                                {
                                    state = State.InsideAttributeList;
                                    insideDoubleQuote = false;
                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Quote]));

                                    if (continuousMark.HasValue)
                                    {
                                        int start = continuousMark.Value;
                                        int length = currentCharIndex - start;
                                        continuousMark = null;

                                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), classificationTypes[FormatNames.AttributeValue]));
                                    }
                                }
                            }
                            else
                            {

                            }

                            break;
                        }
                    case State.AfterCloseAngleBracket:
                        {
                            if (c == '<')
                            {
                                state = State.AfterOpenAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else
                            {
                                continuousMark = currentCharIndex;
                                state = State.InsideElement;
                            }
                            break;
                        }
                    case State.InsideElement:
                        {
                            if (c == '<')
                            {
                                state = State.AfterOpenAngleBracket;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));

                                if (continuousMark.HasValue)
                                {
                                    int start = continuousMark.Value;
                                    int length = currentCharIndex - start;
                                    continuousMark = null;

                                    result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), classificationTypes[FormatNames.Text]));
                                }
                            }
                            else
                            {

                            }

                            break;
                        }
                    case State.AfterCloseTagSlash:
                        {
                            if (char.IsWhiteSpace(c))
                            {

                            }
                            else if (IsNameChar(c))
                            {
                                continuousMark = currentCharIndex;
                                state = State.ElementName;
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    case State.AfterOpenTagSlash:
                        {
                            if (c == '>')
                            {
                                state = State.AfterCloseAngleBracket;
                                continuousMark = null;
                                result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + currentCharIndex, 1), classificationTypes[FormatNames.Delimiter]));
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        }
                    default:
                        break;
                }

                ++currentCharIndex;
            }

            // if the continuous span is stopped because of end of literal,
            // the span was not colored, handle it here
            if (currentCharIndex >= literal.Length)
            {
                if (continuousMark.HasValue)
                {
                    if (state == State.ElementName)
                    {
                        int start = continuousMark.Value;
                        int length = literal.Length - start;
                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), classificationTypes[FormatNames.Element]));
                    }
                    else if (state == State.AttributeName)
                    {
                        int attrNameStart = continuousMark.Value;
                        int attrNameLength = literal.Length - attrNameStart;
                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + attrNameStart, attrNameLength), classificationTypes[FormatNames.AttributeName]));
                    }
                    else if (state == State.AttributeValue)
                    {
                        int start = continuousMark.Value;
                        int length = literal.Length - start;
                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), classificationTypes[FormatNames.AttributeValue]));
                    }
                    else if (state == State.InsideElement)
                    {
                        int start = continuousMark.Value;
                        int length = literal.Length - start;
                        result.Add(new ClassificationSpan(new SnapshotSpan(span.Start + start, length), classificationTypes[FormatNames.Text]));
                    }
                }
            }

            return result;
        }
    }
}
