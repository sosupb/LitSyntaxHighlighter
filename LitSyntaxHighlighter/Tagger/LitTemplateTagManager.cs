using LitSyntaxHighlighter.Utility;
using Microsoft.VisualStudio.Text;
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
        private readonly Regex _closeTemplateRegex = new Regex("(`|\\${|}|\")", RegexOptions.IgnoreCase);

        private SortedDictionary<SnapshotSpan, TagType> _tagCache;
        private ITextSnapshot _currentSnapshot;
        private bool _isParsingTags;

        public bool IsDirty
        {  
            get
            {
                return this._isParsingTags || (_tagCache.Count > 0 && _tagCache.Any(s => s.Key.Snapshot != _currentSnapshot));
            }
        }
        public KeyValuePair<SnapshotSpan, TagType>? SelectedOpenTag { get; private set; }
        public KeyValuePair<SnapshotSpan, TagType>? SelectedCloseTag {  get; private set; }

        public LitTemplateTagManager()
        {
            this._tagCache = new SortedDictionary<SnapshotSpan, TagType>(new SnapshotSpanComparer());
        }

        public IEnumerable<KeyValuePair<SnapshotSpan,TagType>> GetClassifications(SnapshotSpan span)
        {
            if(IsDirty || span.Snapshot != _currentSnapshot)
            {
                TryParseTags(span.Snapshot);
                return Enumerable.Empty<KeyValuePair<SnapshotSpan, TagType>>();
            }

            return _tagCache.Keys
                .Where(s => s.IntersectsWith(span))
                .Select(s => {
                    if (SelectedOpenTag.HasValue && s == SelectedOpenTag.Value.Key)
                        return SelectedOpenTag.Value;
                    if(SelectedCloseTag.HasValue && s == SelectedCloseTag.Value.Key)
                        return SelectedCloseTag.Value;
                    return new KeyValuePair<SnapshotSpan, TagType>(s, _tagCache[s]);

                });
        }

        public void TryParseTags(ITextSnapshot newSnapshot, ITextChange change = null)
        {
            if (this._isParsingTags)
                return;
            try
            {
                if (_currentSnapshot == newSnapshot)
                {
                    return;
                }

                this._isParsingTags = true;
                _currentSnapshot = newSnapshot;

                if (change == null)
                {
                    _tagCache.Clear();
                    ProcessEntireSpan(new SnapshotSpan(_currentSnapshot, 0, _currentSnapshot.Length));
                }
                else if(change.NewSpan.Start > 3)
                {
                    var changedText = new SnapshotSpan(_currentSnapshot, change.NewSpan.Start - 4, 5).GetText();
                    if(changedText != "html`")
                    {
                        ProcessSpecificSpans(new SnapshotSpan(_currentSnapshot, 0, _currentSnapshot.Length), change.NewSpan);
                    }
                }

                if (IsDirty)
                {
                    CleanupRemainingSnapshotTags();
                }
                _isParsingTags = false;
            }
            catch(Exception ex)
            {
                _tagCache.Clear();
                Debug.WriteLine($"ERROR: {ex.Message}");
            }
        }

        public bool IsNameChar(char c)
        {
            return c == '_' || c == '-' || char.IsLetterOrDigit(c);
        }

        public bool IsTextChar(char c)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }

        public void TryUpdateSelectedClassification(SnapshotPoint selectionPoint)
        {
            try
            {
                // skip resetting an empty tag
                if (SelectedOpenTag.HasValue)
                {
                    var newSelectedOpenTag = SelectedOpenTag.Value.Key.TranslateTo(selectionPoint.Snapshot, SpanTrackingMode.EdgeInclusive);
                    if(newSelectedOpenTag.Length == 0 && newSelectedOpenTag.Start.Position == selectionPoint.Position)
                    {
                        return;
                    }
                }

                // reset selected tags
                SelectedOpenTag = null;
                SelectedCloseTag = null;

                // find new tags
                var selectedSpan = _tagCache
                    .Where(s =>
                        (
                            s.Key.Snapshot == selectionPoint.Snapshot &&
                            s.Key.Contains(selectionPoint) ||
                            (selectionPoint > 0 && s.Key.Contains(selectionPoint - 1))
                        ) &&
                        ((s.Value & (TagType.OpenTags | TagType.CloseTags)) > 0)
                    )
                    .Select(s => (SnapshotSpan?)s.Key)
                    .FirstOrDefault();
                if (selectedSpan.HasValue)
                {
                    var key = selectedSpan.Value;
                    var type = _tagCache[key];
                    TagType? target = null;
                    if((type & TagType.OpenTags) > 0)
                    {
                        SelectedOpenTag = new KeyValuePair<SnapshotSpan, TagType>(key, type == TagType.SelfCloseElement ? TagType.SelectedSelfCloseElement : TagType.SelectedOpenElement);
                        target = type == TagType.Element ? TagType.CloseElement : TagType.CommentEnd;
                        if (type != TagType.SelfCloseElement)
                        {
                            SelectedCloseTag = FindMatchingTag(key, type, target.Value);
                        }
                    }
                    else
                    {
                        SelectedCloseTag = new KeyValuePair<SnapshotSpan, TagType>(key, TagType.SelectedCloseElement);
                        target = type == TagType.CloseElement ? TagType.Element : TagType.CommentStart;
                        SelectedOpenTag = FindMatchingTag(key, type, target.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _tagCache.Clear();
                Debug.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private KeyValuePair<SnapshotSpan, TagType>? FindMatchingTag(SnapshotSpan span, TagType type, TagType targetType)
        {
            bool isOpen = (type & TagType.OpenTags) > 0;
            var possibleTags = _tagCache.Where(s =>
                s.Key.GetText() == span.GetText() || 
                ((s.Value & TagType.CommentTags) > 0 && (type & TagType.CommentTags) > 0)
            );

            int tagDepth = 0;
            int? targetTagDepth = null;
            KeyValuePair<SnapshotSpan, TagType>? matchingTag = null;
            foreach (var tag in isOpen ? possibleTags : possibleTags.Reverse())
            {
                if(tag.Key == span)
                {
                    targetTagDepth = tagDepth;
                }
                else if(tag.Value == targetType)
                {
                    if (targetTagDepth.HasValue && tagDepth == targetTagDepth && !matchingTag.HasValue)
                    {
                        matchingTag = new KeyValuePair<SnapshotSpan, TagType>(tag.Key, isOpen ? TagType.SelectedCloseElement : TagType.SelectedOpenElement);
                    }
                    else
                    {
                        tagDepth -= 1;
                    }
                }
                else if(tag.Value == type)
                {
                    tagDepth += 1;
                }
            }
            return tagDepth != 0 ? null : matchingTag;
        }

        private int FindMatchingCloseTemplate(SnapshotSpan span, int startIndex)
        {
            string literal = span.GetText();

            Stack<string> stack = new Stack<string>();
            foreach (Match closeMatch in _closeTemplateRegex.Matches(literal, startIndex))
            {
                var peek = stack.Count > 0 ? stack.Peek() : null;
                if(closeMatch.Index > 4 && literal.Substring(closeMatch.Index - 4, 5) == "html`")
                {
                    stack.Push("html`");
                }
                else if(literal.Substring(closeMatch.Index, closeMatch.Length) == "${")
                {
                    stack.Push("${");
                }
                else if(literal.Substring(closeMatch.Index, closeMatch.Length) == "}")
                {
                    if(peek == "${")
                    {
                        stack.Pop();
                    }
                }
                else if(literal.Substring(closeMatch.Index, closeMatch.Length) == "\"")
                {
                    if (peek == "\"")
                    {
                        stack.Pop();
                    }
                    else
                    {
                        stack.Push("\"");
                    }
                }
                else if(literal.Substring(closeMatch.Index, closeMatch.Length) == "`")
                {
                    if(stack.Count() == 0)
                    {
                        return closeMatch.Index;
                    }
                    else if(peek == "`") 
                    {
                        stack.Pop();
                    }
                    else
                    {
                        stack.Push("`");
                    }
                }
            }
            return span.Length;
        }

        private void CleanupRemainingSnapshotTags()
        {
            // update snapshot version for remaining spans not effected by the change
            foreach (var cache in _tagCache.Where(c => c.Key.Snapshot != _currentSnapshot).ToList().Where(cache => _tagCache.Remove(cache.Key)))
            {
                var newKey = cache.Key.TranslateTo(_currentSnapshot, SpanTrackingMode.EdgeInclusive);
                if (!_tagCache.ContainsKey(newKey))
                    _tagCache.Add(newKey, cache.Value);
            }
        }

        private void RemoveOldTemplateSpans(int startIndex, int endIndex)
        {
            // remove any old tags from the current template ex: spans that no longer exist in the current snapshot
            foreach (var oldCache in _tagCache.Keys
                .Where(s => s.Snapshot != _currentSnapshot &&
                    s.Start.Position >= startIndex &&
                    s.Start.Position + s.Length < endIndex)
                .ToList()
            )
            {
                _tagCache.Remove(oldCache);
            }
        }

        private void CreateTagCacheEntry(int index, int length, TagType type)
        {
            var key = new SnapshotSpan(_currentSnapshot, index, length);
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

        private void ProcessSpecificSpans(SnapshotSpan span, Span change)
        {
            string literal = span.GetText();

            // check for changed spans
            int startIndex = 0;
            int endIndex = span.Length;
            foreach (Match startMatch in _openTemplateRegex.Matches(literal))
            {
                var subSpanStartIndex = startMatch.Index + startMatch.Length;
                var subSpanEndIndex = FindMatchingCloseTemplate(span, subSpanStartIndex);
                var subSpan = new SnapshotSpan(span.Start + subSpanStartIndex, subSpanEndIndex - subSpanStartIndex);
                var isChanged = change.IntersectsWith(subSpan);

                if (isChanged)
                {
                    startIndex = startIndex < subSpan.Start ? subSpan.Start : startIndex;
                    endIndex = endIndex > subSpanEndIndex ? subSpanEndIndex : endIndex;
                }
                else
                {
                    if (change.Start < subSpan.Start)
                    {
                        endIndex = endIndex > subSpan.Start ? subSpan.Start : endIndex;
                    }
                }
            }

            ProcessTemplate(literal, startIndex);
            RemoveOldTemplateSpans(startIndex, endIndex);
        }

        private int ProcessTemplate(string literal, int startIndex)
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
                            else if (peek == TemplateState.InsideTag)
                            {
                                state.Pop();
                                currentCharIndex -= 1;
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
                                    CreateTagCacheEntry(currentCharIndex - 2, 2, TagType.CommentEnd);
                                    CreateTagCacheEntry(currentCharIndex, 1, TagType.Delimiter);
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
                                    CreateTagCacheEntry(currentCharIndex, 3, TagType.CommentStart);
                                    currentCharIndex += 2;
                                    nameCharIndex = currentCharIndex;
                                }
                            }
                            else if (peek == TemplateState.InsideCommentTag && literal.Length > currentCharIndex + 3)
                            {
                                if (literal.Substring(currentCharIndex - 1, 4) == "<!--")
                                {
                                    CreateTagCacheEntry(currentCharIndex, 3, TagType.CommentStart);
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
