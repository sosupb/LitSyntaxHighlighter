using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
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
        internal IClassificationTypeRegistryService _classificationRegistry;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                () => new LitTemplateTagger(
                    _classificationRegistry,
                    buffer,
                    textView
                ) as ITagger<T>
            );

        }
    }
}
