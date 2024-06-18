using LitSyntaxHighlighter.Utility;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private SortedDictionary<SnapshotSpan, TagType> _tagCache;
        private LitTemplateTagRegistry _tagRegistry;
        private ITextSnapshot _snapshot;

        public LitTemplateTagManager(IClassificationTypeRegistryService classificationTypeRegistryService)
        {
            _tagRegistry = new LitTemplateTagRegistry(classificationTypeRegistryService);
            _tagCache = new SortedDictionary<SnapshotSpan, TagType>(new SnapshotSpanComparer());
        }

        public IEnumerable<TagSpan<ClassificationTag>> GetClassifications(SnapshotSpan span)
        {
            return _tagCache.Keys
                .Where(s => s.IntersectsWith(span))
                .Select(s => new TagSpan<ClassificationTag>(s, _tagRegistry.ClassificationTags[_tagCache[s]]));
        }

        public bool IsSnapshotValid(ITextSnapshot snapshot)
        {
            return _snapshot == snapshot;
        }

        public void TryParseTags(SnapshotSpan span, IEnumerable<ITextChange> changes = null)
        {
            try
            {
                if (_snapshot == span.Snapshot)
                {
                    return;
                }
                _snapshot = span.Snapshot;

                if (changes == null || changes.Count() == 0)
                {
                    _tagCache.Clear();
                    ProcessEntireSpan(span);
                }
                else
                {
                    ProcessSpecificSpans(span, changes.Select(s => s.NewSpan));
                }
            }
            catch(Exception ex)
            {
                _tagCache.Clear();
                Debug.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private bool IsNameChar(char c)
        {
            return c == '_' || c == '-' || char.IsLetterOrDigit(c);
        }

        private bool IsTextChar(char c)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        public void UpdateSelectedClassification(SnapshotPoint selectionPoint)
        {
            // reset old tags
            var oldSelectedTags = _tagCache.Where(s => s.Value == TagType.SelectedElement || s.Value == TagType.SelectedCloseElement || s.Value == TagType.SelectedSelfCloseElement).ToList();
            foreach (var tag in oldSelectedTags)
            {
                _tagCache[tag.Key] = tag.Value == TagType.SelectedElement ? TagType.Element : tag.Value == TagType.SelectedCloseElement ? TagType.CloseElement : TagType.SelfCloseElement;
            }

            // find new tags
            var selectedSpan = _tagCache
                .Where(s => (s.Key.Contains(selectionPoint) || s.Key.Contains(selectionPoint - 1)) && (s.Value == TagType.Element || s.Value == TagType.CloseElement || s.Value == TagType.SelfCloseElement))
                .Select(s => (SnapshotSpan?)s.Key)
                .FirstOrDefault();
            if (selectedSpan.HasValue)
            {
                var key = selectedSpan.Value;
                var type = _tagCache[key];
                CreateTagCacheEntry(key.Start, key.Length, type == TagType.Element ? TagType.SelectedElement : type == TagType.CloseElement ? TagType.SelectedCloseElement : TagType.SelfCloseElement);
                if (type != TagType.SelfCloseElement)
                {
                    FindMatchingTag(key, type == TagType.Element ? TagType.CloseElement : TagType.Element);
                }
            }
        }

        private void FindMatchingTag(SnapshotSpan span, TagType targetType)
        {
            bool isOpen = targetType == TagType.CloseElement;
            var possibleTags = _tagCache.Where(s =>
                s.Key.GetText() == span.GetText() &&
                (isOpen ? s.Key.Start > span.Start : s.Key.Start < span.Start)
            );

            int tagDepth = 0;
            foreach(var tag in isOpen ? possibleTags : possibleTags.Reverse())
            {
                if(tag.Value == targetType)
                {
                    if (tagDepth == 0)
                    {
                        CreateTagCacheEntry(tag.Key.Start, tag.Key.Length, tag.Value == TagType.Element ? TagType.SelectedElement : TagType.SelectedCloseElement);
                        break;
                    }
                    tagDepth -= 1;
                }
                else if(tag.Value == (isOpen ? TagType.Element : TagType.CloseElement))
                {
                    tagDepth += 1;
                }
            }
        }

        private void CreateTagCacheEntry(int index, int length, TagType type)
        {
            var key = new SnapshotSpan(_snapshot, index, length);
            if (_tagCache.ContainsKey(key))
            {
                _tagCache[key] = type;
            }
            else
            {
                _tagCache.Add(key, type);
            }
        }

        private void ProcessEntireSpan(SnapshotSpan span)
        {
            string literal = span.GetText();

            foreach (Match startMatch in _openTemplateRegex.Matches(literal))
            {
                ProcessTemplate(literal, startMatch.Index + startMatch.Length);
            }
        }

        private void ProcessSpecificSpans(SnapshotSpan span, IEnumerable<Span> changes)
        {
            string literal = span.GetText();

            AutoCompleteSelectedTag(ref span, ref literal, changes);

            // check for changed spans
            foreach (Match startMatch in _openTemplateRegex.Matches(literal))
            {
                var subSpan = new SnapshotSpan(span.Start + startMatch.Index + startMatch.Length, span.Length - (startMatch.Index + startMatch.Length));
                var changedSpans = changes.Where(s => s.IntersectsWith(subSpan));

                if (changedSpans.Count() > 0)
                {

                    var endIndex = ProcessTemplate(literal, startMatch.Index + startMatch.Length);

                    RemoveOldTemplateSpans(startMatch, endIndex);
                }
            }

            // update snapshot for remaining spans not effected by the change
            foreach (var cache in _tagCache.ToList().Where(cache => _tagCache.Remove(cache.Key)))
            {
                var newKey = cache.Key.TranslateTo(_snapshot, SpanTrackingMode.EdgeInclusive);
                if (!_tagCache.ContainsKey(newKey))
                    _tagCache.Add(newKey, cache.Value);
            }
        }

        private void RemoveOldTemplateSpans(Match startMatch, int endIndex)
        {
            // remove any old tags from the current template
            foreach (var oldCache in _tagCache.Keys
                .Where(s => s.Snapshot != _snapshot &&
                    s.Start.Position >= startMatch.Index &&
                    s.Start.Position + s.Length <= endIndex)
                .ToList()
            )
            {
                _tagCache.Remove(oldCache);
            }
        }

        private void AutoCompleteSelectedTag(ref SnapshotSpan span, ref string literal, IEnumerable<Span> changedSpans)
        {
            // check for selected element name change and update closing tag
            var selectedTag = _tagCache.Where(s => s.Value == TagType.SelectedElement).Select(s => (SnapshotSpan?)s.Key).FirstOrDefault();
            var selectedCloseTag = _tagCache.Where(s => s.Value == TagType.SelectedCloseElement).Select(s => (SnapshotSpan?)s.Key).FirstOrDefault();
            if (selectedTag.HasValue && selectedCloseTag.HasValue)
            {
                var nameChange = changedSpans.Where(c => c.IntersectsWith(selectedTag.Value.Span)).Select(c => (Span?)c).FirstOrDefault();
                if (nameChange.HasValue)
                {
                    var newName = selectedTag.Value.TranslateTo(_snapshot, SpanTrackingMode.EdgeInclusive).GetText();
                    var closeTagSpan = selectedCloseTag.Value.TranslateTo(_snapshot, SpanTrackingMode.EdgeInclusive);
                    if (newName.All(c => IsNameChar(c)))
                    {
                        span.Snapshot.TextBuffer.Replace(closeTagSpan.Span, newName);
                    }
                    literal = span.GetText();
                }
            }
        }

        public int ProcessTemplate(string literal, int startIndex)
        {
            Stack<TemplateState> state = new Stack<TemplateState>();
            state.Push(TemplateState.InsideTemplate);
            int currentCharIndex = startIndex;
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
                                return currentCharIndex;
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
                                return currentCharIndex;
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
                                CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
                                state.Push(TemplateState.TagStart);
                            }
                            else if (peek == TemplateState.BetweenTags)
                            {
                                if (literal.Length <= currentCharIndex + 1)
                                {
                                    CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
                                    return currentCharIndex;
                                }
                                char nextChar = literal[currentCharIndex + 1];
                                if (nextChar == '/')
                                {
                                    CreateTagCacheEntry(currentCharIndex, 2, TagType.Delimiter);
                                    currentCharIndex += 1;
                                    state.Pop();
                                    state.Push(TemplateState.CloseTagStart);
                                }
                                else
                                {
                                    CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
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
                                    CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
                                    return currentCharIndex;
                                }
                                char nextChar = literal[currentCharIndex + 1];
                                if (nextChar == '>')
                                {
                                    CreateTagCacheEntry(currentCharIndex, 2, TagType.Delimiter);
                                    currentCharIndex += 1;
                                    state.Pop();
                                }
                                else
                                {
                                    CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
                                }
                            }
                            break;
                        }
                    case '>':
                        {
                            var peek = state.Peek();
                            if (peek == TemplateState.InsideTag)
                            {
                                CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
                                state.Pop();
                                state.Push(TemplateState.BetweenTags);
                            }
                            else if (peek == TemplateState.InsideCloseTag)
                            {
                                CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
                                state.Pop();
                            }
                            else if (peek == TemplateState.InsideCommentTag && currentCharIndex >= 2)
                            {
                                if (literal.Substring(currentCharIndex - 2, 3) == "-->")
                                {
                                    state.Pop();
                                    CreateTagCacheEntry(nameCharIndex.Value, currentCharIndex - nameCharIndex.Value - 2, TagType.Comment);
                                    CreateTagCacheEntry(currentCharIndex - 2, 3, TagType.Delimiter);
                                    nameCharIndex = null;
                                }
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
                                    CreateTagCacheEntry(currentCharIndex, 3, TagType.Delimiter);
                                    currentCharIndex += 2;
                                    nameCharIndex = currentCharIndex;
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
                                    if(peek == TemplateState.ElementName)
                                    {
                                        if(nextChar == '/')
                                        {
                                            CreateTagCacheEntry(nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, TagType.SelfCloseElement);
                                        }
                                        else
                                        {
                                            CreateTagCacheEntry(nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, TagType.Element);
                                        }
                                    }
                                    else
                                    {
                                        CreateTagCacheEntry(nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, TagType.CloseElement);
                                    }
                                    
                                    var type = state.Pop();
                                    state.Push(type == TemplateState.ElementName ? TemplateState.InsideTag : TemplateState.InsideCloseTag);
                                    nameCharIndex = null;
                                }
                                else if ((peek == TemplateState.AttributeName || peek == TemplateState.EventName) && !IsNameChar(nextChar))
                                {
                                    var type = state.Pop();
                                    CreateTagCacheEntry(nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, type == TemplateState.AttributeName ? TagType.AttributeName : TagType.EventName);
                                    if (nextChar == '=')
                                    {
                                        CreateTagCacheEntry(currentCharIndex + 1, 1, TagType.Delimiter);
                                        currentCharIndex += 1;
                                    }
                                    nameCharIndex = null;
                                }
                                else if (peek == TemplateState.BetweenTagsText && !IsTextChar(nextChar))
                                {
                                    state.Pop();
                                    CreateTagCacheEntry(nameCharIndex.Value, currentCharIndex - nameCharIndex.Value + 1, TagType.Text);
                                    nameCharIndex = null;
                                }
                            }
                            else
                            {
                                return currentCharIndex;
                            }
                            break;
                        }
                }
                currentCharIndex += 1;
            }
            return currentCharIndex;
        }
    }
}
