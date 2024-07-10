using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace LitSyntaxHighlighter.Tagger
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("TypeScript")]
    [TagType(typeof(ClassificationTag))]
    internal class LitTemplateTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService _classificationRegistry = null;

        [Import]
        internal ITextUndoHistoryRegistry _textUndoHistoryRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if(_classificationRegistry == null)
            {
                return null;
            }

            ITextUndoHistory textUndoHistory;
            if (_textUndoHistoryRegistry == null || !_textUndoHistoryRegistry.TryGetHistory(buffer, out textUndoHistory))
            {
                return null;
            }

            return textView.Properties.GetOrCreateSingletonProperty(
                () => new LitTemplateTagger(
                    _classificationRegistry,
                    buffer,
                    textView,
                    textUndoHistory
                ) as ITagger<T>
            );

        }
    }
}
