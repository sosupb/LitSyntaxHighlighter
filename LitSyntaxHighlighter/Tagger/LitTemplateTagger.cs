using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;
using System.Diagnostics;

namespace LitSyntaxHighlighter.Tagger
{
    internal class LitTemplateTagger : ITagger<ClassificationTag>
    {
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private LitTemplateTagRegistry _tagRegistry;
        private LitTemplateTagManager _tagManager;
        private LitTemplateAutoTagger _autoTagger;
        private ITextBuffer _sourceBuffer;
        private ITextView _textView;
        private ITextUndoHistory _textUndoHistory;
        private SnapshotPoint? _currentCaretPoint;

        internal LitTemplateTagger(
            IClassificationTypeRegistryService classificationRegistry, 
            ITextBuffer textBuffer, 
            ITextView view, 
            ITextUndoHistory textUndoHistory
        ) 
        {
            this._tagRegistry = new LitTemplateTagRegistry(classificationRegistry);
            this._tagManager = new LitTemplateTagManager();
            this._tagManager.TryParseTags(textBuffer.CurrentSnapshot);
            this._autoTagger = new LitTemplateAutoTagger(_tagManager);
            this._autoTagger.EditTextBuffer += OnEditTextBuffer;
            this._autoTagger.MoveCaret += OnMoveCaret;
            this._sourceBuffer = textBuffer;
            this._sourceBuffer.Changed += OnBufferChanged;
            this._sourceBuffer.PostChanged += OnPostBufferChange;
            this._textView = view;
            this._textView.Caret.PositionChanged += OnCaretPositionChanged;
            this._textView.LayoutChanged += OnLayoutChanged;
            this._textUndoHistory = textUndoHistory;
            this._currentCaretPoint = null;

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

            if (!_currentCaretPoint.HasValue || _tagManager.IsDirty)
                return;

            _tagManager.TryUpdateSelectedClassification(_currentCaretPoint.Value);
            _autoTagger.CheckCaretMoveQueued(_currentCaretPoint.Value);

            var tempEvent = TagsChanged;

            if (tempEvent != null)
                tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0,
                    _sourceBuffer.CurrentSnapshot.Length)));
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.After != _sourceBuffer.CurrentSnapshot)
                return;

            foreach (var change in e.Changes)
            {
                _autoTagger.QueueUpAutoTaggingAsync(_sourceBuffer, new SnapshotSpan(_sourceBuffer.CurrentSnapshot, change.NewSpan.Start, change.NewSpan.Length)).FireAndForget();
                _tagManager.TryParseTags(_sourceBuffer.CurrentSnapshot, change);
            }
        }

        private void OnPostBufferChange(object sender, EventArgs e)
        {
            _autoTagger.PostCheckForAutoTagging();
        }

        private void OnMoveCaret(object sender, MoveCaretEventArgs e)
        {
            _textView.Caret.MoveTo(e.Position);
        }

        private void OnEditTextBuffer(object sender, AutoTaggerEventArgs e)
        {
            if (e.Snapshot != _sourceBuffer.CurrentSnapshot)
                return;

            var oldCaretPosition = _textView.Caret.Position.BufferPosition;
            var shouldUpdateCaret = false;
            var targetCaretDelta = 0;

            SetupUndoAction(() =>
            {
                using (var editor = _sourceBuffer.CreateEdit())
                {
                    while (e.Edits.Count > 0)
                    {
                        var edit = e.Edits.Dequeue();
                        if (edit.Operation == "Replace")
                        {
                            editor.Replace(edit.Span, edit.Text);
                        }
                        else if (edit.Operation == "Insert")
                        {
                            editor.Insert(edit.Span.Start + edit.Span.Length, edit.Text);
                        }
                        shouldUpdateCaret |= edit.PreserveCaretPosition;
                        if (shouldUpdateCaret)
                        {
                            targetCaretDelta += edit.Text.Length;
                        }
                    }

                    if(targetCaretDelta > 0)
                    {
                        _autoTagger.SetCaretPosition(oldCaretPosition, targetCaretDelta);
                    }
                    editor.Apply();
                    return !editor.Canceled;
                }
            });
        }

        private void SetupUndoAction(Func<bool> action)
        {
            using (var undoTransaction = _textUndoHistory.CreateTransaction("Lit Auto Tagger"))
            {
                try
                {
                    if (!action())
                    {
                        undoTransaction.Cancel();
                        return;
                    }
                    undoTransaction.Complete();
                }
                catch (Exception ex)
                {
                    undoTransaction.Cancel();
                    Debug.WriteLine("Error: {0}", ex);
                }
            }
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {

            if (spans.Count == 0 || _tagManager.IsDirty)
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

            _autoTagger.TriggerAutoTaggerEvents(_sourceBuffer.CurrentSnapshot);

            foreach (var span in spans)
            {
                foreach (var tag in _tagManager.GetClassifications(span))
                {
                    yield return new TagSpan<ClassificationTag>(tag.Key, _tagRegistry.ClassificationTags[tag.Value]);
                }
            }
        }
    }
}
