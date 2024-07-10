using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LitSyntaxHighlighter.Tagger
{
    internal enum AutoTaggerState
    {
        Waiting,
        Queuing,
        ShouldInvokeEdits,
        Editing,
    }

    internal class AutoTaggerEventArgs : EventArgs
    {
        public Queue<AutoTaggerEdit> Edits { get; set; }
        public ITextSnapshot Snapshot { get; set; }
    }

    internal class MoveCaretEventArgs : EventArgs
    {
        public VirtualSnapshotPoint Position { get; set; }
    }

    internal class AutoTaggerEdit
    {
        public Span Span { get; private set; }
        public string Text { get; private set; }
        public string Operation { get; private set; }
        public bool PreserveCaretPosition { get; private set; }

        public AutoTaggerEdit(Span span, string text, string operation, bool preserveCaretLocation = false) 
        {
            Span = span;
            Text = text;
            Operation = operation;
            PreserveCaretPosition = preserveCaretLocation;
        }
    }

    internal class LitTemplateAutoTagger
    {
        private LitTemplateTagManager _tagManager;
        private Queue<AutoTaggerEdit> _autoTaggerEdits;
        private AutoTaggerState _state;

        public bool HasPendingEdits
        {
            get
            {
                return _autoTaggerEdits.Count > 0;
            }
        }

        public SnapshotPoint? OldCaretPosition { get; private set; }
        public int? TargetCaretDelta { get; private set; }

        public event EventHandler<AutoTaggerEventArgs> EditTextBuffer;
        public event EventHandler<MoveCaretEventArgs> MoveCaret;

        public LitTemplateAutoTagger(LitTemplateTagManager tagManager)
        {
            this._tagManager = tagManager;
            this._autoTaggerEdits = new Queue<AutoTaggerEdit>();
            this._state = AutoTaggerState.Waiting;

            this.OldCaretPosition = null;
            this.TargetCaretDelta = null;
        }

        public void TriggerAutoTaggerEvents(ITextSnapshot snapshot)
        {
            if(_state == AutoTaggerState.ShouldInvokeEdits)
            {
                _state = AutoTaggerState.Editing;
                var edits = new Queue<AutoTaggerEdit>(_autoTaggerEdits);
                _autoTaggerEdits.Clear();
                EditTextBuffer?.Invoke(this, new AutoTaggerEventArgs()
                {
                    Edits = edits,
                    Snapshot = snapshot
                });
                _state = AutoTaggerState.Waiting;
            }
        }

        public void SetCaretPosition(SnapshotPoint oldCaretPosition, int targetCaretDelta)
        {
            OldCaretPosition = oldCaretPosition;
            TargetCaretDelta = targetCaretDelta;
        }

        public void PostCheckForAutoTagging()
        {
            if(_state == AutoTaggerState.Waiting && HasPendingEdits)
            {
                _state = AutoTaggerState.ShouldInvokeEdits;
            }
        }

        public void CheckCaretMoveQueued(SnapshotPoint newCaretPosition)
        {
            if (OldCaretPosition.HasValue && TargetCaretDelta.HasValue && newCaretPosition.Position >= OldCaretPosition.Value.Position + TargetCaretDelta.Value)
            {
                OldCaretPosition = null;
                MoveCaret?.Invoke(this, new MoveCaretEventArgs()
                {
                    Position = new VirtualSnapshotPoint(newCaretPosition.Subtract(TargetCaretDelta.Value)),
                });
                TargetCaretDelta = null;
            }
        }

        public async Task QueueUpAutoTaggingAsync(ITextBuffer sourceBuffer, SnapshotSpan change)
        {
            if (_state != AutoTaggerState.Waiting)
                return;

            try
            {
                _state = AutoTaggerState.Queuing;
                GeneralOptions options = await GeneralOptions.GetLiveInstanceAsync();

                var selectedTag = _tagManager.SelectedOpenTag;
                if (selectedTag.HasValue && selectedTag.Value.Key is var selectedSpan && change.Span.IntersectsWith(selectedTag.Value.Key))
                {
                    var newName = selectedSpan.TranslateTo(change.Snapshot, SpanTrackingMode.EdgeInclusive).GetText();
                    var selectedCloseTag = _tagManager.SelectedCloseTag;
                    if (options.AutoRenameClosingTags && selectedCloseTag.HasValue && selectedCloseTag.Value.Key is var selectedCloseSpan)
                    {
                        var closeTagSpan = selectedCloseSpan.TranslateTo(change.Snapshot, SpanTrackingMode.EdgeInclusive);
                        if (newName.All(c => _tagManager.IsNameChar(c)))
                        {
                            // Replace old close tag name directly with new open tag name
                            sourceBuffer.Replace(closeTagSpan.Span, newName);
                        }
                    }
                    else if (options.AutoCloseTags)
                    {
                        if (newName.LastOrDefault() == '>')
                        {
                            // Queue new matching close tag
                            _autoTaggerEdits.Enqueue(new AutoTaggerEdit(change.Span, $"</{newName}", "Insert", true));
                        }
                        else if (newName == "!-- ")
                        {
                            // Queue new matching comment close tag
                            _autoTaggerEdits.Enqueue(new AutoTaggerEdit(change.Span, $" -->", "Insert", true));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                _state = AutoTaggerState.Waiting;
            }
        }
    }
}

    
   
    
