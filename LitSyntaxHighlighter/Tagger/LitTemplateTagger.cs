using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace LitSyntaxHighlighter.Tagger
{
    internal static partial class FormatNames { }

    internal class LitTemplateTagger : ITagger<ClassificationTag>
    {
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private LitTemplateTagManager _tagManager;
        private ITextBuffer _sourceBuffer;

        internal LitTemplateTagger(IClassificationTypeRegistryService classificationRegistry, ITextBuffer textBuffer) 
        {
            _tagManager = new LitTemplateTagManager(classificationRegistry);
            _tagManager.ParseTags(new SnapshotSpan(textBuffer.CurrentSnapshot, 0, textBuffer.CurrentSnapshot.Length));
            _sourceBuffer = textBuffer;
            textBuffer.Changed += OnBufferChanged;
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.After != _sourceBuffer.CurrentSnapshot)
                return;

            _tagManager.ParseTags(new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length));
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || !_tagManager.IsValidSnapshotVersion(spans[0].Snapshot))
            {
                yield break;
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
