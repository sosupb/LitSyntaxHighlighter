using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace LitSyntaxHighlighter.Tagger
{
    internal class LitTemplateTagger : ITagger<ClassificationTag>
    {
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private LitTemplateTagManager _tagManager;
        private ITextBuffer _sourceBuffer;
        private ITextView _textView;
        private SnapshotPoint? _currentCaretPoint;

        internal LitTemplateTagger(IClassificationTypeRegistryService classificationRegistry, ITextBuffer textBuffer, ITextView view) 
        {
            this._tagManager = new LitTemplateTagManager(classificationRegistry);
            this._tagManager.TryParseTags(new SnapshotSpan(textBuffer.CurrentSnapshot, 0, textBuffer.CurrentSnapshot.Length));
            this._sourceBuffer = textBuffer;
            this._textView = view;
            this._sourceBuffer.Changed += OnBufferChanged;
            this._textView.Caret.PositionChanged += OnCaretPositionChanged;
            this._textView.LayoutChanged += OnLayoutChanged;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.NewSnapshot != e.OldSnapshot) //make sure that there has really been a change
            {
                UpdateAtCaretPosition(_textView.Caret.Position);
            }
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            UpdateAtCaretPosition(e.NewPosition);
        }

        private void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            _currentCaretPoint = caretPosition.Point.GetPoint(_sourceBuffer, caretPosition.Affinity);

            if (!_currentCaretPoint.HasValue || !_tagManager.IsSnapshotValid(_currentCaretPoint.Value.Snapshot))
                return;

            _tagManager.UpdateSelectedClassification(_currentCaretPoint.Value);

            var tempEvent = TagsChanged;

            if (tempEvent != null)
                tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0,
                    _sourceBuffer.CurrentSnapshot.Length)));
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.After != _sourceBuffer.CurrentSnapshot)
                return;

            // UpdateAtCaretPosition(_textView.Caret.Position);
            _tagManager.TryParseTags(
                new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length),
                e.Changes
            );
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || !_tagManager.IsSnapshotValid(spans[0].Snapshot))
            {
                yield break;
            }

            if (_currentCaretPoint.HasValue && _currentCaretPoint.Value.Position < _currentCaretPoint.Value.Snapshot.Length)
            {
                SnapshotPoint caretPosition = _currentCaretPoint.Value;
                if (caretPosition.Snapshot != spans[0].Snapshot)
                {
                    caretPosition.TranslateTo(spans[0].Snapshot, PointTrackingMode.Positive);
                }
            }

            foreach (var span in spans)
            {
                foreach (var tag in _tagManager.GetClassifications(span))
                {
                    yield return tag;
                }
            }
        }
    }
}
