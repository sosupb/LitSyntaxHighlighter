using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LitSyntaxHighlighter.Tagger
{
    internal class LitTemplateTagManager
    {
        private enum TemplateState
        {
            Default = 0,
            PauseString,
            PauseFormatString,
            PauseCurly,
            InsideTemplate,
            TagStart,
            ElementName,
            InsideTag,
            AttributeName,
            EventName,
            BetweenTags,
            BetweenTagsText,
            CloseTagStart,
            CloseElementName,
            InsideCloseTag,
            InsideCommentTag,
        }
        private readonly Regex _openTemplateRegex = new Regex("html`", RegexOptions.IgnoreCase);

        private IDictionary<SnapshotSpan, ClassificationTag> _tagCache;
        private LitTemplateTagRegistry _tagRegistry;
        private ITextSnapshot _snapshot;

        public LitTemplateTagManager(IClassificationTypeRegistryService classificationTypeRegistryService) 
        {
            _tagRegistry = new LitTemplateTagRegistry(classificationTypeRegistryService);
            _tagCache = new Dictionary<SnapshotSpan, ClassificationTag>();
        }

        public IEnumerable<TagSpan<ClassificationTag>> GetClassifications(SnapshotSpan span)
        {
            return _tagCache.Keys
                .Where(s => s.IntersectsWith(span))
                .Select(s => new TagSpan<ClassificationTag>(s, _tagCache[s]));
        }

        public bool IsValidSnapshotVersion(ITextSnapshot snapshot)
        {
            return _snapshot == snapshot;
        }

        public void ParseTags(SnapshotSpan span)
        {
            if(_snapshot == span.Snapshot)
            {
                return;
            }
            _snapshot = span.Snapshot;
            _tagCache.Clear();

            ProcessEntireSpan(span);
        }

        private bool IsNameChar(char c)
        {
            return c == '_' || c == '-' || char.IsLetterOrDigit(c);
        }

        private bool IsTextChar(char c)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        public void CreateTagCacheEntry(SnapshotSpan span, int index, int length, TagType type)
        {
            var key = new SnapshotSpan(span.Start + index, length);
            var value = _tagRegistry.ClassificationTags[type];
            _tagCache.Add(key, value);
        }

        private void ProcessEntireSpan(SnapshotSpan span)
        {
            string literal = span.GetText();

            foreach (Match startMatch in _openTemplateRegex.Matches(literal))
            {
                ProcessTemplate(span, literal, startMatch);
            }
        }

        public void ProcessTemplate(SnapshotSpan span, string literal, Match startMatch)
        {
            Stack<TemplateState> state = new Stack<TemplateState>();
            state.Push(TemplateState.InsideTemplate);
            int currentCharIndex = startMatch.Index + startMatch.Length;
            int? nameCharIndex = null;

            while (currentCharIndex < literal.Length)
            {
                char c = literal[currentCharIndex];
                switch (c)
                {
                    case '`':
                        {
                            if (state.Peek() >= TemplateState.InsideTemplate)
                            {
                                return;
                            }
                            else if (state.Peek() == TemplateState.PauseFormatString)
                            {
                                state.Pop();
                                break;
                            }
                            state.Push(TemplateState.PauseFormatString);
                            break;
                        }
                    case '$':
                        {
                            if (literal.Length <= currentCharIndex + 1)
                                return;
                            char nextChar = literal[currentCharIndex + 1];
                            if (nextChar == '{')
                            {
                                currentCharIndex += 1;
                                state.Push(TemplateState.PauseCurly);
                            }
                            break;
                        }
                    case '{':
                        {
                            if (state.Peek() < TemplateState.InsideTemplate)
                            {
                                state.Push(TemplateState.PauseCurly);
                            }
                            break;
                        }
                    case '}':
                        {
                            if (state.Peek() == TemplateState.PauseCurly)
                            {
                                state.Pop();
                            }
                            break;
                        }
                    case '"':
                        {
                            if (state.Peek() >= TemplateState.InsideTemplate)
                            {
                                state.Push(TemplateState.PauseString);
                            }
                            else if (state.Peek() == TemplateState.PauseString)
                            {
                                state.Pop();
                            }
                            break;
                        }
                    case '<':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideTemplate)
                            {
                                CreateTagCacheEntry(span, currentCharIndex, 1, TagType.Delimiter);
                                state.Push(TemplateState.TagStart);
                            }
                            else if (peek == TemplateState.BetweenTags)
                            {
                                if (literal.Length <= currentCharIndex + 1)
                                {
                                    CreateTagCacheEntry(span, currentCharIndex, 1, TagType.Delimiter);
                                    return;
                                }
                                char nextChar = literal[currentCharIndex + 1];
                                if (nextChar == '/')
                                {
                                    CreateTagCacheEntry(span, currentCharIndex, 2, TagType.Delimiter);
                                    currentCharIndex += 1;
                                    state.Pop();
                                    state.Push(TemplateState.CloseTagStart);
                                }
                                else
                                {
                                    CreateTagCacheEntry(span, currentCharIndex, 1, TagType.Delimiter);
                                    state.Push(TemplateState.TagStart);
                                }
                            }
                            break;
                        }
                    case '/':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideTag)
                            {
                                if (literal.Length <= currentCharIndex + 1)
                                {
                                    CreateTagCacheEntry(span, currentCharIndex, 1, TagType.Delimiter);
                                    return;
                                }
                                char nextChar = literal[currentCharIndex + 1];
                                if (nextChar == '>')
                                {
                                    CreateTagCacheEntry(span, currentCharIndex, 2, TagType.Delimiter);
                                    currentCharIndex += 1;
                                    state.Pop();
                                }
                                else
                                {
                                    CreateTagCacheEntry(span, currentCharIndex, 1, TagType.Delimiter);
                                }
                            }
                            break;
                        }
                    case '>':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideTag)
                            {
                                CreateTagCacheEntry(span, currentCharIndex, 1, TagType.Delimiter);
                                state.Pop();
                                state.Push(TemplateState.BetweenTags);
                            }
                            else if (peek == TemplateState.InsideCloseTag)
                            {
                                CreateTagCacheEntry(span, currentCharIndex, 1, TagType.Delimiter);
                                state.Pop();
                            }
                            break;
                        }
                    case '.':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideTag)
                            {
                                state.Push(TemplateState.AttributeName);
                                nameCharIndex = currentCharIndex;
                            }
                            break;
                        }
                    case '?':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideTag)
                            {
                                state.Push(TemplateState.AttributeName);
                                nameCharIndex = currentCharIndex;
                            }
                            break;
                        }
                    case '@':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideTag)
                            {
                                state.Push(TemplateState.EventName);
                                nameCharIndex = currentCharIndex;
                            }
                            break;
                        }
                    case '!':
                        {
                            var peek = state.Peek();
                            if(peek == TemplateState.TagStart && literal.Length > currentCharIndex + 2)
                            {
                                if (literal.Substring(currentCharIndex, 3) == "!--")
                                {
                                    state.Pop();
                                    state.Push(TemplateState.InsideCommentTag);
                                    nameCharIndex = currentCharIndex;
                                    currentCharIndex += 2;
                                }
                            }
                            break;
                        }
                    case '-':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideCommentTag && literal.Length > currentCharIndex + 2)
                            {
                                if(literal.Substring(currentCharIndex, 3) == "-->")
                                {
                                    state.Pop();
                                    CreateTagCacheEntry(span, nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 2, TagType.Comment);
                                    CreateTagCacheEntry(span, currentCharIndex + 2, 1, TagType.Delimiter);
                                    nameCharIndex = null;
                                    currentCharIndex += 2;
                                }
                            }
                            break;
                        }
                    default:
                        {
                            var peek = state.Peek();
                            if(peek == TemplateState.InsideCommentTag)
                            {
                                break;
                            }

                            // process start of name or test
                            if ((peek == TemplateState.TagStart || peek == TemplateState.CloseTagStart) && IsNameChar(c))
                            {
                                var type = state.Pop();
                                state.Push(type == TemplateState.TagStart ? TemplateState.ElementName : TemplateState.CloseElementName);
                                nameCharIndex = currentCharIndex;
                            }
                            else if (peek == TemplateState.InsideTag && IsNameChar(c))
                            {
                                state.Push(TemplateState.AttributeName);
                                nameCharIndex = currentCharIndex;
                            }
                            else if (peek == TemplateState.BetweenTags && IsTextChar(c))
                            {
                                state.Push(TemplateState.BetweenTagsText);
                                nameCharIndex = currentCharIndex;
                            }

                            // check for end of name or text
                            if (literal.Length > currentCharIndex + 1)
                            {
                                peek = state.Peek();
                                char nextChar = literal[currentCharIndex + 1];
                                if ((peek == TemplateState.ElementName || peek == TemplateState.CloseElementName) && !IsNameChar(nextChar))
                                {
                                    CreateTagCacheEntry(span, nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, TagType.Element);
                                    var type = state.Pop();
                                    state.Push(type == TemplateState.ElementName ? TemplateState.InsideTag : TemplateState.InsideCloseTag);
                                    nameCharIndex = null;
                                }
                                else if ((peek == TemplateState.AttributeName || peek == TemplateState.EventName) && !IsNameChar(nextChar))
                                {
                                    var type = state.Pop();
                                    CreateTagCacheEntry(span, nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, type == TemplateState.AttributeName ? TagType.AttributeName : TagType.EventName);
                                    if (nextChar == '=')
                                    {
                                        CreateTagCacheEntry(span, currentCharIndex + 1, 1, TagType.Delimiter);
                                        currentCharIndex += 1;
                                    }
                                    nameCharIndex = null;
                                }
                                else if (peek == TemplateState.BetweenTagsText && !IsTextChar(nextChar))
                                {
                                    state.Pop();
                                    CreateTagCacheEntry(span, nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, TagType.Text);
                                    nameCharIndex = null;
                                }
                            }
                            else
                            {
                                return;
                            }
                            break;
                        }
                }
                currentCharIndex += 1;
            }
        }
    }
}
